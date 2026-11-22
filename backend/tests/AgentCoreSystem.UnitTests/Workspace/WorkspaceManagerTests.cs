// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Services.Workspace;
using AgentCoreSystem.Module.Agents.Infrastructure.Services.Workspace;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AgentCoreSystem.UnitTests.Workspace
{
    public class WorkspaceManagerTests : IDisposable
    {
        private readonly string _testBaseDir;
        private readonly IWorkspaceManager _workspaceManager;
        private readonly Guid _agentProfileId = Guid.CreateVersion7();

        public WorkspaceManagerTests()
        {
            _testBaseDir = Path.Combine(Path.GetTempPath(), "AgentCoreSystemTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testBaseDir);

            var inMemorySettings = new System.Collections.Generic.Dictionary<string, string?>
            {
                { "Storage:WorkspacesRootPath", _testBaseDir }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _workspaceManager = new WorkspaceManager(configuration);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testBaseDir))
                {
                    Directory.Delete(_testBaseDir, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup failures in temp dir
            }
        }

        [Fact]
        public async Task EnsureWorkspaceDirectoriesAsync_ShouldCreateStandardFolders()
        {
            // Act
            var result = await _workspaceManager.EnsureWorkspaceDirectoriesAsync(_agentProfileId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(Directory.Exists(_workspaceManager.GetAgentWorkspacePath(_agentProfileId)));
            Assert.True(Directory.Exists(_workspaceManager.GetScriptsPath(_agentProfileId)));
            Assert.True(Directory.Exists(_workspaceManager.GetArtifactsPath(_agentProfileId)));
            Assert.True(Directory.Exists(_workspaceManager.GetLogsPath(_agentProfileId)));
        }

        [Fact]
        public async Task ListFilesAsync_WhenFilesExist_ShouldCategorizeCorrectly()
        {
            // Arrange
            await _workspaceManager.EnsureWorkspaceDirectoriesAsync(_agentProfileId);
            var scriptPath = Path.Combine(_workspaceManager.GetScriptsPath(_agentProfileId), "test_script.py");
            await File.WriteAllTextAsync(scriptPath, "print('hello')");

            var artifactPath = Path.Combine(_workspaceManager.GetArtifactsPath(_agentProfileId), "result.csv");
            await File.WriteAllTextAsync(artifactPath, "id,name\n1,test");

            // Act
            var result = await _workspaceManager.ListFilesAsync(_agentProfileId);

            // Assert
            Assert.True(result.IsSuccess);
            var files = result.Value;
            Assert.Contains(files, f => f.Name == "test_script.py" && f.Category == "scripts");
            Assert.Contains(files, f => f.Name == "result.csv" && f.Category == "artifacts");
        }

        [Theory]
        [InlineData("../../outside.txt")]
        [InlineData("..\\..\\windows_leak.txt")]
        [InlineData("/etc/passwd")]
        [InlineData("C:\\Windows\\System32\\cmd.exe")]
        public async Task PathTraversal_ShouldBeBlockedByValidation(string maliciousPath)
        {
            // Arrange
            await _workspaceManager.EnsureWorkspaceDirectoriesAsync(_agentProfileId);

            // Act
            var streamResult = await _workspaceManager.GetFileStreamAsync(_agentProfileId, maliciousPath);
            var deleteResult = await _workspaceManager.DeleteFileAsync(_agentProfileId, maliciousPath);

            // Assert
            Assert.False(streamResult.IsSuccess);
            Assert.Equal("Workspace.InvalidPath", streamResult.Error.Code);

            Assert.False(deleteResult.IsSuccess);
            Assert.Equal("Workspace.InvalidPath", deleteResult.Error.Code);
        }

        [Fact]
        public async Task WriteScriptFileAsync_ValidName_ShouldCreateFileUnderScripts()
        {
            // Arrange
            await _workspaceManager.EnsureWorkspaceDirectoriesAsync(_agentProfileId);
            const string scriptContent = "import sys\nprint(sys.version)";

            // Act
            var writeResult = await _workspaceManager.WriteScriptFileAsync(_agentProfileId, "benchmark.py", scriptContent);

            // Assert
            Assert.True(writeResult.IsSuccess);
            var expectedFilePath = Path.Combine(_workspaceManager.GetScriptsPath(_agentProfileId), "benchmark.py");
            Assert.True(File.Exists(expectedFilePath));
            Assert.Equal(scriptContent, await File.ReadAllTextAsync(expectedFilePath));
        }

        [Fact]
        public async Task GetWorkspaceStatsAsync_ShouldCalculateSizeAndCounts()
        {
            // Arrange
            await _workspaceManager.EnsureWorkspaceDirectoriesAsync(_agentProfileId);
            await _workspaceManager.WriteScriptFileAsync(_agentProfileId, "run.py", "print('hello world')");

            // Act
            var statsResult = await _workspaceManager.GetWorkspaceStatsAsync(_agentProfileId);

            // Assert
            Assert.True(statsResult.IsSuccess);
            var stats = statsResult.Value;
            Assert.Equal(_agentProfileId, stats.AgentProfileId);
            Assert.Equal(1, stats.ScriptsCount);
            Assert.Equal(0, stats.ArtifactsCount);
            Assert.True(stats.TotalSizeBytes > 0);
        }
    }
}

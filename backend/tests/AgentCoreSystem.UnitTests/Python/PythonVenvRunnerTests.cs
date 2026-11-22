// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Services.Python;
using AgentCoreSystem.Module.Agents.Application.Services.Workspace;
using AgentCoreSystem.Module.Agents.Infrastructure.Services.Python;
using AgentCoreSystem.Module.Agents.Infrastructure.Services.Workspace;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AgentCoreSystem.UnitTests.Python
{
    public class PythonVenvRunnerTests : IDisposable
    {
        private readonly string _testBaseDir;
        private readonly IWorkspaceManager _workspaceManager;
        private readonly IPythonSandboxRunner _runner;
        private readonly Guid _agentProfileId = Guid.CreateVersion7();

        public PythonVenvRunnerTests()
        {
            _testBaseDir = Path.Combine(Path.GetTempPath(), "AgentCorePythonTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testBaseDir);

            // Inject a fake sensitive environment variable in current process to test stripping
            Environment.SetEnvironmentVariable("DefaultConnection", "Server=Secret;Database=Secret;");
            Environment.SetEnvironmentVariable("JWT_SECRET_KEY", "SuperSecret1234567890!");

            var inMemorySettings = new System.Collections.Generic.Dictionary<string, string?>
            {
                { "Storage:WorkspacesRootPath", _testBaseDir }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _workspaceManager = new WorkspaceManager(configuration);
            _runner = new PythonVenvRunner(_workspaceManager, configuration);
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
                // Ignore cleanup failures
            }
        }

        [Fact]
        public async Task ExecuteScriptAsync_ValidScript_ShouldReturnSuccessWithOutput()
        {
            // Arrange
            const string script = "print('Hello from Python Sandbox!')";
            var request = new PythonExecutionRequest(
                AgentProfileId: _agentProfileId,
                ScriptContent: script,
                ScriptName: "test_hello.py"
            );

            // Act
            var result = await _runner.ExecuteScriptAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.Value.ExitCode);
            Assert.Contains("Hello from Python Sandbox!", result.Value.Stdout);
            Assert.False(result.Value.IsTimeout);
        }

        [Fact]
        public async Task ExecuteScriptAsync_EnvironmentVariables_MustBeStrippedOfSecrets()
        {
            // Arrange
            const string script = @"
import os
print('LEAK_DB:', 'DefaultConnection' in os.environ)
print('LEAK_JWT:', 'JWT_SECRET_KEY' in os.environ)
";
            var request = new PythonExecutionRequest(
                AgentProfileId: _agentProfileId,
                ScriptContent: script,
                ScriptName: "test_secrets.py"
            );

            // Act
            var result = await _runner.ExecuteScriptAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("LEAK_DB: False", result.Value.Stdout);
            Assert.Contains("LEAK_JWT: False", result.Value.Stdout);
        }

        [Fact]
        public async Task ExecuteScriptAsync_WhenTimeoutOccurs_ShouldTerminateProcessAndSetTimeoutFlag()
        {
            // Arrange
            const string infiniteLoopScript = @"
import time
while True:
    time.sleep(0.1)
";
            var request = new PythonExecutionRequest(
                AgentProfileId: _agentProfileId,
                ScriptContent: infiniteLoopScript,
                ScriptName: "infinite.py",
                TimeoutSeconds: 1
            );

            // Act
            var result = await _runner.ExecuteScriptAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value.IsTimeout);
            Assert.NotEqual(0, result.Value.ExitCode);
        }

        [Fact]
        public async Task ExecuteScriptAsync_WhenWritingArtifacts_ShouldDetectNewGeneratedArtifacts()
        {
            // Arrange
            const string script = @"
import os
os.makedirs('artifacts', exist_ok=True)
with open('artifacts/report.csv', 'w') as f:
    f.write('id,metric\n1,100\n')
";
            var request = new PythonExecutionRequest(
                AgentProfileId: _agentProfileId,
                ScriptContent: script,
                ScriptName: "gen_artifact.py"
            );

            // Act
            var result = await _runner.ExecuteScriptAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains(result.Value.GeneratedArtifacts, a => a.FileName == "report.csv");
            var artifactFilePath = Path.Combine(_workspaceManager.GetArtifactsPath(_agentProfileId), "report.csv");
            Assert.True(File.Exists(artifactFilePath));
        }

        [Fact]
        public async Task ExecuteScriptAsync_SyntaxError_ShouldCaptureStderr()
        {
            // Arrange
            const string badScript = "def broken_syntax(:\n    pass";
            var request = new PythonExecutionRequest(
                AgentProfileId: _agentProfileId,
                ScriptContent: badScript,
                ScriptName: "syntax_err.py"
            );

            // Act
            var result = await _runner.ExecuteScriptAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(result.Value.IsSuccess);
            Assert.NotEqual(0, result.Value.ExitCode);
            Assert.Contains("SyntaxError", result.Value.Stderr);
        }
    }
}

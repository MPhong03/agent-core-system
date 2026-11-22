// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Services.Python;
using AgentCoreSystem.Module.Agents.Application.Services.Workspace;
using AgentCoreSystem.Module.Agents.Application.Workspace.Commands;
using AgentCoreSystem.Module.Agents.Application.Workspace.DTOs;
using AgentCoreSystem.Module.Agents.Application.Workspace.Queries;
using AgentCoreSystem.Module.Agents.Presentation.Contracts;
using AgentCoreSystem.Module.Agents.Presentation.Extensions;
using AgentCoreSystem.Shared.Kernel.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgentCoreSystem.Module.Agents.Presentation.Controllers.v1
{
    /// <summary>
    /// Quản lý không gian làm việc (Workspace), môi trường ảo (.venv) và thực thi script Python cho Agent.
    /// </summary>
    [ApiController]
    [Route("api/v1/agents/{agentId:guid}/workspace")]
    public class AgentWorkspaceController(
        ISender sender,
        IWorkspaceManager workspaceManager,
        IPythonSandboxRunner pythonRunner) : BaseApiController
    {
        /// <summary>
        /// Lấy thông tin cấu hình và thống kê không gian làm việc của Agent.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<AgentWorkspaceDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetWorkspace(
            [FromRoute] Guid agentId,
            CancellationToken cancellationToken)
        {
            var query = new GetAgentWorkspaceQuery(agentId);
            var result = await sender.Send(query, cancellationToken);

            return result.IsSuccess
                ? Ok(ApiResponse<AgentWorkspaceDetailDto>.Ok(result.Value))
                : result.ToProblemDetails(this);
        }

        /// <summary>
        /// Khởi tạo hoặc sửa chữa môi trường ảo .venv cho Agent.
        /// </summary>
        [HttpPost("init-venv")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> InitializeVenv(
            [FromRoute] Guid agentId,
            CancellationToken cancellationToken)
        {
            var command = new InitializeVenvCommand(agentId);
            var result = await sender.Send(command, cancellationToken);

            return result.IsSuccess
                ? Ok(ApiResponse<string>.Ok(result.Value, "Khởi tạo môi trường ảo Python thành công."))
                : result.ToProblemDetails(this);
        }

        /// <summary>
        /// Lấy danh sách các gói thư viện đã cài đặt trong môi trường ảo của Agent (pip list).
        /// </summary>
        [HttpGet("packages")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<string>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPackages(
            [FromRoute] Guid agentId,
            CancellationToken cancellationToken)
        {
            var result = await pythonRunner.ListInstalledPackagesAsync(agentId, cancellationToken);

            return result.IsSuccess
                ? Ok(ApiResponse<IReadOnlyList<string>>.Ok(result.Value))
                : result.ToProblemDetails(this);
        }

        /// <summary>
        /// Cài đặt các gói thư viện Python mới vào .venv của Agent.
        /// </summary>
        [HttpPost("install-packages")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> InstallPackages(
            [FromRoute] Guid agentId,
            [FromBody] InstallPackagesApiRequest request,
            CancellationToken cancellationToken)
        {
            var command = new InstallPackagesCommand(agentId, request.Packages);
            var result = await sender.Send(command, cancellationToken);

            return result.IsSuccess
                ? Ok(ApiResponse<string>.Ok(result.Value, "Cài đặt packages thành công."))
                : result.ToProblemDetails(this);
        }

        /// <summary>
        /// Duyệt danh sách tệp tin trong không gian làm việc của Agent (scripts, artifacts, logs).
        /// </summary>
        [HttpGet("files")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<WorkspaceFileDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ListFiles(
            [FromRoute] Guid agentId,
            [FromQuery] string? category,
            CancellationToken cancellationToken)
        {
            var query = new ListWorkspaceFilesQuery(agentId, category);
            var result = await sender.Send(query, cancellationToken);

            return result.IsSuccess
                ? Ok(ApiResponse<IReadOnlyList<WorkspaceFileDto>>.Ok(result.Value))
                : result.ToProblemDetails(this);
        }

        /// <summary>
        /// Tải xuống một tệp tin từ không gian làm việc của Agent.
        /// </summary>
        [HttpGet("files/download")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadFile(
            [FromRoute] Guid agentId,
            [FromQuery] string path,
            CancellationToken cancellationToken)
        {
            var result = await workspaceManager.GetFileStreamAsync(agentId, path, cancellationToken);
            if (!result.IsSuccess)
            {
                return result.ToProblemDetails(this);
            }

            var fileName = Path.GetFileName(path);
            var contentType = GetContentType(fileName);
            return File(result.Value, contentType, fileName);
        }

        /// <summary>
        /// Thực thi script Python thử nghiệm từ giao diện quản trị.
        /// </summary>
        [HttpPost("execute")]
        [ProducesResponseType(typeof(ApiResponse<PythonExecutionResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ExecuteScript(
            [FromRoute] Guid agentId,
            [FromBody] ExecuteScriptApiRequest request,
            CancellationToken cancellationToken)
        {
            var command = new ExecuteScriptCommand(
                AgentProfileId: agentId,
                ScriptContent: request.ScriptContent,
                ScriptName: request.ScriptName,
                TimeoutSeconds: request.TimeoutSeconds,
                CallerType: "Manual",
                ExecutionSourceId: GetCurrentUserId()
            );

            var result = await sender.Send(command, cancellationToken);

            return result.IsSuccess
                ? Ok(ApiResponse<PythonExecutionResult>.Ok(result.Value))
                : result.ToProblemDetails(this);
        }

        /// <summary>
        /// Lấy lịch sử các lần thực thi mã nguồn Python của Agent.
        /// </summary>
        [HttpGet("history")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ScriptExecutionHistoryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetHistory(
            [FromRoute] Guid agentId,
            [FromQuery] int limit = 50,
            CancellationToken cancellationToken = default)
        {
            var query = new GetScriptHistoryQuery(agentId, limit);
            var result = await sender.Send(query, cancellationToken);

            return result.IsSuccess
                ? Ok(ApiResponse<IReadOnlyList<ScriptExecutionHistoryDto>>.Ok(result.Value))
                : result.ToProblemDetails(this);
        }

        private static string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".csv" => "text/csv",
                ".json" => "application/json",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".svg" => "image/svg+xml",
                ".pdf" => "application/pdf",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".txt" => "text/plain",
                ".py" => "text/x-python",
                _ => "application/octet-stream"
            };
        }
    }
}

// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using AgentCoreSystem.Module.Organization.Application.AgentProfiles.Commands.CreateAgentProfile;
using AgentCoreSystem.Module.Organization.Application.AgentProfiles.DTOs;
using AgentCoreSystem.Module.Organization.Application.AgentProfiles.Queries.GetAgentProfiles;
using AgentCoreSystem.Module.Organization.Presentation.Extensions;
using AgentCoreSystem.Module.Organization.Presentation.Requests;
using AgentCoreSystem.Shared.Kernel.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgentCoreSystem.Module.Organization.Presentation.Controllers.v1
{
    /// <summary>
    /// Phân hệ Quản lý Hồ sơ Định danh Nhân sự AI Agent (Phiên bản API 1.0).
    /// </summary>
    /// <remarks>
    /// Cung cấp các thao tác cấu hình hồ sơ Agent, lựa chọn model LLM, tham số sáng tạo và quản lý bộ nhớ riêng.
    /// </remarks>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/agent-profiles")]
    [Produces("application/json")]
    public class AgentProfilesController(ISender mediator) : ControllerBase
    {
        // ── Command Actions ──

        /// <summary>
        /// Tạo mới một hồ sơ nhân sự AI Agent.
        /// </summary>
        /// <remarks>
        /// ### Luồng nghiệp vụ:
        /// 1. Xác thực thông tin mô hình LLM, System Prompt và phạm vi nhiệt độ (0.0 - 2.0).
        /// 2. Khởi tạo hồ sơ Agent với mã định danh UUIDv7.
        /// 3. Lưu hồ sơ vào cơ sở dữ liệu trên schema 'org'.
        /// </remarks>
        /// <param name="request">Thông tin cấu hình Agent cần tạo.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <response code="201">Tạo hồ sơ Agent thành công. Trả về Id định danh UUIDv7 của hồ sơ.</response>
        /// <response code="400">Dữ liệu cấu hình không hợp lệ (Validation Error).</response>
        /// <response code="500">Lỗi máy chủ nội bộ.</response>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateAgentProfile(
            [FromBody] CreateAgentProfileRequest request,
            CancellationToken cancellationToken)
        {
            var command = new CreateAgentProfileCommand(
                request.Name,
                request.AvatarUrl,
                request.Provider,
                request.ModelName,
                request.SystemPrompt,
                request.Temperature,
                request.PrivateMemoryNotes,
                request.ToolExecutionMode);

            var result = await mediator.Send(command, cancellationToken);

            return result.IsSuccess
                ? StatusCode(StatusCodes.Status201Created, ApiResponse<Guid>.Ok(result.Value, "Tạo hồ sơ Agent thành công."))
                : result.ToProblemDetails(this);
        }

        // ── Query Actions ──

        /// <summary>
        /// Lấy danh sách toàn bộ hồ sơ nhân sự AI Agent trong tổ chức.
        /// </summary>
        /// <remarks>
        /// ### Luồng nghiệp vụ:
        /// - Truy vấn toàn bộ hồ sơ Agent còn hoạt động (chưa bị xóa mềm).
        /// - Trả về đầy đủ thông tin Provider, ModelName, Temperature và System Prompt.
        /// </remarks>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <response code="200">Truy vấn thành công. Trả về danh sách hồ sơ Agent.</response>
        /// <response code="500">Lỗi máy chủ nội bộ.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AgentProfileDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAgentProfiles(CancellationToken cancellationToken)
        {
            var query = new GetAgentProfilesQuery();
            var result = await mediator.Send(query, cancellationToken);

            return result.IsSuccess
                ? Ok(ApiResponse<IReadOnlyList<AgentProfileDto>>.Ok(result.Value))
                : result.ToProblemDetails(this);
        }
    }
}

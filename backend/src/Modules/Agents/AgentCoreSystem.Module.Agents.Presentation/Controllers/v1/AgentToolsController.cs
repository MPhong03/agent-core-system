// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Tools.Commands;
using AgentCoreSystem.Module.Agents.Application.Tools.DTOs;
using AgentCoreSystem.Module.Agents.Application.Tools.Queries;
using AgentCoreSystem.Module.Agents.Presentation.Contracts;
using AgentCoreSystem.Module.Agents.Presentation.Extensions;
using AgentCoreSystem.Shared.Kernel.Responses;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgentCoreSystem.Module.Agents.Presentation.Controllers.v1
{
    /// <summary>
    /// API Controller quản lý phân quyền công cụ cho Agent Profile.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/agents/profiles/{agentId:guid}/tools")]
    [Authorize]
    public class AgentToolsController(ISender mediator) : BaseApiController
    {
        /// <summary>
        /// Lấy danh sách các công cụ đang hoạt động được phân quyền cho một Agent Profile cụ thể.
        /// </summary>
        /// <param name="agentId">Mã định danh Agent Profile cần truy vấn.</param>
        /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
        /// <returns>Danh sách các công cụ được cấp quyền cho Agent.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<ToolDefinitionDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAgentTools(Guid agentId, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new GetAgentToolsQuery(agentId, MaskSecrets: true), cancellationToken);
            if (result.IsFailure)
            {
                return result.ToProblemDetails(this);
            }

            return Ok(ApiResponse<List<ToolDefinitionDto>>.Ok(result.Value, "Lấy danh sách công cụ của Agent thành công."));
        }

        /// <summary>
        /// Phân quyền và cập nhật danh sách các công cụ được phép sử dụng cho một Agent Profile.
        /// </summary>
        /// <remarks>
        /// Hệ thống sẽ kiểm tra tính tồn tại và trạng thái hoạt động của các công cụ yêu cầu trước khi đồng bộ phân quyền.
        /// </remarks>
        /// <param name="agentId">Mã định danh Agent Profile cần phân quyền.</param>
        /// <param name="request">Danh sách mã UUIDv7 các công cụ cần cấp quyền.</param>
        /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
        /// <returns>Thông báo kết quả thao tác phân quyền công cụ.</returns>
        [HttpPut]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignAgentTools(
            Guid agentId,
            [FromBody] AssignAgentToolsRequest request,
            CancellationToken cancellationToken)
        {
            var command = new AssignAgentToolsCommand(agentId, request.ToolDefinitionIds);
            var result = await mediator.Send(command, cancellationToken);
            if (result.IsFailure)
            {
                return result.ToProblemDetails(this);
            }

            return Ok(ApiResponse<string>.Ok("Phân quyền công cụ cho Agent thành công."));
        }
    }
}

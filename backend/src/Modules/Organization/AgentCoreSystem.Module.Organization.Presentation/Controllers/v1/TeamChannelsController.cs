// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using AgentCoreSystem.Module.Organization.Application.Members.Commands.AssignMemberToChannel;
using AgentCoreSystem.Module.Organization.Application.Members.DTOs;
using AgentCoreSystem.Module.Organization.Application.Members.Queries.GetChannelMembers;
using AgentCoreSystem.Module.Organization.Presentation.Extensions;
using AgentCoreSystem.Module.Organization.Presentation.Requests;
using AgentCoreSystem.Shared.Kernel.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgentCoreSystem.Module.Organization.Presentation.Controllers.v1
{
    /// <summary>
    /// Phân hệ Quản lý Kênh làm việc &amp; Thành viên cộng tác (Phiên bản API 1.0).
    /// </summary>
    /// <remarks>
    /// Cung cấp các thao tác quản lý thành viên (Human và AI Agent) trong các kênh nhóm.
    /// </remarks>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/channels")]
    [Produces("application/json")]
    public class TeamChannelsController(ISender mediator) : ControllerBase
    {
        /// <summary>
        /// Gán một thành viên (Con người hoặc AI Agent) vào kênh làm việc.
        /// </summary>
        /// <remarks>
        /// ### Luồng nghiệp vụ:
        /// 1. Kiểm tra sự tồn tại của kênh làm việc.
        /// 2. Nếu thành viên là AI Agent, kiểm tra sự tồn tại của AgentProfileId.
        /// 3. Đảm bảo tên hiển thị (DisplayName) là duy nhất trong phạm vi kênh.
        /// 4. Tạo bản ghi thành viên liên kết và lưu vào cơ sở dữ liệu.
        /// </remarks>
        /// <param name="channelId">Mã định danh kênh làm việc.</param>
        /// <param name="request">Thông tin thành viên cần gán.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <response code="201">Gán thành viên thành công. Trả về Id định danh UUIDv7 của thành viên.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ (Validation Error).</response>
        /// <response code="404">Không tìm thấy kênh hoặc hồ sơ Agent tương ứng.</response>
        /// <response code="409">Tên hiển thị đã được sử dụng trong kênh này (Conflict Error).</response>
        /// <response code="500">Lỗi máy chủ nội bộ.</response>
        [HttpPost("{channelId:guid}/members")]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AssignMember(
            [FromRoute] Guid channelId,
            [FromBody] AssignMemberRequest request,
            CancellationToken cancellationToken)
        {
            var command = new AssignMemberToChannelCommand(
                channelId,
                request.Type,
                request.UserId,
                request.AgentProfileId,
                request.DisplayName);

            var result = await mediator.Send(command, cancellationToken);

            return result.IsSuccess
                ? StatusCode(StatusCodes.Status201Created, ApiResponse<Guid>.Ok(result.Value, "Gán thành viên vào kênh thành công."))
                : result.ToProblemDetails(this);
        }

        /// <summary>
        /// Lấy danh sách thành viên (Human &amp; Agent) đang tham gia kênh làm việc.
        /// </summary>
        /// <remarks>
        /// ### Luồng nghiệp vụ:
        /// 1. Kiểm tra sự tồn tại của kênh làm việc.
        /// 2. Truy vấn danh sách thành viên thuộc kênh (loại trừ các bản ghi xóa mềm).
        /// </remarks>
        /// <param name="channelId">Mã định danh kênh làm việc.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <response code="200">Truy vấn thành công. Trả về danh sách thành viên kênh.</response>
        /// <response code="404">Không tìm thấy kênh làm việc với mã cung cấp.</response>
        /// <response code="500">Lỗi máy chủ nội bộ.</response>
        [HttpGet("{channelId:guid}/members")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MemberDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetChannelMembers(
            [FromRoute] Guid channelId,
            CancellationToken cancellationToken)
        {
            var query = new GetChannelMembersQuery(channelId);
            var result = await mediator.Send(query, cancellationToken);

            return result.IsSuccess
                ? Ok(ApiResponse<IReadOnlyList<MemberDto>>.Ok(result.Value))
                : result.ToProblemDetails(this);
        }
    }
}

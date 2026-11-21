// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using AgentCoreSystem.Module.Chat.Application.Messages.DTOs;
using AgentCoreSystem.Module.Chat.Application.Messages.Queries;
using AgentCoreSystem.Module.Chat.Application.Tools.Commands;
using AgentCoreSystem.Module.Chat.Application.Tools.DTOs;
using AgentCoreSystem.Module.Chat.Presentation.Extensions;
using AgentCoreSystem.Shared.Kernel.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgentCoreSystem.Module.Chat.Presentation.Controllers.v1
{
    /// <summary>
    /// Phân hệ Trò chuyện Thời gian thực &amp; Lịch sử Tin nhắn Kênh (Phiên bản API 1.0).
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Cung cấp các thao tác truy vấn lịch sử tin nhắn trò chuyện theo kênh làm việc.</para>
    /// <para><strong>When:</strong> Khách hàng (Angular Client) cần nạp dữ liệu lịch sử hội thoại khi người dùng truy cập hoặc cuộn ngược trang trong kênh làm việc.</para>
    /// <para><strong>Why:</strong> Đảm bảo phân trang tối ưu hiệu năng, giảm thiểu lượng dữ liệu truyền tải và hỗ trợ nạp tin nhắn theo mốc thời gian.</para>
    /// </remarks>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/channels")]
    [Authorize]
    public class ChatController(ISender mediator) : BaseApiController
    {
        // ── Channel Message Queries ──

        /// <summary>
        /// Truy vấn danh sách tin nhắn trong một kênh làm việc với phân trang theo mốc tin nhắn.
        /// </summary>
        /// <remarks>
        /// ### Luồng nghiệp vụ:
        /// 1. Tiếp nhận mã định danh kênh làm việc (<paramref name="channelId"/>) cùng các tham số phân trang (<paramref name="limit"/>, <paramref name="before"/>).
        /// 2. Nếu có tham số <paramref name="before"/> (Id tin nhắn mốc), hệ thống sẽ tìm tin nhắn mốc để phân trang lùi các tin nhắn cũ hơn.
        /// 3. Lấy tối đa <paramref name="limit"/> tin nhắn (mặc định 50) và trả về danh sách theo thứ tự thời gian tăng dần.
        /// </remarks>
        /// <param name="channelId">Mã định danh kênh làm việc.</param>
        /// <param name="limit">Số lượng tin nhắn tối đa cần lấy (mặc định 50).</param>
        /// <param name="before">Mã định danh tin nhắn mốc để phân trang lùi (tùy chọn).</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <response code="200">Truy vấn thành công. Trả về danh sách tin nhắn kênh.</response>
        /// <response code="400">Tham số yêu cầu không hợp lệ (Validation Error).</response>
        /// <response code="401">Chưa xác thực thông tin đăng nhập.</response>
        /// <response code="404">Không tìm thấy tin nhắn mốc tương ứng.</response>
        /// <response code="500">Lỗi máy chủ nội bộ.</response>
        [HttpGet("{channelId:guid}/messages")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ChatMessageDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetChannelMessages(
            [FromRoute] Guid channelId,
            [FromQuery] int limit = 50,
            [FromQuery] Guid? before = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetChannelMessagesQuery(channelId, limit, before);
            var result = await mediator.Send(query, cancellationToken);

            return result.IsSuccess
                ? Ok(ApiResponse<IReadOnlyList<ChatMessageDto>>.Ok(result.Value, "Truy vấn lịch sử tin nhắn kênh thành công."))
                : result.ToProblemDetails(this);
        }

        // ── Tool Call Approvals ──

        /// <summary>
        /// Phê duyệt và thực thi yêu cầu gọi công cụ đang chờ duyệt trong kênh trò chuyện.
        /// </summary>
        /// <param name="callId">Mã định danh yêu cầu gọi công cụ (PendingToolCallId).</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <response code="200">Xác nhận và kích hoạt thực thi công cụ thành công.</response>
        /// <response code="400">Yêu cầu không hợp lệ hoặc đã hết hạn.</response>
        /// <response code="401">Chưa xác thực thông tin đăng nhập.</response>
        /// <response code="404">Không tìm thấy yêu cầu gọi công cụ.</response>
        /// <response code="409">Yêu cầu gọi công cụ không ở trạng thái chờ duyệt.</response>
        /// <response code="500">Lỗi máy chủ nội bộ.</response>
        [HttpPost("~/api/v{version:apiVersion}/chat/tool-calls/{callId:guid}/confirm")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ConfirmToolCall(
            [FromRoute] Guid callId,
            CancellationToken cancellationToken = default)
        {
            var command = new ConfirmToolCallCommand(callId);
            var result = await mediator.Send(command, cancellationToken);

            return result.IsSuccess
                ? Ok(ApiResponse<bool>.Ok(result.Value, "Xác nhận thực thi công cụ thành công."))
                : result.ToProblemDetails(this);
        }

        /// <summary>
        /// Từ chối yêu cầu thực thi công cụ đang chờ duyệt trong kênh trò chuyện.
        /// </summary>
        /// <param name="callId">Mã định danh yêu cầu gọi công cụ (PendingToolCallId).</param>
        /// <param name="request">Thông tin lý do từ chối (tùy chọn).</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <response code="200">Từ chối thực thi công cụ thành công.</response>
        /// <response code="400">Yêu cầu không hợp lệ hoặc đã hết hạn.</response>
        /// <response code="401">Chưa xác thực thông tin đăng nhập.</response>
        /// <response code="404">Không tìm thấy yêu cầu gọi công cụ.</response>
        /// <response code="409">Yêu cầu gọi công cụ không ở trạng thái chờ duyệt.</response>
        /// <response code="500">Lỗi máy chủ nội bộ.</response>
        [HttpPost("~/api/v{version:apiVersion}/chat/tool-calls/{callId:guid}/reject")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RejectToolCall(
            [FromRoute] Guid callId,
            [FromBody] RejectToolCallRequest? request = null,
            CancellationToken cancellationToken = default)
        {
            var command = new RejectToolCallCommand(callId, request?.Reason);
            var result = await mediator.Send(command, cancellationToken);

            return result.IsSuccess
                ? Ok(ApiResponse<bool>.Ok(result.Value, "Từ chối thực thi công cụ thành công."))
                : result.ToProblemDetails(this);
        }
    }
}

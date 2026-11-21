// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Contracts;
using AgentCoreSystem.Module.Agents.Domain.Entities;
using AgentCoreSystem.Module.Agents.Domain.Enums;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;

namespace AgentCoreSystem.Module.Agents.Application.Tools.Commands
{
    /// <summary>
    /// Lệnh ghi lại nhật ký thực thi công cụ (Tool Execution Log).
    /// </summary>
    /// <param name="ToolDefinitionId">Mã định danh công cụ được gọi.</param>
    /// <param name="AgentProfileId">Mã định danh Agent thực hiện cuộc gọi.</param>
    /// <param name="CallerType">Ngữ cảnh gọi công cụ (Chat hoặc Task).</param>
    /// <param name="ExecutionSourceId">Mã định danh nguồn phát sinh cuộc gọi (ChatMessageId hoặc TaskItemId).</param>
    /// <param name="InputArguments">Chuỗi JSON các tham số đầu vào.</param>
    /// <param name="ResponsePayload">Nội dung phản hồi từ API mục tiêu.</param>
    /// <param name="StatusCode">Mã trạng thái HTTP.</param>
    /// <param name="DurationMs">Thời gian thực thi tính theo mili-giây.</param>
    /// <param name="IsSuccess">Cờ xác định lệnh gọi thành công hay thất bại.</param>
    /// <param name="ErrorMessage">Thông điệp lỗi chi tiết (nếu thất bại).</param>
    public record RecordToolExecutionCommand(
        Guid ToolDefinitionId,
        Guid AgentProfileId,
        ToolCallerType CallerType,
        Guid ExecutionSourceId,
        string? InputArguments,
        string? ResponsePayload,
        int? StatusCode,
        long DurationMs,
        bool IsSuccess,
        string? ErrorMessage = null) : ICommand<Guid>;

    /// <summary>
    /// Handler xử lý ghi nhật ký thực thi công cụ vào CSDL schema 'agent'.
    /// </summary>
    public class RecordToolExecutionCommandHandler(IAgentsDbContext dbContext)
        : ICommandHandler<RecordToolExecutionCommand, Guid>
    {
        /// <inheritdoc />
        public async Task<Result<Guid>> Handle(
            RecordToolExecutionCommand request,
            CancellationToken cancellationToken)
        {
            var log = new ToolExecutionLog(
                request.ToolDefinitionId,
                request.AgentProfileId,
                request.CallerType,
                request.ExecutionSourceId,
                request.InputArguments,
                request.ResponsePayload,
                request.StatusCode,
                request.DurationMs,
                request.IsSuccess,
                request.ErrorMessage);

            dbContext.ToolExecutionLogs.Add(log);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success(log.Id);
        }
    }
}

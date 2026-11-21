// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Module.Agents.Domain.Enums;
using AgentCoreSystem.Shared.Kernel.Primitives;

namespace AgentCoreSystem.Module.Agents.Domain.Entities
{
    /// <summary>
    /// Thực thể nhật ký lịch sử thực thi công cụ (Tool Execution Log).
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Lưu vết chi tiết từng lần gọi công cụ bao gồm tham số đầu vào, kết quả phản hồi HTTP, thời gian chạy và thông báo lỗi nếu có.</para>
    /// <para><strong>Why:</strong> Phục vụ Audit Trail, giám sát độ trễ (latency), phát hiện lỗi tích hợp và kiểm tra an toàn bảo mật.</para>
    /// </remarks>
    public class ToolExecutionLog : BaseEntity
    {
        // ── Constructors ──

        /// <summary>
        /// Khởi tạo phục vụ Entity Framework Core deserialization.
        /// </summary>
        protected ToolExecutionLog() : base()
        {
        }

        /// <summary>
        /// Khởi tạo một bản ghi nhật ký thực thi công cụ mới với khóa chính UUIDv7.
        /// </summary>
        /// <param name="toolDefinitionId">Mã định danh công cụ được gọi.</param>
        /// <param name="agentProfileId">Mã định danh Agent thực hiện cuộc gọi.</param>
        /// <param name="callerType">Nguồn gốc ngữ cảnh gọi công cụ (Chat hoặc Task).</param>
        /// <param name="executionSourceId">Mã định danh nguồn phát sinh cuộc gọi (ChatMessageId hoặc TaskItemId).</param>
        /// <param name="inputArguments">Chuỗi JSON các tham số đầu vào.</param>
        /// <param name="responsePayload">Nội dung phản hồi từ API mục tiêu.</param>
        /// <param name="statusCode">Mã trạng thái HTTP (200, 400, 500...).</param>
        /// <param name="durationMs">Thời gian thực thi tính theo mili-giây.</param>
        /// <param name="isSuccess">Cờ xác định lệnh gọi thành công hay thất bại.</param>
        /// <param name="errorMessage">Thông điệp lỗi chi tiết (nếu thất bại).</param>
        /// <param name="id">Mã định danh (UUIDv7, tùy chọn).</param>
        public ToolExecutionLog(
            Guid toolDefinitionId,
            Guid agentProfileId,
            ToolCallerType callerType,
            Guid executionSourceId,
            string? inputArguments,
            string? responsePayload,
            int? statusCode,
            long durationMs,
            bool isSuccess,
            string? errorMessage = null,
            Guid? id = null) : base(id ?? Guid.Empty)
        {
            ToolDefinitionId = toolDefinitionId;
            AgentProfileId = agentProfileId;
            CallerType = callerType;
            ExecutionSourceId = executionSourceId;
            InputArguments = inputArguments;
            ResponsePayload = responsePayload;
            StatusCode = statusCode;
            DurationMs = durationMs;
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        // ── Properties ──

        /// <summary>
        /// Mã định danh công cụ được thực thi.
        /// </summary>
        public Guid ToolDefinitionId { get; private set; }

        /// <summary>
        /// Mã định danh hồ sơ Agent thực hiện cuộc gọi (tham chiếu logic sang org.AgentProfiles).
        /// </summary>
        /// <remarks>⛔ <strong>Cấm:</strong> Không tạo Foreign Key vật lý sang bảng org.AgentProfiles.</remarks>
        public Guid AgentProfileId { get; private set; }

        /// <summary>
        /// Ngữ cảnh gọi công cụ (Chat hoặc Task).
        /// </summary>
        public ToolCallerType CallerType { get; private set; }

        /// <summary>
        /// Mã định danh nguồn phát sinh cuộc gọi (ChatMessageId hoặc TaskItemId).
        /// </summary>
        public Guid ExecutionSourceId { get; private set; }

        /// <summary>
        /// Các tham số đầu vào truyền vào công cụ dưới dạng JSON.
        /// </summary>
        public string? InputArguments { get; private set; }

        /// <summary>
        /// Dữ liệu phản hồi nhận được từ công cụ dưới dạng chuỗi (thường là JSON hoặc văn bản).
        /// </summary>
        public string? ResponsePayload { get; private set; }

        /// <summary>
        /// Mã trạng thái HTTP nhận được từ API bên ngoài.
        /// </summary>
        public int? StatusCode { get; private set; }

        /// <summary>
        /// Thời gian xử lý lệnh gọi (tính theo mili-giây).
        /// </summary>
        public long DurationMs { get; private set; }

        /// <summary>
        /// Cờ xác định cuộc gọi có thành công hay không.
        /// </summary>
        public bool IsSuccess { get; private set; }

        /// <summary>
        /// Thông điệp lỗi chi tiết nếu cuộc gọi thất bại.
        /// </summary>
        public string? ErrorMessage { get; private set; }

        // ── Navigation ──

        /// <summary>
        /// Định nghĩa công cụ tương ứng.
        /// </summary>
        public virtual ToolDefinition? ToolDefinition { get; private set; }
    }
}

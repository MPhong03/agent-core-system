// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Shared.Kernel.Primitives;

namespace AgentCoreSystem.Module.Agents.Domain.Entities
{
    /// <summary>
    /// Thực thể ghi nhận lịch sử thực thi mã nguồn Python của Agent phục vụ audit, tracing và review.
    /// </summary>
    public class ScriptExecutionHistory : BaseEntity
    {
        // ── Constructors ──

        /// <summary>
        /// Khởi tạo phục vụ Entity Framework Core deserialization.
        /// </summary>
        protected ScriptExecutionHistory() : base()
        {
            CallerType = string.Empty;
            ScriptName = string.Empty;
            ScriptContent = string.Empty;
        }

        private ScriptExecutionHistory(
            Guid agentProfileId,
            string callerType,
            Guid executionSourceId,
            string scriptName,
            string scriptContent,
            int exitCode,
            string? stdout,
            string? stderr,
            long durationMs,
            bool isTimeout,
            string? artifactsJson) : base()
        {
            AgentProfileId = agentProfileId;
            CallerType = callerType;
            ExecutionSourceId = executionSourceId;
            ScriptName = scriptName;
            ScriptContent = scriptContent;
            ExitCode = exitCode;
            Stdout = stdout;
            Stderr = stderr;
            DurationMs = durationMs;
            IsTimeout = isTimeout;
            ArtifactsJson = artifactsJson;
        }

        // ── Properties ──

        /// <summary>
        /// Định danh Agent thực hiện chạy script.
        /// </summary>
        public Guid AgentProfileId { get; private set; }

        /// <summary>
        /// Nguồn gốc kích hoạt thực thi ('Chat', 'Task', 'Manual').
        /// </summary>
        public string CallerType { get; private set; }

        /// <summary>
        /// Định danh thực thể kích hoạt (ChatMessageId, TaskId, hoặc AgentProfileId).
        /// </summary>
        public Guid ExecutionSourceId { get; private set; }

        /// <summary>
        /// Tên file script (ví dụ: 'analyze_sales.py').
        /// </summary>
        public string ScriptName { get; private set; }

        /// <summary>
        /// Nội dung mã nguồn Python đã thực thi.
        /// </summary>
        public string ScriptContent { get; private set; }

        /// <summary>
        /// Mã thoát của tiến trình (0 là thành công, khác 0 là lỗi).
        /// </summary>
        public int ExitCode { get; private set; }

        /// <summary>
        /// Đầu ra chuẩn (Standard Output) từ tiến trình Python.
        /// </summary>
        public string? Stdout { get; private set; }

        /// <summary>
        /// Đầu ra lỗi (Standard Error) từ tiến trình Python.
        /// </summary>
        public string? Stderr { get; private set; }

        /// <summary>
        /// Thời gian thực thi tính theo mili-giây (ms).
        /// </summary>
        public long DurationMs { get; private set; }

        /// <summary>
        /// Cờ đánh dấu tiến trình có bị ngắt do vượt quá timeout hay không.
        /// </summary>
        public bool IsTimeout { get; private set; }

        /// <summary>
        /// Chuỗi JSON danh sách artifacts mới được sinh ra sau khi chạy.
        /// </summary>
        public string? ArtifactsJson { get; private set; }

        // ── Factory Methods ──

        /// <summary>
        /// Tạo mới một bản ghi lịch sử thực thi mã nguồn.
        /// </summary>
        public static Result<ScriptExecutionHistory> Create(
            Guid agentProfileId,
            string callerType,
            Guid executionSourceId,
            string scriptName,
            string scriptContent,
            int exitCode,
            string? stdout,
            string? stderr,
            long durationMs,
            bool isTimeout,
            string? artifactsJson)
        {
            if (agentProfileId == Guid.Empty)
            {
                return Result.Failure<ScriptExecutionHistory>(Error.Validation("ScriptExecutionHistory.InvalidAgentId", "AgentProfileId không được để trống."));
            }

            if (string.IsNullOrWhiteSpace(scriptName))
            {
                return Result.Failure<ScriptExecutionHistory>(Error.Validation("ScriptExecutionHistory.EmptyScriptName", "Tên script không được để trống."));
            }

            var history = new ScriptExecutionHistory(
                agentProfileId,
                string.IsNullOrWhiteSpace(callerType) ? "Manual" : callerType,
                executionSourceId == Guid.Empty ? agentProfileId : executionSourceId,
                scriptName,
                scriptContent ?? string.Empty,
                exitCode,
                stdout,
                stderr,
                Math.Max(0, durationMs),
                isTimeout,
                artifactsJson
            );

            return Result.Success(history);
        }
    }
}

// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Shared.Kernel.Primitives;

namespace AgentCoreSystem.Module.Agents.Application.Services.Python
{
    /// <summary>
    /// Giao diện thực thi mã nguồn Python và quản lý gói thư viện trong môi trường ảo (.venv) cách ly của Agent.
    /// </summary>
    public interface IPythonSandboxRunner
    {
        /// <summary>
        /// Đảm bảo môi trường ảo .venv đã được tạo cho Agent.
        /// </summary>
        Task<Result<string>> EnsureVenvInitializedAsync(Guid agentProfileId, CancellationToken ct = default);

        /// <summary>
        /// Cài đặt các gói thư viện Python qua pip vào .venv của Agent.
        /// </summary>
        Task<Result<string>> InstallPackagesAsync(Guid agentProfileId, IEnumerable<string> packages, CancellationToken ct = default);

        /// <summary>
        /// Lấy danh sách các gói thư viện đã cài đặt trong môi trường ảo của Agent.
        /// </summary>
        Task<Result<IReadOnlyList<string>>> ListInstalledPackagesAsync(Guid agentProfileId, CancellationToken ct = default);

        /// <summary>
        /// Thực thi script Python với cơ chế giới hạn thời gian (Timeout) và làm sạch biến môi trường (Environment Stripping).
        /// </summary>
        Task<Result<PythonExecutionResult>> ExecuteScriptAsync(PythonExecutionRequest request, CancellationToken ct = default);
    }
}

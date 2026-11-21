// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace AgentCoreSystem.Module.Tasks.Application.Contracts
{
    /// <summary>
    /// Giao diện thực thi ngầm cho tác vụ được giao cho Nhân sự AI qua Hangfire.
    /// </summary>
    public interface ITaskAgentJobRunner
    {
        /// <summary>
        /// Thực thi tác vụ bất đồng bộ trong hàng đợi Hangfire.
        /// </summary>
        Task ExecuteTaskAsync(Guid taskId, CancellationToken cancellationToken = default);
    }
}

// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace AgentCoreSystem.Module.Agents.Presentation.Contracts
{
    /// <summary>
    /// Payload yêu cầu thực thi script Python thử nghiệm từ giao diện quản trị.
    /// </summary>
    public record ExecuteScriptApiRequest(
        string ScriptContent,
        string? ScriptName = null,
        int? TimeoutSeconds = null
    );

    /// <summary>
    /// Payload yêu cầu cài đặt danh sách packages qua pip.
    /// </summary>
    public record InstallPackagesApiRequest(
        IReadOnlyList<string> Packages
    );
}

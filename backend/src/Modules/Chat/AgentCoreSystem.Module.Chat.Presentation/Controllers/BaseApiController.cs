// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;

namespace AgentCoreSystem.Module.Chat.Presentation.Controllers
{
    /// <summary>
    /// Base API Controller cho phân hệ Chat.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Lớp cơ sở cung cấp cấu hình định tuyến API Controller và định dạng phản hồi JSON chuẩn.</para>
    /// <para><strong>When:</strong> Tất cả các Controller trong phân hệ Chat kế thừa từ lớp này.</para>
    /// <para><strong>Why:</strong> Tập trung hóa các thuộc tính chuẩn RESTful và giảm thiểu mã lặp lại.</para>
    /// </remarks>
    [ApiController]
    [Produces("application/json")]
    public abstract class BaseApiController : ControllerBase
    {
    }
}

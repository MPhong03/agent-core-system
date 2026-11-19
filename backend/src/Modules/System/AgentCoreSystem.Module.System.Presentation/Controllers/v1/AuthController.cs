// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using AgentCoreSystem.Module.System.Application.Auth.Commands.Login;
using AgentCoreSystem.Module.System.Presentation.Extensions;
using AgentCoreSystem.Shared.Kernel.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgentCoreSystem.Module.System.Presentation.Controllers.v1
{
    /// <summary>
    /// Phân hệ Xác thực &amp; Quản lý Phiên đăng nhập (Phiên bản API 1.0).
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/auth")]
    [Produces("application/json")]
    public class AuthController(ISender mediator) : ControllerBase
    {
        /// <summary>
        /// Đăng nhập hệ thống bằng Email và Mật khẩu.
        /// </summary>
        /// <remarks>
        /// Tiếp nhận thông tin định danh người dùng và trả về Token xác thực kèm thông tin vai trò.
        /// </remarks>
        /// <param name="command">Thông tin đăng nhập.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <response code="200">Đăng nhập thành công.</response>
        /// <response code="400">Dữ liệu yêu cầu không hợp lệ.</response>
        /// <response code="401">Tài khoản hoặc mật khẩu không chính xác.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(command, cancellationToken);
            return result.IsSuccess 
                ? Ok(ApiResponse<AuthResponseDto>.Ok(result.Value, "Đăng nhập thành công.")) 
                : result.ToProblemDetails(this);
        }
    }
}


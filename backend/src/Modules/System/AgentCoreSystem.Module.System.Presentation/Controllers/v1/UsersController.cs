// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using AgentCoreSystem.Module.System.Application.Users.Commands.ChangePassword;
using AgentCoreSystem.Module.System.Application.Users.Commands.CreateUser;
using AgentCoreSystem.Module.System.Application.Users.Commands.DeleteUser;
using AgentCoreSystem.Module.System.Application.Users.Commands.UpdateUser;
using AgentCoreSystem.Module.System.Application.Users.DTOs;
using AgentCoreSystem.Module.System.Application.Users.Queries.GetUserById;
using AgentCoreSystem.Module.System.Application.Users.Queries.GetUsers;
using AgentCoreSystem.Module.System.Presentation.Extensions;
using AgentCoreSystem.Shared.Kernel.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgentCoreSystem.Module.System.Presentation.Controllers.v1
{
    /// <summary>
    /// Phân hệ Quản lý Người dùng &amp; Tài khoản hệ thống (Phiên bản API 1.0).
    /// </summary>
    /// <remarks>
    /// Cung cấp trọn bộ các thao tác CRUD tài khoản, phân quyền vai trò (Role) và đổi mật khẩu an toàn.
    /// Toàn bộ các thao tác đều yêu cầu xác thực bằng JWT Bearer Token (SuperAdmin tự động bypass phân quyền).
    /// </remarks>
    [ApiController]
    [Authorize]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/users")]
    [Produces("application/json")]
    public class UsersController(ISender mediator) : ControllerBase
    {
        /// <summary>
        /// Truy vấn danh sách người dùng theo bộ lọc và phân trang.
        /// </summary>
        /// <remarks>
        /// ### Luồng nghiệp vụ:
        /// 1. Tiếp nhận tiêu chí lọc từ Query String (tên, email, từ khóa).
        /// 2. Áp dụng phân trang (PageIndex, PageSize).
        /// 3. Tự động loại trừ các tài khoản đã bị xóa mềm (IsDeleted = true).
        /// </remarks>
        /// <param name="query">Bộ tiêu chí tìm kiếm và tham số phân trang.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <response code="200">Truy vấn thành công. Trả về danh sách người dùng kèm metadata phân trang.</response>
        /// <response code="400">Tham số lọc hoặc phân trang không hợp lệ.</response>
        /// <response code="401">Chưa xác thực thông tin đăng nhập (thiếu hoặc token hết hạn).</response>
        /// <response code="403">Người dùng không có quyền truy cập chức năng này.</response>
        /// <response code="500">Lỗi máy chủ nội bộ trong quá trình xử lý.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUsers([FromQuery] GetUsersQuery query, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(query, cancellationToken);
            return result.IsSuccess 
                ? Ok(ApiResponse<PagedResult<UserDto>>.Ok(result.Value)) 
                : result.ToProblemDetails(this);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một người dùng theo mã định danh UUIDv7.
        /// </summary>
        /// <remarks>
        /// ### Luồng nghiệp vụ:
        /// - Tìm kiếm người dùng trong cơ sở dữ liệu dựa trên Id.
        /// - Trả về đầy đủ thông tin cá nhân, vai trò, ngày tạo và ngày cập nhật.
        /// </remarks>
        /// <param name="id">Mã định danh UUIDv7 của tài khoản.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <response code="200">Truy vấn thành công. Trả về chi tiết người dùng.</response>
        /// <response code="401">Chưa xác thực đăng nhập.</response>
        /// <response code="404">Không tìm thấy tài khoản người dùng với Id cung cấp.</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new GetUserByIdQuery(id), cancellationToken);
            return result.IsSuccess 
                ? Ok(ApiResponse<UserDetailDto>.Ok(result.Value)) 
                : result.ToProblemDetails(this);
        }

        /// <summary>
        /// Tạo mới một tài khoản người dùng trong hệ thống (Tích hợp mã hóa mật khẩu PBKDF2).
        /// </summary>
        /// <remarks>
        /// ### Luồng nghiệp vụ:
        /// 1. Kiểm tra tính duy nhất của Email.
        /// 2. Tạo mới tài khoản sử dụng thuật toán khóa UUIDv7.
        /// 3. Mã hóa mật khẩu bằng PBKDF2 HMAC-SHA512 với Salt ngẫu nhiên 128-bit (100.000 iterations).
        /// 4. Gán các vai trò mặc định (hoặc theo danh sách truyền vào).
        /// </remarks>
        /// <param name="command">Dữ liệu thông tin tạo mới người dùng.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <response code="201">Tạo mới tài khoản thành công. Trả về Id định danh UUIDv7 của người dùng.</response>
        /// <response code="400">Dữ liệu đầu vào vi phạm quy tắc xác thực (Validation Error).</response>
        /// <response code="401">Chưa xác thực quyền truy cập.</response>
        /// <response code="409">Email đã tồn tại trong hệ thống (Conflict Error).</response>
        /// <response code="500">Lỗi máy chủ nội bộ.</response>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(command, cancellationToken);
            return result.IsSuccess 
                ? StatusCode(StatusCodes.Status201Created, ApiResponse<Guid>.Ok(result.Value, "Tạo mới tài khoản thành công.")) 
                : result.ToProblemDetails(this);
        }

        /// <summary>
        /// Cập nhật thông tin cá nhân và vai trò của tài khoản người dùng.
        /// </summary>
        /// <remarks>
        /// ### Luồng nghiệp vụ:
        /// 1. Kiểm tra sự tồn tại của tài khoản và đảm bảo chưa bị xóa mềm.
        /// 2. Cập nhật họ tên, số điện thoại, trạng thái hoạt động/khóa.
        /// 3. Điều chỉnh danh sách vai trò (Roles) được phân bổ.
        /// </remarks>
        /// <param name="id">Mã định danh UUIDv7 của người dùng.</param>
        /// <param name="command">Dữ liệu cần cập nhật.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <response code="200">Cập nhật thành công.</response>
        /// <response code="400">Dữ liệu không hợp lệ.</response>
        /// <response code="401">Chưa xác thực.</response>
        /// <response code="404">Không tìm thấy người dùng.</response>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserCommand command, CancellationToken cancellationToken)
        {
            if (id != command.Id)
            {
                return BadRequest(ProblemDetailsExtensions.CreateMismatchProblem(this, "Id trên URL và Id trong body không trùng khớp."));
            }

            var result = await mediator.Send(command, cancellationToken);
            return result.IsSuccess 
                ? Ok(ApiResponse<string>.Ok("Cập nhật tài khoản thành công.")) 
                : result.ToProblemDetails(this);
        }

        /// <summary>
        /// Xóa mềm (Soft Delete) một tài khoản người dùng khỏi hệ thống.
        /// </summary>
        /// <remarks>
        /// ### Luồng nghiệp vụ:
        /// 1. Tìm tài khoản theo Id.
        /// 2. Chuyển cờ `IsDeleted = true`, lưu thời điểm `DeletedDate = UtcNow`, khóa tài khoản `IsActive = false`.
        /// 3. Không xóa vật lý để bảo đảm toàn vẹn dữ liệu kiểm toán.
        /// </remarks>
        /// <param name="id">Mã định danh UUIDv7 của tài khoản cần xóa.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <response code="200">Xóa mềm tài khoản thành công.</response>
        /// <response code="401">Chưa xác thực.</response>
        /// <response code="404">Không tìm thấy tài khoản người dùng.</response>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new DeleteUserCommand(id), cancellationToken);
            return result.IsSuccess 
                ? Ok(ApiResponse<string>.Ok("Xóa tài khoản thành công.")) 
                : result.ToProblemDetails(this);
        }

        /// <summary>
        /// Thay đổi mật khẩu tài khoản người dùng.
        /// </summary>
        /// <remarks>
        /// ### Luồng nghiệp vụ:
        /// 1. Xác thực tính chính xác của mật khẩu hiện tại (CurrentPassword).
        /// 2. Băm mật khẩu mới bằng PBKDF2 với Salt mới 128-bit.
        /// 3. Cập nhật hash mới vào cơ sở dữ liệu.
        /// </remarks>
        /// <param name="id">Mã định danh UUIDv7 của tài khoản.</param>
        /// <param name="command">Thông tin mật khẩu cũ và mật khẩu mới.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <response code="200">Đổi mật khẩu thành công.</response>
        /// <response code="400">Mật khẩu mới không hợp lệ hoặc mật khẩu cũ không đúng.</response>
        /// <response code="401">Chưa xác thực đăng nhập.</response>
        /// <response code="404">Không tìm thấy tài khoản người dùng.</response>
        [HttpPost("{id:guid}/change-password")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordCommand command, CancellationToken cancellationToken)
        {
            if (id != command.UserId)
            {
                return BadRequest(ProblemDetailsExtensions.CreateMismatchProblem(this, "UserId trên URL và UserId trong body không trùng khớp."));
            }

            var result = await mediator.Send(command, cancellationToken);
            return result.IsSuccess 
                ? Ok(ApiResponse<string>.Ok("Đổi mật khẩu thành công.")) 
                : result.ToProblemDetails(this);
        }
    }
}


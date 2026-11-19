// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using AgentCoreSystem.Module.Organization.Application.Channels.Commands.CreateTeamChannel;
using AgentCoreSystem.Module.Organization.Application.Channels.DTOs;
using AgentCoreSystem.Module.Organization.Application.Channels.Queries.GetChannelsByDepartment;
using AgentCoreSystem.Module.Organization.Application.Departments.Commands.CreateDepartment;
using AgentCoreSystem.Module.Organization.Application.Departments.DTOs;
using AgentCoreSystem.Module.Organization.Application.Departments.Queries.GetDepartments;
using AgentCoreSystem.Module.Organization.Presentation.Extensions;
using AgentCoreSystem.Module.Organization.Presentation.Requests;
using AgentCoreSystem.Shared.Kernel.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgentCoreSystem.Module.Organization.Presentation.Controllers.v1
{
    /// <summary>
    /// Phân hệ Quản lý Phòng ban &amp; Khối chức năng doanh nghiệp (Phiên bản API 1.0).
    /// </summary>
    /// <remarks>
    /// Cung cấp các thao tác quản trị sơ đồ tổ chức, phòng ban và các kênh làm việc trực thuộc.
    /// </remarks>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/departments")]
    [Produces("application/json")]
    public class DepartmentsController(ISender mediator) : ControllerBase
    {
        /// <summary>
        /// Tạo mới một phòng ban trong tổ chức.
        /// </summary>
        /// <remarks>
        /// ### Luồng nghiệp vụ:
        /// 1. Kiểm tra tính duy nhất của tên phòng ban.
        /// 2. Tạo bản ghi phòng ban mới với khóa UUIDv7.
        /// 3. Lưu vào cơ sở dữ liệu trên schema 'org'.
        /// </remarks>
        /// <param name="request">Thông tin phòng ban cần tạo.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <response code="201">Tạo phòng ban thành công. Trả về Id định danh UUIDv7 của phòng ban.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ (Validation Error).</response>
        /// <response code="409">Tên phòng ban đã tồn tại (Conflict Error).</response>
        /// <response code="500">Lỗi máy chủ nội bộ.</response>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateDepartment(
            [FromBody] CreateDepartmentRequest request, 
            CancellationToken cancellationToken)
        {
            var command = new CreateDepartmentCommand(request.Name, request.Description);
            var result = await mediator.Send(command, cancellationToken);

            return result.IsSuccess
                ? StatusCode(StatusCodes.Status201Created, ApiResponse<Guid>.Ok(result.Value, "Tạo phòng ban thành công."))
                : result.ToProblemDetails(this);
        }

        /// <summary>
        /// Lấy danh sách toàn bộ phòng ban kèm số lượng kênh làm việc trực thuộc.
        /// </summary>
        /// <remarks>
        /// ### Luồng nghiệp vụ:
        /// - Truy vấn toàn bộ phòng ban hoạt động trong hệ thống.
        /// - Tự động đếm số lượng kênh chưa bị xóa mềm của từng phòng ban.
        /// </remarks>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <response code="200">Truy vấn thành công. Trả về danh sách phòng ban.</response>
        /// <response code="500">Lỗi máy chủ nội bộ.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DepartmentDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDepartments(CancellationToken cancellationToken)
        {
            var query = new GetDepartmentsQuery();
            var result = await mediator.Send(query, cancellationToken);

            return result.IsSuccess
                ? Ok(ApiResponse<IReadOnlyList<DepartmentDto>>.Ok(result.Value))
                : result.ToProblemDetails(this);
        }

        /// <summary>
        /// Tạo mới một kênh làm việc trong phòng ban chỉ định.
        /// </summary>
        /// <remarks>
        /// ### Luồng nghiệp vụ:
        /// 1. Kiểm tra sự tồn tại của phòng ban theo departmentId.
        /// 2. Kiểm tra tính duy nhất của tên kênh trong phạm vi phòng ban đó.
        /// 3. Khởi tạo kênh mới với khóa UUIDv7.
        /// </remarks>
        /// <param name="departmentId">Mã định danh phòng ban trực thuộc.</param>
        /// <param name="request">Thông tin kênh cần tạo.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <response code="201">Tạo kênh thành công. Trả về Id định danh UUIDv7 của kênh.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="404">Không tìm thấy phòng ban với mã cung cấp.</response>
        /// <response code="409">Tên kênh đã tồn tại trong phòng ban này.</response>
        /// <response code="500">Lỗi máy chủ nội bộ.</response>
        [HttpPost("{departmentId:guid}/channels")]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateChannel(
            [FromRoute] Guid departmentId,
            [FromBody] CreateChannelRequest request,
            CancellationToken cancellationToken)
        {
            var command = new CreateTeamChannelCommand(departmentId, request.Name, request.Description);
            var result = await mediator.Send(command, cancellationToken);

            return result.IsSuccess
                ? StatusCode(StatusCodes.Status201Created, ApiResponse<Guid>.Ok(result.Value, "Tạo kênh làm việc thành công."))
                : result.ToProblemDetails(this);
        }

        /// <summary>
        /// Lấy danh sách kênh làm việc thuộc về một phòng ban.
        /// </summary>
        /// <remarks>
        /// ### Luồng nghiệp vụ:
        /// 1. Kiểm tra sự tồn tại của phòng ban.
        /// 2. Truy vấn danh sách các kênh trực thuộc kèm số lượng thành viên đang tham gia.
        /// </remarks>
        /// <param name="departmentId">Mã định danh phòng ban.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <response code="200">Truy vấn thành công. Trả về danh sách kênh nhóm.</response>
        /// <response code="404">Không tìm thấy phòng ban với mã cung cấp.</response>
        /// <response code="500">Lỗi máy chủ nội bộ.</response>
        [HttpGet("{departmentId:guid}/channels")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TeamChannelDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetChannelsByDepartment(
            [FromRoute] Guid departmentId,
            CancellationToken cancellationToken)
        {
            var query = new GetChannelsByDepartmentQuery(departmentId);
            var result = await mediator.Send(query, cancellationToken);

            return result.IsSuccess
                ? Ok(ApiResponse<IReadOnlyList<TeamChannelDto>>.Ok(result.Value))
                : result.ToProblemDetails(this);
        }
    }
}

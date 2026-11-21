// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Tools.Commands;
using AgentCoreSystem.Module.Agents.Application.Tools.DTOs;
using AgentCoreSystem.Module.Agents.Application.Tools.Queries;
using AgentCoreSystem.Module.Agents.Presentation.Contracts;
using AgentCoreSystem.Module.Agents.Presentation.Extensions;
using AgentCoreSystem.Shared.Kernel.Responses;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgentCoreSystem.Module.Agents.Presentation.Controllers.v1
{
    /// <summary>
    /// API Controller quản lý và kiểm thử định nghĩa công cụ (Tool Definitions - No-Code Tool Builder).
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/agents/tools")]
    [Authorize]
    public class ToolsController(ISender mediator) : BaseApiController
    {
        /// <summary>
        /// Lấy danh sách toàn bộ các định nghĩa công cụ (Tool Definitions) trong hệ thống.
        /// </summary>
        /// <remarks>
        /// Các giá trị nhạy cảm trong HTTP Headers (Authorization, API Keys...) luôn được che giấu tự động dạng '••••••••'.
        /// </remarks>
        /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
        /// <returns>Danh sách thông tin các công cụ đã đăng ký.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<ToolDefinitionDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetToolDefinitions(CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new GetToolDefinitionsQuery(MaskSecrets: true), cancellationToken);
            if (result.IsFailure)
            {
                return result.ToProblemDetails(this);
            }

            return Ok(ApiResponse<List<ToolDefinitionDto>>.Ok(result.Value, "Lấy danh sách công cụ thành công."));
        }

        /// <summary>
        /// Lấy thông tin chi tiết một định nghĩa công cụ theo mã định danh duy nhất (UUIDv7).
        /// </summary>
        /// <param name="id">Mã định danh công cụ cần truy vấn.</param>
        /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
        /// <returns>Chi tiết định nghĩa công cụ kèm schema tham số và cấu hình headers đã che giấu.</returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ToolDefinitionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetToolDefinitionById(Guid id, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new GetToolDefinitionByIdQuery(id, MaskSecrets: true), cancellationToken);
            if (result.IsFailure)
            {
                return result.ToProblemDetails(this);
            }

            return Ok(ApiResponse<ToolDefinitionDto>.Ok(result.Value, "Lấy chi tiết công cụ thành công."));
        }

        /// <summary>
        /// Tạo mới một định nghĩa công cụ trong phân hệ Agents.
        /// </summary>
        /// <remarks>
        /// Hỗ trợ đăng ký công cụ REST API, tự động kiểm tra JSON Schema và lưu trữ an toàn cấu hình Headers.
        /// </remarks>
        /// <param name="request">Thông tin cấu hình công cụ mới.</param>
        /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
        /// <returns>Bản ghi định nghĩa công cụ vừa tạo.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ToolDefinitionDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateToolDefinition(
            [FromBody] CreateToolRequest request,
            CancellationToken cancellationToken)
        {
            var command = new CreateToolDefinitionCommand(
                Name: request.Name,
                DisplayName: request.DisplayName,
                Description: request.Description,
                Type: request.Type,
                EndpointUrl: request.EndpointUrl,
                HttpMethod: request.HttpMethod,
                HeadersConfig: request.HeadersConfig,
                ParametersSchema: request.ParametersSchema,
                RequiresConfirmation: request.RequiresConfirmation,
                TimeoutSeconds: request.TimeoutSeconds);

            var result = await mediator.Send(command, cancellationToken);
            if (result.IsFailure)
            {
                return result.ToProblemDetails(this);
            }

            return CreatedAtAction(
                nameof(GetToolDefinitionById),
                new { id = result.Value.Id },
                ApiResponse<ToolDefinitionDto>.Ok(result.Value, "Tạo định nghĩa công cụ thành công."));
        }

        /// <summary>
        /// Cập nhật thông tin và cấu hình của một định nghĩa công cụ hiện có.
        /// </summary>
        /// <remarks>
        /// Nếu giá trị của một HTTP Header được gửi lên là '••••••••', hệ thống sẽ tự động bảo toàn giá trị Secret ban đầu trong cơ sở dữ liệu.
        /// </remarks>
        /// <param name="id">Mã định danh công cụ cần cập nhật.</param>
        /// <param name="request">Dữ liệu cấu hình mới cần áp dụng.</param>
        /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
        /// <returns>Bản ghi công cụ sau khi đã cập nhật thành công.</returns>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ToolDefinitionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateToolDefinition(
            Guid id,
            [FromBody] UpdateToolRequest request,
            CancellationToken cancellationToken)
        {
            var command = new UpdateToolDefinitionCommand(
                Id: id,
                DisplayName: request.DisplayName,
                Description: request.Description,
                EndpointUrl: request.EndpointUrl,
                HttpMethod: request.HttpMethod,
                HeadersConfig: request.HeadersConfig,
                ParametersSchema: request.ParametersSchema,
                RequiresConfirmation: request.RequiresConfirmation,
                TimeoutSeconds: request.TimeoutSeconds);

            var result = await mediator.Send(command, cancellationToken);
            if (result.IsFailure)
            {
                return result.ToProblemDetails(this);
            }

            return Ok(ApiResponse<ToolDefinitionDto>.Ok(result.Value, "Cập nhật định nghĩa công cụ thành công."));
        }

        /// <summary>
        /// Xóa mềm (Soft Delete) một định nghĩa công cụ khỏi hệ thống.
        /// </summary>
        /// <param name="id">Mã định danh công cụ cần xóa.</param>
        /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
        /// <returns>Thông báo kết quả thao tác xóa.</returns>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteToolDefinition(Guid id, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new DeleteToolDefinitionCommand(id), cancellationToken);
            if (result.IsFailure)
            {
                return result.ToProblemDetails(this);
            }

            return Ok(ApiResponse<string>.Ok("Xóa định nghĩa công cụ thành công."));
        }

        /// <summary>
        /// Chạy thử nghiệm kiểm tra tính hoạt động của công cụ REST API đã được lưu trong cơ sở dữ liệu.
        /// </summary>
        /// <remarks>
        /// Yêu cầu gọi API được thực thi qua RestApiExecutor với cơ chế bảo vệ SSRF nhiều lớp (chặn loopback, dải IP nội bộ RFC 1918, link-local, chuyển hướng nội bộ).
        /// </remarks>
        /// <param name="id">Mã định danh công cụ cần kiểm thử.</param>
        /// <param name="request">Tham số và cấu hình giả lập khi chạy test.</param>
        /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
        /// <returns>Kết quả thực thi HTTP bao gồm StatusCode, ResponsePayload và thời gian thực thi.</returns>
        [HttpPost("{id:guid}/test")]
        [ProducesResponseType(typeof(ApiResponse<ToolTestResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> TestToolDefinition(
            Guid id,
            [FromBody] TestToolRequest? request,
            CancellationToken cancellationToken)
        {
            var command = new TestToolDefinitionCommand(
                ToolDefinitionId: id,
                HeadersConfig: request?.HeadersConfig,
                Arguments: request?.Arguments,
                ArgumentsJson: request?.ArgumentsJson,
                TimeoutSeconds: request?.TimeoutSeconds);

            var result = await mediator.Send(command, cancellationToken);
            if (result.IsFailure)
            {
                return result.ToProblemDetails(this);
            }

            return Ok(ApiResponse<ToolTestResultDto>.Ok(result.Value, "Kiểm thử công cụ thành công."));
        }

        /// <summary>
        /// Chạy thử nghiệm kiểm tra tính hoạt động của công cụ ad-hoc (trước khi lưu vào cơ sở dữ liệu).
        /// </summary>
        /// <remarks>
        /// Cho phép người dùng test nhanh API endpoint trên giao diện No-Code Tool Builder. Bảo vệ SSRF tuyệt đối.
        /// </remarks>
        /// <param name="request">Cấu hình endpoint, phương thức và tham số giả lập cần kiểm thử.</param>
        /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
        /// <returns>Kết quả thực thi HTTP từ endpoint đích.</returns>
        [HttpPost("test")]
        [ProducesResponseType(typeof(ApiResponse<ToolTestResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> TestAdHocTool(
            [FromBody] TestAdHocToolRequest request,
            CancellationToken cancellationToken)
        {
            var command = new TestToolDefinitionCommand(
                ToolDefinitionId: null,
                EndpointUrl: request.EndpointUrl,
                HttpMethod: request.HttpMethod,
                HeadersConfig: request.HeadersConfig,
                Arguments: request.Arguments,
                ArgumentsJson: request.ArgumentsJson,
                TimeoutSeconds: request.TimeoutSeconds);

            var result = await mediator.Send(command, cancellationToken);
            if (result.IsFailure)
            {
                return result.ToProblemDetails(this);
            }

            return Ok(ApiResponse<ToolTestResultDto>.Ok(result.Value, "Kiểm thử công cụ ad-hoc thành công."));
        }
    }
}

// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Organization.Application.Contracts;
using AgentCoreSystem.Module.Organization.Domain.Entities;
using AgentCoreSystem.Module.Organization.Domain.Enums;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using FluentValidation;

namespace AgentCoreSystem.Module.Organization.Application.AgentProfiles.Commands.CreateAgentProfile
{
    /// <summary>
    /// Lệnh tạo mới một hồ sơ nhân sự AI Agent.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Thiết lập định danh, lựa chọn mô hình LLM, System Prompt và tham số nhiệt độ cho Agent.</para>
    /// <para><strong>Why:</strong> Cho phép doanh nghiệp cấu hình và khởi tạo các chuyên viên AI thực hiện nhiệm vụ tự động.</para>
    /// </remarks>
    /// <param name="Name">Tên định danh của Agent.</param>
    /// <param name="AvatarUrl">Đường dẫn ảnh đại diện.</param>
    /// <param name="Provider">Nhà cung cấp LLM.</param>
    /// <param name="ModelName">Tên model kỹ thuật.</param>
    /// <param name="SystemPrompt">Chỉ dẫn hệ thống cốt lõi.</param>
    /// <param name="Temperature">Độ sáng tạo của mô hình (0.0 đến 2.0).</param>
    /// <param name="PrivateMemoryNotes">Ghi chú bộ nhớ riêng ban đầu.</param>
    public record CreateAgentProfileCommand(
        string Name,
        string? AvatarUrl,
        ModelProviderType Provider,
        string ModelName,
        string SystemPrompt,
        decimal Temperature = 0.7m,
        string? PrivateMemoryNotes = null) : ICommand<Guid>;

    /// <summary>
    /// Bộ xác thực tính hợp lệ của lệnh tạo hồ sơ Agent.
    /// </summary>
    public class CreateAgentProfileCommandValidator : AbstractValidator<CreateAgentProfileCommand>
    {
        public CreateAgentProfileCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên Agent không được để trống.")
                .MaximumLength(150).WithMessage("Tên Agent không được vượt quá 150 ký tự.");

            RuleFor(x => x.ModelName)
                .NotEmpty().WithMessage("Tên mô hình (ModelName) không được để trống.")
                .MaximumLength(150).WithMessage("Tên mô hình không được vượt quá 150 ký tự.");

            RuleFor(x => x.SystemPrompt)
                .NotEmpty().WithMessage("System Prompt không được để trống.");

            RuleFor(x => x.Temperature)
                .InclusiveBetween(0.0m, 2.0m).WithMessage("Độ sáng tạo (Temperature) phải nằm trong khoảng từ 0.0 đến 2.0.");

            RuleFor(x => x.AvatarUrl)
                .MaximumLength(500).WithMessage("Đường dẫn Avatar không được vượt quá 500 ký tự.");
        }
    }

    /// <summary>
    /// Handler xử lý tạo mới hồ sơ Agent.
    /// </summary>
    /// <remarks>
    /// <para>Khởi tạo bản ghi <see cref="AgentProfile"/> với khóa chính UUIDv7 và lưu vào schema 'org'.</para>
    /// <para>AI-NOTE: Hồ sơ Agent sau khi tạo sẽ sẵn sàng để được gán vào các kênh làm việc qua <c>AssignMemberToChannelCommand</c>.</para>
    /// </remarks>
    public class CreateAgentProfileCommandHandler(IOrganizationDbContext dbContext) 
        : ICommandHandler<CreateAgentProfileCommand, Guid>
    {
        /// <inheritdoc />
        public async Task<Result<Guid>> Handle(
            CreateAgentProfileCommand request, 
            CancellationToken cancellationToken)
        {
            var agentProfile = new AgentProfile(
                request.Name,
                request.AvatarUrl,
                request.Provider,
                request.ModelName,
                request.SystemPrompt,
                request.Temperature,
                request.PrivateMemoryNotes);

            dbContext.AgentProfiles.Add(agentProfile);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success(agentProfile.Id);
        }
    }
}

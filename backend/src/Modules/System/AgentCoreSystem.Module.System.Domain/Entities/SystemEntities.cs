// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.AspNetCore.Identity;

namespace AgentCoreSystem.Module.System.Domain.Entities
{
    /// <summary>
    /// Thực thể tài khoản người dùng tích hợp Microsoft Identity.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Chứa thông tin đăng nhập, xác thực và các trường mở rộng của người dùng.</para>
    /// <para><strong>Why:</strong> Kế thừa IdentityUser với khóa Guid sinh từ UUIDv7, tích hợp sẵn các trường kiểm toán và xóa mềm.</para>
    /// </remarks>
    public class ApplicationUser : IdentityUser<Guid>, IAuditableEntity, ISoftDeletable
    {
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedDate { get; set; }
        public DateTimeOffset? DeletedDate { get; set; }
        public bool IsDeleted { get; set; } = false;

        public ApplicationUser()
        {
            Id = Guid.CreateVersion7();
            SecurityStamp = Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Thực thể vai trò (Role) tích hợp Microsoft Identity.
    /// </summary>
    public class ApplicationRole : IdentityRole<Guid>
    {
        public string? Description { get; set; }

        public ApplicationRole()
        {
            Id = Guid.CreateVersion7();
        }

        public ApplicationRole(string roleName, string? description = null) : base(roleName)
        {
            Id = Guid.CreateVersion7();
            Description = description;
        }
    }

    /// <summary>
    /// Nhật ký kiểm toán các thao tác quan trọng trên hệ thống.
    /// </summary>
    public class AuditLog : BaseEntity
    {
        public Guid? UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}


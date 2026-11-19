// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace AgentCoreSystem.Shared.Kernel.Primitives
{
    /// <summary>
    /// Giao diện đánh dấu thực thể có hỗ trợ theo dõi lịch sử tạo và cập nhật.
    /// </summary>
    /// <remarks>
    /// <strong>Why:</strong> Hỗ trợ Interceptor tự động gán giá trị thời gian UTC nhất quán trước khi lưu vào cơ sở dữ liệu.
    /// </remarks>
    public interface IAuditableEntity
    {
        DateTimeOffset CreatedDate { get; set; }
        DateTimeOffset? UpdatedDate { get; set; }
    }

    /// <summary>
    /// Giao diện đánh dấu thực thể có hỗ trợ cơ chế xóa mềm (Soft Delete).
    /// </summary>
    /// <remarks>
    /// <strong>Why:</strong> Cho phép bảo toàn dữ liệu lịch sử và phục hồi khi cần, kết hợp với Global Query Filter của EF Core.
    /// </remarks>
    public interface ISoftDeletable
    {
        DateTimeOffset? DeletedDate { get; set; }
        bool IsDeleted { get; set; }
    }

    /// <summary>
    /// Lớp thực thể cơ sở trừu tượng cung cấp khóa chính định danh và các trường kiểm toán.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Chứa khóa chính <see cref="Id"/> kiểu <typeparamref name="TId"/> cùng các trường kiểm toán vòng đời.</para>
    /// <para><strong>Why:</strong> Đảm bảo tất cả các bảng trong hệ thống đều có cấu trúc kiểm toán nhất quán và hỗ trợ xóa mềm.</para>
    /// </remarks>
    /// <typeparam name="TId">Kiểu dữ liệu của khóa chính định danh.</typeparam>
    public abstract class BaseEntity<TId> : IAuditableEntity, ISoftDeletable
    {
        /// <summary>
        /// Khóa chính định danh duy nhất của thực thể.
        /// </summary>
        public TId Id { get; protected set; } = default!;

        /// <summary>
        /// Thời điểm bản ghi được tạo ra (UTC).
        /// </summary>
        public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Thời điểm bản ghi được cập nhật lần cuối cùng (UTC), null nếu chưa từng chỉnh sửa.
        /// </summary>
        public DateTimeOffset? UpdatedDate { get; set; }

        /// <summary>
        /// Thời điểm bản ghi bị đánh dấu xóa mềm (UTC), null nếu bản ghi còn hoạt động.
        /// </summary>
        public DateTimeOffset? DeletedDate { get; set; }

        /// <summary>
        /// Trạng thái xóa mềm. True nếu bản ghi đã bị xóa logic.
        /// </summary>
        public bool IsDeleted { get; set; } = false;
    }

    /// <summary>
    /// Lớp thực thể cơ sở mặc định sử dụng khóa <see cref="Guid"/> với thuật toán UUIDv7.
    /// </summary>
    /// <remarks>
    /// <para><strong>Why UUIDv7:</strong> Sử dụng <see cref="Guid.CreateVersion7()"/> của .NET 9 để sinh khóa tăng tuần tự theo thời gian, tối ưu hóa triệt để hiệu năng Index B-Tree Clustered của SQL Server, chống phân mảnh trang đĩa khi dữ liệu tăng trưởng lớn.</para>
    /// </remarks>
    public abstract class BaseEntity : BaseEntity<Guid>
    {
        protected BaseEntity()
        {
            Id = Guid.CreateVersion7();
        }

        protected BaseEntity(Guid id)
        {
            Id = id == Guid.Empty ? Guid.CreateVersion7() : id;
        }
    }
}


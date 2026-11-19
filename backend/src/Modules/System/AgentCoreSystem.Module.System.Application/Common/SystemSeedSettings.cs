// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

namespace AgentCoreSystem.Module.System.Application.Common
{
    /// <summary>
    /// Lớp đóng gói cấu hình khởi tạo dữ liệu mẫu (Data Seeding) cho phân hệ System.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Chứa các tham số cấu hình bật/tắt seed và thông tin tài khoản SuperAdmin ban đầu.</para>
    /// <para><strong>When to use:</strong> Được nạp khi khởi động ứng dụng và phục vụ dịch vụ SystemDataSeeder.</para>
    /// <para><strong>Why:</strong> Loại bỏ hoàn toàn việc hardcode email và mật khẩu tài khoản quản trị trong mã nguồn C#, tuân thủ nguyên tắc Fail-Fast và Zero Hardcoded Secrets.</para>
    /// </remarks>
    public class SystemSeedSettings
    {
        public const string SectionName = "SeedSettings";

        /// <summary>Cờ cho phép tự động kiểm tra và khởi tạo dữ liệu mẫu khi khởi động.</summary>
        public bool EnableSeeding { get; set; }

        /// <summary>Thông tin tài khoản SuperAdmin ban đầu.</summary>
        public SuperAdminSeedSettings SuperAdmin { get; set; } = new();
    }

    /// <summary>
    /// Thông tin tài khoản SuperAdmin phục vụ khởi tạo ban đầu.
    /// </summary>
    public class SuperAdminSeedSettings
    {
        /// <summary>Địa chỉ email đăng nhập của SuperAdmin (bắt buộc khi EnableSeeding = true).</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Mật khẩu khởi tạo ban đầu (bắt buộc khi EnableSeeding = true, tối thiểu 6 ký tự).</summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>Họ và tên hiển thị của SuperAdmin.</summary>
        public string FullName { get; set; } = string.Empty;
    }
}


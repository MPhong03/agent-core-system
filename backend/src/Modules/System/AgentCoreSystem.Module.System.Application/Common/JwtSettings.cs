// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

namespace AgentCoreSystem.Module.System.Application.Common
{
    /// <summary>
    /// Lớp đóng gói cấu hình thông số kỹ thuật cho dịch vụ JSON Web Token (JWT) theo Options Pattern.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Chứa các tham số cấu hình Secret, Issuer, Audience và ExpiryMinutes được nạp từ Configuration.</para>
    /// <para><strong>When to use:</strong> Được sử dụng bởi JwtTokenGenerator và dịch vụ cấu hình Authentication tại Host API.</para>
    /// <para><strong>Why:</strong> Tuân thủ nguyên tắc Zero Hardcoded Secrets và Fail-Fast, tuyệt đối không gán giá trị fallback mặc định trong mã nguồn.</para>
    /// </remarks>
    public class JwtSettings
    {
        public const string SectionName = "JwtSettings";

        /// <summary>Khóa bí mật ký JWT đối xứng (bắt buộc cấu hình từ môi trường/appsettings, tối thiểu 256 bits / 32 ký tự).</summary>
        public string Secret { get; set; } = string.Empty;

        /// <summary>Đơn vị phát hành token hợp lệ.</summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>Đối tượng tiếp nhận token hợp lệ.</summary>
        public string Audience { get; set; } = string.Empty;

        /// <summary>Thời gian sống của Access Token tính bằng phút (phải lớn hơn 0).</summary>
        public int ExpiryMinutes { get; set; }
    }
}


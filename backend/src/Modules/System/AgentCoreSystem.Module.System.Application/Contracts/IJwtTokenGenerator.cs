// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using AgentCoreSystem.Module.System.Domain.Entities;

namespace AgentCoreSystem.Module.System.Application.Contracts
{
    /// <summary>
    /// Giao diện dịch vụ phát hành JSON Web Token (JWT) cho người dùng hệ thống.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Chịu trách nhiệm tạo Access Token (JWT) và Refresh Token phục vụ xác thực không trạng thái (Stateless Authentication).</para>
    /// <para><strong>Why:</strong> Đóng gói logic sinh token vào abstraction tại tầng Application để Handlers (Login, Refresh) không bị phụ thuộc trực tiếp vào thư viện System.IdentityModel.Tokens.Jwt.</para>
    /// </remarks>
    public interface IJwtTokenGenerator
    {
        /// <summary>
        /// Tạo mới một chuỗi JWT Access Token mang thông tin định danh và quyền hạn của người dùng.
        /// </summary>
        /// <remarks>
        /// <strong>Why:</strong> Payload chứa các Claim tiêu chuẩn gồm `sub` (UUIDv7 của User), `email`, `jti`, `name`, và danh sách các vai trò `role` để Client và Middleware phân quyền nhanh chóng mà không cần query Database lại.
        /// </remarks>
        /// <param name="user">Thực thể người dùng đã được xác thực thành công.</param>
        /// <param name="roles">Danh sách các vai trò được phân bổ cho người dùng.</param>
        /// <returns>Chuỗi JWT Token hợp lệ đã được ký số HMAC-SHA256.</returns>
        string GenerateToken(ApplicationUser user, IEnumerable<string> roles);

        /// <summary>
        /// Tạo một chuỗi Refresh Token ngẫu nhiên an toàn dùng để gia hạn phiên làm việc.
        /// </summary>
        string GenerateRefreshToken();
    }
}


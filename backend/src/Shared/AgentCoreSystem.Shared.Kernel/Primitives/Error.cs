// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

namespace AgentCoreSystem.Shared.Kernel.Primitives
{
    /// <summary>
    /// Định nghĩa các nhóm lỗi nghiệp vụ phục vụ việc ánh xạ tập trung sang mã trạng thái HTTP chuẩn.
    /// </summary>
    /// <remarks>
    /// <strong>Why:</strong> Giúp tầng Presentation ánh xạ chính xác và đồng nhất từ lỗi nghiệp vụ sang HTTP Status Code và ProblemDetails.
    /// </remarks>
    public enum ErrorType
    {
        Failure = 0,       // Lỗi logic nghiệp vụ chung (400 Bad Request)
        Validation = 1,    // Lỗi vi phạm dữ liệu đầu vào (400 Bad Request)
        NotFound = 2,      // Không tìm thấy tài nguyên (404 Not Found)
        Conflict = 3,      // Xung đột trạng thái / Trùng lặp Unique (409 Conflict)
        Unauthorized = 4,  // Chưa xác thực định danh (401 Unauthorized)
        Forbidden = 5,     // Không có quyền truy cập (403 Forbidden)
        Unexpected = 6     // Lỗi bất thường hệ thống (500 Internal Server Error)
    }

    /// <summary>
    /// Đối tượng đóng gói thông tin lỗi chuẩn mực của toàn hệ thống.
    /// </summary>
    public record Error(string Code, string Description, ErrorType Type = ErrorType.Failure)
    {
        public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);
        public static readonly Error NullValue = new("General.NullValue", "Giá trị truyền vào không được phép null.", ErrorType.Validation);

        public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);
        public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);
        public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);
        public static Error Unauthorized(string code, string description) => new(code, description, ErrorType.Unauthorized);
        public static Error Forbidden(string code, string description) => new(code, description, ErrorType.Forbidden);
        public static Error Unexpected(string code, string description) => new(code, description, ErrorType.Unexpected);
    }
}


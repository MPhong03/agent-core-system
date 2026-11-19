// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace AgentCoreSystem.Shared.Kernel.Responses
{
    /// <summary>
    /// Định dạng phản hồi chuẩn cho toàn bộ các API endpoint trong hệ thống.
    /// </summary>
    /// <remarks>
    /// <strong>Why:</strong> Đảm bảo client (Angular, Mobile) luôn nhận được một cấu trúc phản hồi đồng nhất.
    /// </remarks>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public string? ErrorCode { get; set; }
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        public static ApiResponse<T> Ok(T data, string message = "Thành công") => new()
        {
            Success = true,
            Message = message,
            Data = data
        };

        public static ApiResponse<T> Fail(string errorCode, string message) => new()
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message,
            Data = default
        };
    }

    /// <summary>
    /// Đối tượng đóng gói danh sách dữ liệu có phân trang kèm metadata.
    /// </summary>
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
        public int PageIndex { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public PagedResult(IReadOnlyList<T> items, int count, int pageIndex, int pageSize)
        {
            Items = items;
            TotalCount = count;
            PageIndex = pageIndex;
            PageSize = pageSize;
        }
    }
}


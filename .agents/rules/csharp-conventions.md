# C# Coding Conventions & Standards

Tài liệu quy định chi tiết quy chuẩn viết mã C# áp dụng cho toàn bộ các project Backend trong Solution.

---

## 1. Bản quyền & Thông tin Tác giả (Copyright Header)
Chỉ áp dụng tại các **file kiến trúc cốt lõi/quan trọng** (Building Blocks, Base Classes, DbContext, Key Interceptors/Services, Root configurations):

```csharp
// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------
```

---

## 2. Block-scoped Namespaces
**BẮT BUỘC** sử dụng cú pháp block-scoped namespaces với cặp dấu ngoặc ôm `{ ... }`. Tuyệt đối không dùng file-scoped namespace (`namespace X;`):

```csharp
// ĐÚNG:
namespace AgentCoreSystem.Module.System.Application.Users.Commands
{
    public class CreateUserCommand : ICommand<Result<Guid>>
    {
        // ...
    }
}

// SAI (KHÔNG ĐƯỢC DÙNG):
namespace AgentCoreSystem.Module.System.Application.Users.Commands;
```

---

## 3. C# Primary Constructors
Khi một class tiếp nhận các dependencies qua Dependency Injection hoặc nhận các tham số khởi tạo bất biến, **BẮT BUỘC** sử dụng cú pháp **Primary Constructor** của C# 12+:

```csharp
// ĐÚNG:
public class MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger) : ICacheService
{
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        // Sử dụng trực tiếp memoryCache, logger
    }
}

// KHÔNG NÊN: Viết constructor truyền thống với nhiều dòng gán _field = field
```

---

## 4. Quy tắc Comment XML: What - When - Why
Mọi class, interface, method nghiệp vụ và MediatR Handler đều phải có XML Documentation đầy đủ:
- **What (Thẻ `<summary>`)**: Mô tả ngắn gọn đây là đối tượng gì, làm nhiệm vụ gì.
- **When & Why (Thẻ `<remarks>`)**:
  - `When`: Khi nào thì dùng đối tượng/phương thức này.
  - `Why`: Lý do kiến trúc hoặc kỹ thuật đằng sau cách thiết kế này.
- **Thẻ `<param>` và `<returns>`**: Giải thích cụ thể từng tham số đầu vào và kiểu dữ liệu trả về.

```csharp
/// <summary>
/// Dịch vụ quản lý bộ nhớ đệm cục bộ ứng dụng.
/// </summary>
/// <remarks>
/// <para><strong>When to use:</strong> Dùng cho các dữ liệu ít biến động, đọc thường xuyên và hệ thống triển khai single instance.</para>
/// <para><strong>Why:</strong> Đạt tốc độ truy xuất cực nhanh (RAM in-process) và cách ly UseCase thông qua giao diện ICacheService để chuyển sang Redis khi cần.</para>
/// </remarks>
```


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

## 4. Quy tắc Comment XML & Code Annotations
Mọi class, interface, property, method `public` và `internal`, cũng như MediatR Handlers **BẮT BUỘC** phải có XML Documentation và tuân thủ các nguyên tắc sau:

### 4.1. Cấu trúc Thẻ XML Doc Chuẩn
- **`<summary>`**: Mô tả ngắn gọn đối tượng/thành viên (What).
- **`<remarks>`**: Giải thích chi tiết **lý do thiết kế (Why)** và **ràng buộc kỹ thuật/nghiệp vụ (When/Constraints)**.
- **`<param>` & `<returns>`**: Mô tả cụ thể từng tham số đầu vào và kết quả trả về.
- **`<see cref="..."/>`**: Liên kết đến class/interface/method liên quan.
- **`<c>...</c>`**: Đánh dấu các đoạn code, giá trị hằng hoặc biểu thức inline.
- **`<inheritdoc />`**: Sử dụng khi hiện thực hóa interface hoặc override method từ base class mà không thay đổi ngữ nghĩa nghiệp vụ.

### 4.2. Ngôn ngữ & Phong cách Viết Comment
- **Ngôn ngữ**: Toàn bộ nội dung viết bằng **tiếng Việt**, các **thuật ngữ kỹ thuật giữ nguyên tiếng Anh** (vd: *pipeline behavior*, *handler*, *soft delete*, *primary constructor*, *audit log*).
- **Tập trung vào Lý do & Ràng buộc**: **KHÔNG diễn giải lại code** (tránh viết lại những gì code đã thể hiện rõ). Comment phải nêu bật được lý do kiến trúc hoặc ràng buộc tiềm ẩn.
  - *Ví dụ chuẩn:* `«Enricher điền sau phân trang ⇒ không lọc/sắp được»` thay vì `«Hàm này để điền thêm dữ liệu»`.

### 4.3. Các Ký Hiệu Đánh Dấu Đặc Biệt
Sử dụng các ký hiệu trực quan để nhấn mạnh ngữ cảnh:
- 🔴 : Cảnh báo quan trọng — «đừng đổi cái này vì...» (nguy cơ breaking changes, lỗi hiệu năng hoặc race condition).
- ⛔ : Điều cấm — hành vi bị nghiêm cấm trong logic này.
- ⇒ : Quan hệ kéo theo / hệ quả logic (ví dụ: *soft delete ⇒ phải qua query filter*).

### 4.4. Các Dấu Hiệu Phân Nhóm & Ghi Chú AI
- **`AI-NOTE:`**: Ghi chú dành riêng cho AI Agent hoặc Developer bảo trì đọc sau (giải thích ngữ cảnh ngầm, nhắc nhở điểm mở rộng hoặc lưu ý khi refactor).
- **`// ── Tiêu đề nhóm ──`**: Sử dụng để chia các nhóm property hoặc method liên quan trong class.

### 4.5. Ví dụ Minh Họa Chuẩn
```csharp
/// <summary>
/// Dịch vụ quản lý bộ nhớ đệm cục bộ ứng dụng.
/// </summary>
/// <remarks>
/// <para>Dùng cho các dữ liệu ít biến động, đọc thường xuyên và hệ thống triển khai single instance.</para>
/// <para>🔴 <strong>Lưu ý:</strong> In-process cache không đồng bộ giữa nhiều node server ⇒ Khi scale-out bắt buộc chuyển sang <see cref="IDistributedCache"/>.</para>
/// <para>⛔ <strong>Cấm:</strong> Không lưu các object có dung lượng lớn hơn 10MB vào cache này.</para>
/// </remarks>
public class MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger) : ICacheService
{
    // ── Configuration & Metadata ──

    /// <summary>
    /// Thời gian sống mặc định của cache item.
    /// </summary>
    /// <remarks>Thời gian ngắn để tránh stale data ⇒ 5 phút.</remarks>
    public TimeSpan DefaultExpiration { get; } = TimeSpan.FromMinutes(5);

    // ── Cache Operations ──

    /// <summary>
    /// Lấy dữ liệu từ cache hoặc nạp mới nếu chưa tồn tại.
    /// </summary>
    /// <param name="key">Khóa định danh cache.</param>
    /// <param name="factory">Hàm khởi tạo dữ liệu khi cache miss.</param>
    /// <param name="cancellationToken">Token hủy tác vụ.</param>
    /// <returns>Đối tượng kiểu <typeparamref name="T"/> đã được cache.</returns>
    /// <remarks>
    /// AI-NOTE: Sử dụng SemaphoreSlim nội bộ trong factory để chống Cache Stampede khi có nhiều request đồng thời.
    /// </remarks>
    public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, CancellationToken cancellationToken = default)
    {
        // ...
    }
}
```


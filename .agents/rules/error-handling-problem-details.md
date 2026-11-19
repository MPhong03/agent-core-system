# Error Handling & Problem Details Standards

Tài liệu quy định cơ chế quản lý lỗi tập trung, phân loại lỗi nghiệp vụ và chuẩn hóa phản hồi theo RFC 7807 Problem Details.

---

## 1. Phân loại Lỗi Nghiệp vụ (ErrorType)
Tất cả các lỗi nghiệp vụ trong hệ thống đều được định danh thông qua record `Error` và phân loại theo enum `ErrorType`:

| ErrorType | Ý nghĩa | HTTP Status Code | RFC Type URI |
| :--- | :--- | :--- | :--- |
| **Validation** | Dữ liệu đầu vào không hợp lệ hoặc vi phạm validator | 400 Bad Request | `https://tools.ietf.org/html/rfc7231#section-6.5.1` |
| **NotFound** | Không tìm thấy tài nguyên theo Id hoặc điều kiện | 404 Not Found | `https://tools.ietf.org/html/rfc7231#section-6.5.4` |
| **Conflict** | Xung đột trạng thái, trùng lặp Unique Index (vd: Email) | 409 Conflict | `https://tools.ietf.org/html/rfc7231#section-6.5.8` |
| **Unauthorized**| Chưa đăng nhập hoặc Access Token không hợp lệ | 401 Unauthorized | `https://tools.ietf.org/html/rfc7235#section-3.1` |
| **Forbidden** | Người dùng không đủ quyền hạn (Role/Permission) | 403 Forbidden | `https://tools.ietf.org/html/rfc7231#section-6.5.3` |
| **Unexpected** | Lỗi nội bộ hoặc ngoại lệ hệ thống ngoài ý muốn | 500 Internal Server Error | `https://tools.ietf.org/html/rfc7231#section-6.6.1` |

---

## 2. Quy tắc Xử lý trong Application Layer (CQRS Handlers)
- Handlers không được quăng `Exception` cho các lỗi nghiệp vụ thông thường (Business Failures).
- Luôn trả về `Result<T>` hoặc `Result`:
  ```csharp
  if (userExists)
  {
      return Result.Failure<Guid>(Error.Conflict("User.EmailConflict", "Email này đã được sử dụng."));
  }
  ```

---

## 3. Quy tắc Controller: Ánh xạ sang ProblemDetails
Trong Presentation Controllers, sử dụng tiện ích mở rộng `ToProblemDetails()`:
```csharp
[HttpPost]
public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command, CancellationToken ct)
{
    var result = await mediator.Send(command, ct);
    return result.IsSuccess 
        ? CreatedAtAction(nameof(GetUserById), new { id = result.Value }, ApiResponse<Guid>.Success(result.Value)) 
        : result.ToProblemDetails(this);
}
```

---

## 4. Xử lý Ngoại lệ Toàn cục (Global Exception Handler)
- Sử dụng `IExceptionHandler` của .NET 9 để bắt tất cả các Unhandled Exception chưa được kiểm soát.
- Log chi tiết stack trace kèm `traceId` (`HttpContext.TraceIdentifier`).
- Định dạng trả về client với `Content-Type: application/problem+json`:
  ```json
  {
    "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
    "title": "Lỗi Máy Chủ Nội Bộ",
    "status": 500,
    "detail": "Đã xảy ra sự cố trong quá trình xử lý.",
    "instance": "/api/v1/users",
    "traceId": "0HN12345678:00000001",
    "timestamp": "2026-11-17T20:00:00Z"
  }
  ```

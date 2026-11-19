# OpenAPI Specification (OAS) Rules

Tài liệu quy định tiêu chuẩn tài liệu hóa API Controllers và DTOs theo chuẩn OpenAPI v3.

---

## 1. Cấu trúc Chú thích Controller Action
Mọi endpoint trong API Controllers bắt buộc phải có tài liệu hóa đầy đủ:

1. **`<summary>`**: Tiêu đề ngắn gọn của endpoint (hiển thị trên Swagger UI).
2. **`<remarks>`**: Mô tả chi tiết luồng nghiệp vụ, quyền hạn yêu cầu, và các lưu ý đặc thù (hỗ trợ Markdown).
3. **`<param>`**: Giải thích ý nghĩa của từng tham số (route, query string, request body).
4. **`<response code="...">`**: Giải thích từng mã trạng thái HTTP có thể trả về.
5. **`[ProducesResponseType]`**: Định nghĩa kiểu schema chính xác cho từng mã trạng thái.

```csharp
/// <summary>
/// Truy vấn danh sách tài khoản người dùng theo bộ lọc và phân trang.
/// </summary>
/// <remarks>
/// ### Luồng nghiệp vụ:
/// 1. Tiếp nhận tiêu chí lọc từ Query String.
/// 2. Áp dụng phân trang (PageIndex, PageSize).
/// 3. Bỏ qua các tài khoản đã bị xóa mềm (IsDeleted = true).
/// </remarks>
/// <param name="query">Bộ tiêu chí tìm kiếm và tham số phân trang.</param>
/// <param name="cancellationToken">CancellationToken.</param>
/// <response code="200">Truy vấn thành công. Trả về danh sách tài khoản.</response>
/// <response code="400">Tham số lọc không hợp lệ.</response>
/// <response code="401">Chưa xác thực thông tin đăng nhập.</response>
/// <response code="403">Không đủ quyền hạn thực hiện thao tác.</response>
[HttpGet]
[ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
```

---

## 2. API Versioning Chuẩn RESTful
- Mọi Controller phải có attribute `[ApiVersion("1.0")]` (hoặc phiên bản tương ứng).
- Route bắt buộc: `[Route("api/v{version:apiVersion}/[controller]")]`.
- Swagger Explorer được cấu hình tách riêng từng phiên bản (v1, v2...) giúp việc tích hợp phía client minh bạch và không xung đột.

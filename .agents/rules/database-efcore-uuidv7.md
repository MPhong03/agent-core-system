# Database, EF Core & UUIDv7 Rules

Tài liệu quy định cấu trúc thực thể (Entities), cơ chế khóa chính UUIDv7, Audit Log, và Soft Delete trong Entity Framework Core.

---

## 1. Khóa chính UUIDv7 với `BaseEntity`
- Tất cả các Entity nghiệp vụ trong hệ thống phải kế thừa `BaseEntity<TId>` hoặc `BaseEntity` (mặc định khóa `Guid`).
- Thuật toán **UUIDv7**:
  - Tận dụng `Guid.CreateVersion7()` được tích hợp natively trong .NET 9.
  - Các bit đầu tiên là Unix Epoch Timestamp (mili-giây), đảm bảo ID luôn tăng tuần tự theo thời gian tạo.
  - Khi lưu trữ vào SQL Server Clustered Index (B-Tree), UUIDv7 không gây ra chia tách trang đĩa (Index Page Split) như UUIDv4 ngẫu nhiên, giúp tốc độ INSERT và truy vấn tương đương `bigint`.

```csharp
public abstract class BaseEntity : BaseEntity<Guid>
{
    protected BaseEntity()
    {
        Id = Guid.CreateVersion7();
    }
}
```

---

## 2. Trường Kiểm toán & Xóa Mềm (Audit & Soft Delete)
Mọi Entity kế thừa `BaseEntity` đều sở hữu 4 trường kiểm toán cốt lõi:
- `CreatedDate`: Thời điểm tạo bản ghi (UTC).
- `UpdatedDate`: Thời điểm cập nhật cuối cùng (UTC), `null` nếu chưa từng cập nhật.
- `DeletedDate`: Thời điểm bị xóa logic (UTC), `null` nếu còn hoạt động.
- `IsDeleted`: Cờ logic (`bool`), mặc định `false`.

---

## 3. Quy tắc Cấu hình EF Core
1. **Auditable Entity Interceptor**:
   - Tự động gán `CreatedDate = DateTimeOffset.UtcNow` cho entity có trạng thái `EntityState.Added`.
   - Tự động gán `UpdatedDate = DateTimeOffset.UtcNow` cho entity có trạng thái `EntityState.Modified`.
   - Chặn thao tác `EntityState.Deleted` vật lý, tự động chuyển thành `EntityState.Modified` và gán `IsDeleted = true`, `DeletedDate = DateTimeOffset.UtcNow`.
2. **Global Query Filter**:
   - Mọi bảng có Soft Delete bắt buộc phải có Global Query Filter:
     ```csharp
     builder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
     ```
   - Khi cần truy vấn cả dữ liệu đã xóa để khôi phục hoặc kiểm toán, sử dụng `.IgnoreQueryFilters()`.

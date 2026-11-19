# Modular Monolith Architecture Rules

Tài liệu quy định ranh giới kiến trúc, cách ly dữ liệu và giao tiếp giữa các phân hệ (Modules) trong hệ thống.

---

## 1. Cấu trúc Dự án theo Module (Clean Architecture per Module)
Mỗi module đại diện cho một Bounded Context độc lập, bao gồm 4 project theo chuẩn Clean Architecture:
- `AgentCoreSystem.Module.{Name}.Domain`: Chứa Entities, Value Objects, Domain Events, Enums, Exceptions và Repository Interfaces. Hoàn toàn thuần túy, không phụ thuộc EF Core hay các framework bên ngoài.
- `AgentCoreSystem.Module.{Name}.Application`: Chứa Use Cases, MediatR Commands/Queries, DTOs, FluentValidation Validators, và Public Interfaces (`I{Name}ModuleApi`).
- `AgentCoreSystem.Module.{Name}.Infrastructure`: Chứa `DbContext` độc quyền, EF Core EntityTypeConfigurations, triển khai Repositories, và Background Jobs (Hangfire).
- `AgentCoreSystem.Module.{Name}.Presentation`: Chứa API Controllers đa phiên bản (v1, v2) và extension method `Add{Name}Module()` để đăng ký DI với Host.

---

## 2. Cách ly Dữ liệu: Schema-per-module trong SQL Server
- Mỗi module sử dụng một schema cơ sở dữ liệu riêng biệt trên cùng một SQL Server vật lý:
  - Module `System` -> Schema `system`
  - Module `Catalog` -> Schema `catalog`
  - Module `Order` -> Schema `order`
- Cấu hình bắt buộc trong DbContext:
  ```csharp
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
      base.OnModelCreating(modelBuilder);
      modelBuilder.HasDefaultSchema("system");
      modelBuilder.ApplyConfigurationsFromAssembly(typeof(SystemDbContext).Assembly);
  }
  ```
- **Quản lý Migrations độc lập**:
  Bảng lưu trữ lịch sử migration phải được cách ly theo từng schema:
  `sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory_{ModuleName}", "{schema}");`

---

## 3. Quy tắc Ranh giới Dữ liệu & Giao tiếp Liên Module
1. **Tuyệt đối cấm Foreign Key giữa các schema khác nhau**:
   - Ví dụ: Bảng `order.Orders` không được phép tạo Foreign Key ràng buộc vật lý đến `system.Users`. Thay vào đó, chỉ lưu `UserId` (UUIDv7) dưới dạng giá trị độc lập.
2. **Giao tiếp Đồng bộ (Synchronous)**:
   - Các module chỉ được phép gọi nhau qua Public Application Interface (ví dụ: `ISystemModuleApi`) do module cung cấp, không được inject DbContext hay Repository của module khác.
3. **Giao tiếp Bất đồng bộ (Asynchronous / Event-driven)**:
   - Ưu tiên sử dụng Domain Events / MediatR Notifications để thông báo trạng thái khi có sự kiện thay đổi dữ liệu giữa các module.


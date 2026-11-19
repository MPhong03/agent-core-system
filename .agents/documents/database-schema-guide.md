# Hướng dẫn Quản lý Database Schema & Migrations trên SQL Server

Tài liệu hướng dẫn kỹ thuật quản lý Schema-per-module và EF Core Migrations độc lập.

---

## 1. Cơ chế Schema-per-module
Mỗi module chịu trách nhiệm về schema của riêng mình trên cùng một cơ sở dữ liệu vật lý:
- `system`: Chứa các bảng quản trị hệ thống, users, roles, audit logs, system settings.
- `order`: Chứa các bảng đơn hàng, chi tiết đơn hàng, thanh toán.
- `catalog`: Chứa danh mục, sản phẩm, thuộc tính.

Trong `OnModelCreating` của DbContext:
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.HasDefaultSchema("system");
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(SystemDbContext).Assembly);
}
```

---

## 2. Quản lý Migrations Độc lập
Để tránh tình trạng các module tranh chấp bảng lịch sử migration `__EFMigrationsHistory`, mỗi module được chỉ định bảng lịch sử riêng:

```csharp
services.AddDbContext<SystemDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory_System", SystemDbContext.SchemaName);
    }));
```

### Lệnh tạo và áp dụng Migration cho Module System:
Chạy từ thư mục gốc backend:
```bash
# Tạo migration mới cho module System
dotnet ef migrations add InitialSystemDb `
  --project src/Modules/System/AgentCoreSystem.Module.System.Infrastructure `
  --startup-project src/Host/AgentCoreSystem.Api `
  --context SystemDbContext `
  --output-dir Persistence/Migrations

# Cập nhật Database
dotnet ef database update `
  --project src/Modules/System/AgentCoreSystem.Module.System.Infrastructure `
  --startup-project src/Host/AgentCoreSystem.Api `
  --context SystemDbContext
```


# Identity & RBAC Rules

Tài liệu quy định cách cấu hình Microsoft Identity, quản lý tài khoản và phân quyền dựa trên vai trò (Role-Based Access Control).

---

## 1. Microsoft Identity trong Module `System`
- Phân hệ `System` là nơi độc quyền quản lý và xác thực người dùng cho toàn bộ hệ thống.
- `SystemDbContext` kế thừa `IdentityDbContext<ApplicationUser, ApplicationRole, Guid>` và đặt toàn bộ các bảng vào schema `system`:
  - `system.Users`
  - `system.Roles`
  - `system.UserRoles`
  - `system.UserClaims`
  - `system.RoleClaims`
  - `system.UserLogins`
  - `system.UserTokens`

---

## 2. Các Vai trò Mặc định (Default Roles)
Hệ thống thiết lập 3 vai trò mặc định:

```csharp
public static class SystemRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Employee = "Employee";

    public static readonly IReadOnlyList<string> All = [SuperAdmin, Admin, Employee];
}
```

- **SuperAdmin**: Toàn quyền hệ thống, tự động bypass toàn bộ các bộ lọc và chính sách phân quyền (Authorization Policies & Requirements) thông qua `SuperAdminBypassAuthorizationHandler`.
- **Admin**: Quản trị viên phân hệ/chi nhánh, quản lý nhân viên và cấu hình module.
- **Employee**: Nhân viên tác nghiệp hàng ngày, chỉ được thực hiện các chức năng được cấp phép.

---

## 3. Seed Dữ liệu Ban đầu (Data Seeding)
- Khi khởi động ứng dụng (hoặc qua Seeder service), hệ thống kiểm tra và tự động khởi tạo 3 Role nếu chưa tồn tại.
- **Không hardcode thông tin tài khoản SuperAdmin trong mã nguồn**:
  - Thông tin tài khoản SuperAdmin ban đầu được nạp động từ `SystemSeedSettings` (`SeedSettings:SuperAdmin`) trong cấu hình ngoài (`appsettings.json`, Environment Variables, Secret Manager).
  - Cờ `SeedSettings:EnableSeeding` quyết định việc thực thi khởi tạo.
  - Tuân thủ nguyên tắc **Fail-Fast**: nếu `EnableSeeding = true` mà thông tin tài khoản SuperAdmin thiếu hoặc mật khẩu dưới 6 ký tự, hệ thống ném `InvalidOperationException` ngay lập tức.

---

## 4. Quản lý Khóa Bí Mật JWT & Cấu hình Fail-Fast
- Tuân thủ nghiêm ngặt quy định tại [Security & Configuration Fail-Fast Rules](security-and-configuration-fail-fast.md).
- **Tuyệt đối không lưu khóa bí mật mặc định trong code**: Class `JwtSettings` không được gán fallback default value cho `Secret`, `Issuer`, `Audience`.
- **Fail-Fast**: Bắt buộc ném `InvalidOperationException` ngay tại thời điểm khởi động nếu thiếu cấu hình `JwtSettings` hoặc `Secret` ngắn hơn 32 ký tự (256 bits).


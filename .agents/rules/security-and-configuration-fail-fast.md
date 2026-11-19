# Security & Configuration Fail-Fast Rules

Tài liệu quy định các chuẩn mực bảo mật đối với việc quản lý thông tin nhạy cảm, khóa bí mật (Secrets), và nguyên tắc **Fail-Fast** trong cấu hình hệ thống.

---

## 1. Nguyên tắc "Zero Hardcoded Secrets & Data" (Không Lưu Dữ Liệu Nhạy Cảm Trong Code)
- **Tuyệt đối cấm** lưu trữ bất kỳ thông tin nhạy cảm nào trong mã nguồn (source code/templates):
  - Khóa ký JWT (JWT Secret / Symmetric Signing Key).
  - Chuỗi kết nối cơ sở dữ liệu (Database Connection Strings).
  - Tài khoản quản trị, Mật khẩu khởi tạo ban đầu (Admin / SuperAdmin Seed Credentials).
  - Private Keys, API Keys của dịch vụ bên thứ ba (Third-party credentials, Payment gateway keys, SMTP credentials).
  - Địa chỉ hạ tầng tĩnh (CORS Allowed Origins, Third-party URLs).
- Dữ liệu cấu hình chỉ được nạp từ các nguồn an toàn bên ngoài:
  - Development: `appsettings.Development.json` (git-ignored) hoặc .NET `dotnet user-secrets`.
  - Production / Staging: Environment Variables, Azure Key Vault, AWS Secrets Manager, hoặc HashiCorp Vault.

---

## 2. Nguyên tắc "Fail-Fast Configuration" (Không Fallback Ngầm Định)
- **Tuyệt đối cấm fallback giá trị mặc định** cho các thiết lập cấu hình trọng yếu và nhạy cảm:
  - *Sai lầm phổ biến:* Khởi tạo property trong POCO Options với chuỗi hardcoded fallback (ví dụ: `public string Secret { get; set; } = "DefaultKey...";` hoặc `const string superAdminEmail = "admin@mvp.vn";`).
  - *Nguy cơ:* Ứng dụng chạy trên Production nhưng âm thầm sử dụng tài khoản hoặc khóa bí mật mặc định, dẫn đến lỗ hổng chiếm đoạt quyền quản trị và giả mạo token nghiêm trọng.
- **Cơ chế Fail-Fast bắt buộc:**
  - Nếu thiếu bất kỳ cấu hình bắt buộc nào khi khởi động (Bootstrap phase), ứng dụng phải **ngay lập tức dừng lại và ném `InvalidOperationException`** với thông điệp chỉ rõ cấu hình bị thiếu.
  - Áp dụng triệt để cho:
    1. `ConnectionStrings:DefaultConnection` (SQL Server)
    2. `ConnectionStrings:HangfireMongoConnection` (MongoDB)
    3. `JwtSettings:Secret`, `JwtSettings:Issuer`, `JwtSettings:Audience`
    4. `CorsSettings:AllowedOrigins`
    5. `SeedSettings:SuperAdmin` (khi `EnableSeeding = true`)

---

## 3. Quy chuẩn Khai báo POCO Options Pattern
Mọi class Options chứa cấu hình nhạy cảm phải tuân thủ khuôn mẫu:

```csharp
namespace AgentCoreSystem.Module.System.Application.Common
{
    /// <summary>
    /// Lớp cấu hình JWT theo Options Pattern.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Đọc các thiết lập từ mục "JwtSettings" trong file cấu hình.</para>
    /// <para><strong>Why:</strong> Không chứa giá trị fallback ngầm định để đảm bảo nguyên tắc Fail-Fast và Zero Hardcoded Secrets.</para>
    /// </remarks>
    public class JwtSettings
    {
        public const string SectionName = "JwtSettings";

        /// <summary>Khóa bí mật ký JWT đối xứng (tối thiểu 256 bits / 32 ký tự).</summary>
        public string Secret { get; set; } = string.Empty;

        /// <summary>Đơn vị phát hành token hợp lệ.</summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>Đối tượng tiếp nhận token hợp lệ.</summary>
        public string Audience { get; set; } = string.Empty;

        /// <summary>Thời gian sống của Access Token tính bằng phút.</summary>
        public int ExpiryMinutes { get; set; }
    }
}
```

---

## 4. Quy định về Khởi tạo Dữ liệu Mẫu (Data Seeding)
- **Không hardcode tài khoản trong Seeder**:
  - Lớp `SystemDataSeeder` không được chứa chuỗi hardcode cho Email, Password, hay FullName của SuperAdmin.
  - Toàn bộ thông tin seed phải đọc từ `IOptions<SystemSeedSettings>`.
- **Cờ kiểm soát `EnableSeeding`**:
  - Mặc định chỉ bật khi thiết lập môi trường (`SeedSettings:EnableSeeding = true`).
  - Khi tắt, Seeder tự động bỏ qua mà không thực hiện truy vấn hay khởi tạo tài khoản mới.
- **Fail-Fast khi Seed Data**:
  - Nếu `EnableSeeding = true`, hệ thống bắt buộc kiểm tra `Email` hợp lệ và `Password` có độ dài tối thiểu (>= 6 ký tự). Nếu thiếu, ném ngay `InvalidOperationException`.

---

## 5. Quy định về Giao diện Người dùng (Frontend Sanitization)
- **Không nhúng tài khoản/mật khẩu mặc định vào Frontend**:
  - Các ô nhập liệu (Input Form) tại màn hình Đăng nhập (Login) phải khởi tạo rỗng (`''`), không tự động điền sẵn tài khoản hoặc mật khẩu quản trị.
  - Không hardcode các khối thông tin chứa mật khẩu mẫu (`<code>Admin@2026</code>`) trong HTML template hay CSS style của production bundle.
  - Placeholders phải ở dạng tổng quát (`name@example.com` hoặc dùng key i18n), không đại diện cho tài khoản có thực trên hệ thống.

---

## 6. Danh mục Kiểm tra Bảo mật dành cho Agent (Security Checklist)
- [ ] Không có chuỗi secret/credential/tài khoản admin nào được gán mặc định trong mã nguồn C# hoặc TypeScript.
- [ ] `SystemDataSeeder` lấy toàn bộ thông tin tài khoản từ `SystemSeedSettings`.
- [ ] Mọi POCO Settings đều khởi tạo rỗng (`string.Empty`) hoặc `required`, không gán chuỗi giả định.
- [ ] Có câu lệnh kiểm tra `?? throw new InvalidOperationException(...)` tại Bootstrap cho mọi chuỗi kết nối, CORS origins, và khóa bí mật.
- [ ] Màn hình Đăng nhập (Frontend) không chứa thông tin đăng nhập hardcode.
- [ ] Khóa đối xứng HMAC-SHA256 có độ dài tối thiểu 256 bits (32 bytes UTF-8).


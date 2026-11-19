# Hướng dẫn Cấu hình Hangfire với MongoDB Storage

Tài liệu giải thích lý do kỹ thuật và cách vận hành Hangfire sử dụng MongoDB.

---

## 1. Lý do Kỹ thuật: Vì sao sử dụng MongoDB cho Hangfire?
Trong các hệ thống phân tán phức tạp với hàng trăm job chạy định kỳ (Recurring Jobs) hoặc hàng triệu background tasks (gửi thông báo, đồng bộ dữ liệu, kiểm toán):
- **Giảm tải cho SQL Server**: SQL Server chỉ nên tập trung tối đa cho dữ liệu quan hệ nghiệp vụ (ACID transactions). Các bảng của Hangfire có tần suất đọc/ghi thăm dò (polling), lock hàng đợi và log trạng thái cực kỳ cao.
- **MongoDB không khóa bảng (Lock-free queues)**: MongoDB xử lý tài liệu JSON và khối lượng ghi đồng thời rất tốt, có TTL index để tự động dọn dẹp các job cũ mà không làm chậm SQL Server.

---

## 2. Cấu hình Chuỗi Kết nối
Trong file `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AgentCoreSystemDb;Trusted_Connection=True;TrustServerCertificate=True;",
    "HangfireMongoConnection": "mongodb://localhost:27017/AgentCoreSystemHangfireDb"
  }
}
```

---

## 3. Hangfire Dashboard
Dashboard được kích hoạt tại đường dẫn `/hangfire`.
Trong môi trường Production, bắt buộc cấu hình Authorization Filter để chỉ những người dùng có vai trò `SuperAdmin` mới được phép truy cập xem job và retry thủ công.


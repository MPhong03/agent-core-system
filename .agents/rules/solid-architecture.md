# SOLID Architecture Guidelines

Tài liệu hướng dẫn áp dụng 5 nguyên lý thiết kế SOLID vào hệ thống Clean Architecture và Modular Monolith.

---

## 1. Single Responsibility Principle (SRP)
- Mỗi MediatR Command hoặc Query Handler chỉ xử lý duy nhất một Use Case cụ thể.
- Tách biệt hoàn toàn phần xác thực dữ liệu đầu vào (Input Validation) sang các lớp `AbstractValidator<T>` (FluentValidation) riêng, không đưa logic kiểm tra format vào trong Handler.
- Entity chỉ đảm bảo các bất biến nghiệp vụ (Invariants), không thực hiện I/O (không gọi DbContext hay HTTP service).

---

## 2. Open/Closed Principle (OCP)
- Mở rộng luồng xử lý thông qua MediatR Pipeline Behaviors (Logging, Validation, Performance, Transaction) mà không cần can thiệp hay chỉnh sửa code của các Handlers.
- Khi bổ sung một phân hệ nghiệp vụ mới (ví dụ: `Module.Catalog`), chỉ cần tạo một cụm project mới và gọi `services.AddCatalogModule(config)` mà không sửa đổi bất kỳ dòng code nào trong các module khác.

---

## 3. Liskov Substitution Principle (LSP)
- Mọi implementation của một Abstraction phải có khả năng thay thế hoàn toàn cho nhau mà không làm thay đổi tính đúng đắn của chương trình.
- Cụ thể: `MemoryCacheService` và `RedisCacheService` đều hiện thực `ICacheService`. Khi chuyển từ Memory Cache sang Redis Cache, caller hoàn toàn không nhận thấy sự khác biệt về hành vi ngoại trừ tốc độ và phạm vi lưu trữ.

---

## 4. Interface Segregation Principle (ISP)
- Không bắt buộc client phải phụ thuộc vào các method mà họ không sử dụng.
- Chia nhỏ các giao diện:
  - `ISoftDeletable`: Chỉ chứa `IsDeleted` và `DeletedDate`.
  - `IAuditableEntity`: Chỉ chứa `CreatedDate` và `UpdatedDate`.
  - `ISystemModuleApi`: Chỉ công khai các hàm nghiệp vụ tối thiểu mà các module khác cần gọi, không lộ toàn bộ Repository hay DbContext của module `System`.

---

## 5. Dependency Inversion Principle (DIP)
- Các tầng cốt lõi (Domain và Application) hoàn toàn không được phụ thuộc vào tầng bên ngoài (Infrastructure, EF Core, Controllers).
- Tất cả các tác vụ truy xuất dữ liệu, lưu cache, gửi mail, lập lịch job đều phải thông qua Interfaces định nghĩa tại tầng Application hoặc Shared Kernel.

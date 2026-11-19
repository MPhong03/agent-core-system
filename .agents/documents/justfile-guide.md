# Hướng dẫn Sử dụng 'justfile' (Command Runner Guide)

Tài liệu hướng dẫn chi tiết cách sử dụng `justfile` trong repository **AgentCoreSystem**.

---

## 1. Giới thiệu về `just`
`just` là một công cụ thực thi lệnh (command runner) hiện đại, tiện lợi và tương thích đa nền tảng (Windows, macOS, Linux). `just` đóng vai trò là điểm vào duy nhất (Single Entry Point) cho toàn bộ thao tác vận hành dự án, giúp developer và AI Agent không cần phải nhớ hoặc gõ các câu lệnh dài dòng.

### Cài đặt `just` CLI
Nếu làm việc trên máy trạm mới, bạn có thể cài đặt `just` qua package manager:
- **Windows (Winget):** `winget install Casey.Just`
- **Windows (Chocolatey):** `choco install just`
- **Windows (Scoop):** `scoop install just`
- **macOS:** `brew install just`
- **Linux:** `cargo install just` hoặc thông qua package manager của bản phân phối.

---

## 2. Danh mục Lệnh (Recipes Reference)

Bạn có thể chạy `just` hoặc `just --list` bất cứ lúc nào từ thư mục gốc của dự án để xem menu hướng dẫn trực quan.

### 2.1. Nhóm Phát triển & Khởi chạy (Development)
| Lệnh | Mô tả |
| :--- | :--- |
| `just api` | Chạy Backend Web API (`dotnet run`) |
| `just watch-api` | Chạy Backend Web API với chế độ tự động reload khi sửa code (`dotnet watch`) |
| `just ui` | Khởi chạy Frontend Angular dev server (`ng serve` tại cổng mặc định `4200`) |
| `just dev` | Chạy song song cả Backend (cửa sổ ngầm) và Frontend cho quy trình fullstack |

### 2.2. Nhóm Biên dịch & Build (Compilation)
| Lệnh | Mô tả |
| :--- | :--- |
| `just build` | Build toàn bộ solution Backend (.NET 9) và bundle Frontend (Angular) |
| `just build-backend` | Build riêng giải pháp backend `backend/AgentCoreSystem.slnx` |
| `just build-ui` | Build riêng frontend Angular (`ng build`) sang thư mục `dist/` |

### 2.3. Nhóm Kiểm thử Tự động (Testing)
| Lệnh | Mô tả |
| :--- | :--- |
| `just test` | Chạy toàn bộ Unit Tests của cả Backend và Frontend |
| `just test-backend` | Chạy xUnit tests của Backend (.NET 9) |
| `just test-ui` | Chạy Vitest tests của Frontend Angular (chế độ single-run) |

### 2.4. Nhóm Quản lý EF Core Migrations (Schema-per-module)
Trong kiến trúc Modular Monolith, mỗi module có DbContext và Schema riêng. `just` đơn giản hóa các lệnh dài dòng với tham số tự động:

| Lệnh | Cú pháp & Ví dụ | Mô tả |
| :--- | :--- | :--- |
| `just ef-add` | `just ef-add <Module> <MigrationName>`<br>*Ví dụ:* `just ef-add System AddUserAvatar` | Tự động xác định đúng Infrastructure project, Startup project, DbContext và đường dẫn Migrations để tạo migration mới. |
| `just ef-update` | `just ef-update <Module>`<br>*Ví dụ:* `just ef-update System` | Cập nhật database cho module được chỉ định lên migration mới nhất. |
| `just ef-remove` | `just ef-remove <Module>`<br>*Ví dụ:* `just ef-remove System` | Gỡ bỏ migration gần nhất chưa được áp dụng vào database. |
| `just ef-list` | `just ef-list <Module>`<br>*Ví dụ:* `just ef-list System` | Liệt kê danh sách các migrations đã tạo và trạng thái áp dụng. |

### 2.5. Nhóm Định dạng Code & Dọn dẹp (Formatting & Cleanup)
| Lệnh | Mô tả |
| :--- | :--- |
| `just format` | Tự động format toàn bộ mã nguồn C# và TypeScript/HTML/SCSS |
| `just format-backend` | Format C# theo chuẩn conventions (`dotnet format`) |
| `just format-ui` | Format Frontend theo chuẩn Prettier (`npx prettier --write`) |
| `just clean` | Xóa sạch các thư mục rác `bin/` và `obj/` trong backend |

### 2.6. Nhóm Hạ tầng Docker (Containers)
| Lệnh | Mô tả |
| :--- | :--- |
| `just infra-up` | Khởi động các container hạ tầng (SQL Server, MongoDB...) qua Docker Compose |
| `just infra-down` | Dừng và hạ các container hạ tầng |

---

## 3. Quy ước Dành cho Agent & Developer
- Khi cần build, test, format hoặc làm việc với EF Core, Agent **BẮT BUỘC** ưu tiên gọi qua `just <command>` thay vì tự gõ các lệnh terminal phức tạp.
- File cấu hình `justfile` sử dụng PowerShell làm shell mặc định trên Windows (`set windows-shell := ["powershell.exe", "-NoProfile", "-Command"]`).

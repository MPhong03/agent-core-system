# Quy Chuẩn Chống Code Ẩu & Mã Rác (Anti-AI-Slop Standards)

Tài liệu quy định hệ thống kiểm soát chất lượng mã nguồn áp dụng cho cả **Backend (.NET 9)** và **Frontend (Angular 19)** nhằm triệt tiêu hoàn toàn các dấu hiệu code ẩu, code đối phó, code rác (AI Slop) do AI hoặc lập trình viên thiếu cẩn trọng sinh ra.

Hệ thống gồm 14 tiêu chuẩn được phân loại theo **4 Trụ Cột Kiến Trúc** và xếp theo thứ tự mức độ nghiêm trọng (**CRIT** $\rightarrow$ **WARN**).

---

## 1. Bảng Tiêu Chuẩn Anti-AI-Slop (Code Quality Checklist)

| Mã | Nội dung Quy chuẩn | Mức phạt | Phạm vi | Nhóm Trụ cột |
| :--- | :--- | :--- | :--- | :--- |
| **RULE-01** | Hai nửa của một contract (FE↔BE, ghi↔đọc) phải khớp về field, casing, kiểu, verb, route (**ưu tiên cao nhất**) | **CRIT** | Contract | 🏛️ Hợp đồng & Ranh giới |
| **RULE-02** | Không gán cứng host, port, IP nội bộ, version hay cờ quyền `true` | **CRIT** | BE & FE | 🏛️ Hợp đồng & Ranh giới |
| **RULE-03** | Mọi khoá i18n, quyền, route, config và symbol import đều phải tồn tại, đủ ở mọi ngôn ngữ (EN, VI) | **CRIT** | Chung | 🏛️ Hợp đồng & Ranh giới |
| **RULE-04** | Không để mock, seed, json tĩnh nằm trong luồng chạy thật; BE không resolve ra bản stub `Null*/Fake*` | **CRIT** | BE & FE | ⚙️ Luồng Thực Thi Thực Chất |
| **RULE-05** | Chức năng chưa làm không được lộ ra người dùng: handler rỗng bind vào nút, `NotImplementedException`, route chỉ có khung rỗng | **CRIT** | BE & FE | ⚙️ Luồng Thực Thi Thực Chất |
| **RULE-06** | Chỉ báo thành công khi việc đã làm xong thật; `catchError` nuốt mọi lỗi rồi trả `of([])`/`null` là "thành công giả" | **CRIT** | BE & FE | ⚙️ Luồng Thực Thi Thực Chất |
| **RULE-07** | BE: `catch` bắt lỗi rộng mà thân rỗng phải có lý do; bộ lọc huỷ không được che lỗi timeout | **CRIT** | Backend | ⚙️ Luồng Thực Thi Thực Chất |
| **RULE-08** | Không để dead code: export/route không ai gọi, khối code bị comment-out | **WARN** | BE & FE | 🧹 Chất Lượng Code & Tái Cấu Trúc |
| **RULE-09** | BE: logic nghiệp vụ bị chép tới lần thứ ba thì phải gom về một chỗ (DRY) | **WARN** | Backend | 🧹 Chất Lượng Code & Tái Cấu Trúc |
| **RULE-10** | BE: seam hoặc service trả hằng thay cho nguồn thật phải ghi rõ là tạm và nêu cách thay bằng bản thật | **WARN** | Backend | 🧹 Chất Lượng Code & Tái Cấu Trúc |
| **RULE-11** | Comment phải tự hiểu được, không kể lịch sử, không nhắc tới phiên làm việc | **WARN** | Comment | 📝 Tài Liệu & Kỷ Luật Git |
| **RULE-12** | Comment, docs, README, AGENTS.md, `.agents/documents/` phải mô tả đúng code hiện tại | **WARN** | Tài liệu | 📝 Tài Liệu & Kỷ Luật Git |
| **RULE-13** | Làm theo luật đã ghi trong repo, nếu không có thì theo pattern đã có | **WARN** | Chung | 📝 Tài Liệu & Kỷ Luật Git |
| **RULE-14** | Diff phải khớp với tên nhánh và commit message, không sửa lan sang việc ngoài phạm vi | **WARN** | Chung | 📝 Tài Liệu & Kỷ Luật Git |

---

## 2. Hướng Dẫn Kỹ Thuật Chi Tiết Theo 4 Trụ Cột

### 🏛️ Trụ cột 1: Tính Toàn Vẹn Kiến Trúc & Hợp Đồng (Mức CRIT)

#### RULE-01: Khớp Hợp Đồng FE ↔ BE (Ưu Tiên Tối Thượng)
- **Casing:** JSON Payload luôn dùng `camelCase` (ví dụ: `userName`, `isActive`). C# DTO dùng `PascalCase` (`UserName`, `IsActive`).
- **Data Types:** 
  - `Guid` bên C# ↔ `string` (UUIDv7) bên TypeScript.
  - `DateTime` / `DateTimeOffset` C# ↔ `string` (ISO 8601) bên TypeScript.
  - `decimal` tiền tệ ↔ `number` bên TypeScript.
- **Route & HTTP Verbs:** Đảm bảo URL path, query params và HTTP Verb (`GET`, `POST`, `PUT`, `PATCH`, `DELETE`) giữa Angular Service và ASP.NET Controller khớp nhau từng ký tự.

#### RULE-02: Cấm Hardcode Host, Port, Cờ Quyền
- Cấm tuyệt đối: `http://localhost:5001`, `127.0.0.1`, `http://192.168.x.x` trong code logic. Mọi địa chỉ phải đọc từ `appsettings.json` (BE) hoặc `environment.ts` (FE).
- Cấm hardcode cờ bypass quyền: `const isAdmin = true;` hay `if (user.Role == "Admin" || true)`.

#### RULE-03: Toàn Vẹn i18n, Route, Config & Permissions
- Khi thêm khóa dịch thuật mới, bắt buộc phải khai báo đầy đủ ở cả 2 ngôn ngữ: Tiếng Việt (`vi-VN.json` / Resources) và Tiếng Anh (`en-US.json` / Resources).
- Không để xảy ra tình trạng thiếu key dẫn đến giao diện hiển thị mã thô `system.users.btn_save`.
- Quyền hạn (Permissions) phải tồn tại trong Constants/Enums hệ thống.

---

### ⚙️ Trụ cột 2: Tính Thực Chất Của Chức Năng & Luồng Thực Thi (Mức CRIT)

#### RULE-04: Cấm Mock/Seed/Stub trong Luồng Chạy Thật
- **Backend:** 
  - Cấm inject các class dạng `NullEmailSender`, `FakePaymentGateway` trong Container DI của production/runtime mà không có cờ cấu hình và log cảnh báo rõ ràng.
  - Cấm Controller/Handler trả về danh sách fix cứng tĩnh (hardcoded list) để "giả vờ" đã kết nối CSDL.
- **Frontend:**
  - Cấm import file `mock-data.json` hoặc hardcode mảng dữ liệu mẫu trong Service/Component runtime để hiển thị giao diện.
  - Toàn bộ dữ liệu hiển thị phải đến từ HTTP client gọi API thật (ngoại trừ các file test spec `*.spec.ts`).

#### RULE-05: Không Lộ Chức Năng Rỗng / Chưa Hoàn Thiện
- **Backend:**
  - Cấm tạo endpoint quăng `throw new NotImplementedException()`. Nếu Use Case chưa làm, không khai báo route và không xuất bản lên Swagger OAS.
- **Frontend:**
  - Cấm gán sự kiện click vào hàm rỗng hoặc hàm chỉ chứa `console.log('TODO')`.
  - Cấm tạo route trỏ đến component chỉ có dòng chữ "Coming soon" hoặc layout trống không.
  - Nếu một action chưa được hỗ trợ: Ẩn nút khỏi UI hoặc disable nút kèm tooltip giải thích lý do rõ ràng.

#### RULE-06: Cấm "Báo Thành Công Giả" & Cấm Nuốt Lỗi
- **Backend:**
  - Cấm trả về `Result.Success()` khi tác vụ ngầm (Background Job) bị fail hoặc giao dịch chưa commit thành công vào DB.
- **Frontend:**
  - Tuyệt đối cấm pattern nuốt lỗi:
    ```typescript
    // SAI (VI PHẠM RULE-06): Nuốt lỗi rồi giả vờ thành công
    this.http.get('/api/v1/items').pipe(
      catchError(() => of([])) // nuốt lỗi, UI tưởng là danh sách rỗng
    );
    ```
  - Bắt buộc phải đẩy lỗi về Signal trạng thái (`errorState`), hiển thị Toast thông báo lỗi hoặc UI Error State cho người dùng.

#### RULE-07: Quản Lý Exception & CancellationToken Nghiêm Ngặt (BE)
- Cấm nuốt exception bằng `catch (Exception) { }` rỗng. Nếu cố tình bỏ qua ngoại lệ, bắt buộc phải có comment giải thích rõ lý do kèm `AI-NOTE:`.
- Khi bắt `OperationCanceledException`, phải phân biệt rõ giữa việc Client ngắt kết nối chủ động với Timeout từ hệ thống (không để timeout bị che giấu).

---

### 🧹 Trụ cột 3: Chất Lượng Mã Nguồn & Tái Cấu Trúc (Mức WARN)

#### RULE-08: Triệt Tiêu Dead Code & Comment-out Code
- Cấm để lại các đoạn mã bị comment-out `// var oldCode = ...;` hoặc `/* <div class="old-ui">...</div> */`. Mọi lịch sử thay đổi đã có Git quản lý.
- Cấm giữ lại các class, interface, enum, helper, service hoặc route không còn bất kỳ nơi nào trong dự án tham chiếu tới.

#### RULE-09: Nguyên Tắc DRY - Không Nhân Bản Logic Quá 3 Lần (BE)
- Khi một đoạn mã xử lý nghiệp vụ, mapping dữ liệu hoặc validation xuất hiện từ lần thứ 3 trở lên:
  - Bắt buộc phải refactor, gom về một nơi duy nhất: Domain Service, Custom Extension Method, hoặc Shared Value Object.

#### RULE-10: Seam / Stub Phải Có Hướng Dẫn Thay Thế Rõ Ràng (BE)
- Nếu một service bắt buộc phải trả dữ liệu tĩnh tạm thời (ví dụ: chờ đối tác cung cấp API SMS/ZNS):
  - Bắt buộc gắn XML Doc `<remarks>` kèm ký hiệu 🔴 và `AI-NOTE:`.
  - Phải ghi rõ: *"Dữ liệu tạm thời. Cách thay thế bằng dịch vụ thật: Triển khai IZnsClient và cấu hình ZnsSettings trong appsettings.json"*.

---

### 📝 Trụ cột 4: Tài Liệu, Comment & Kỷ Luật Git (Mức WARN)

#### RULE-11: Comment Khách Quan, Không Kể Lịch Sử Phiên Làm Việc
- Cấm: `// Phong vừa yêu cầu sửa ngày 20/11`, `// AI sửa hàm này vì bug ở phiên trước`.
- ĐÚNG: Nêu rõ **lý do kỹ thuật** và **ràng buộc kiến trúc** tại thời điểm hiện tại.

#### RULE-12: Tài Liệu Luôn Phản Ánh Code Thực Tế
- Khi đổi tên bảng, đổi schema, thêm endpoint, sửa hợp đồng DTO: lập tức cập nhật tài liệu tương ứng trong `AGENTS.md`, `.agents/documents/` và XML docs/JSDoc.
- Không để tài liệu nói một đằng nhưng code chạy một nẻo.

#### RULE-13: Tuân Thủ Luật & Pattern Hiện Hữu
- Ưu tiên cao nhất là tuân thủ các quy tắc trong `.agents/rules/` và chỉ dẫn tại `AGENTS.md`.
- **Quy ước vị trí tài liệu AI**: Toàn bộ file Design Spec và Implementation Plan do AI sinh ra bắt buộc phải lưu tại `.agents/specs/` và `.agents/plans/`. Tuyệt đối cấm tạo thư mục `docs/superpowers/` hay vương vãi tài liệu quy trình ra ngoài thư mục gốc.
- Nếu gặp bài toán mới chưa có quy định thành văn, phải khảo sát phong cách và pattern đã có sẵn trong các module trước đó của repo để làm theo, cấm tự ý chế biến style dị biệt.

#### RULE-14: Diff Tập Trung, Không Lan Man
- Thay đổi trong Git Diff phải bám sát 100% mục tiêu của nhánh hoặc commit.
- Cấm "tiện tay" format lại các file không liên quan, cấm sửa style hoặc đổi tên biến ở các module ngoài phạm vi yêu cầu.

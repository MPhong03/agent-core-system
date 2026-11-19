---
name: review
description: Use when reviewing git diffs, working tree changes, PRs, or commits against the project rules in .agents/rules/ before committing or merging. Triggered by /review or @review.
---

# /review - AgentCoreSystem Rules Compliance Code Review

Skill này là công cụ thanh tra chuyên sâu (Auditor) dành riêng cho dự án **AgentCoreSystem**. Mục tiêu tối thượng là đối chiếu từng dòng mã nguồn thay đổi (diff) so với toàn bộ 10 bộ quy tắc cốt lõi được định nghĩa trong `.agents/rules/`, phát hiện vi phạm kiến trúc, bảo mật, quy ước code và yêu cầu khắc phục trước khi commit hoặc merge vào `main`.

---

## 1. Khi Nào Kích Hoạt Skill Này?
- Khi người dùng gõ `/review`, `@review`, hoặc yêu cầu "review code", "kiểm tra vi phạm rules", "đối chiếu với .agents/rules/".
- Trước khi tạo Pull Request hoặc sau khi hoàn thành một feature branch.
- Khi muốn kiểm tra working tree (`git diff`) hoặc một commit cụ thể (`git diff HEAD~1`).

---

## 2. Quy Trình Thực Hiện (Review Workflow)

### Bước 1: Thu Thập Phạm Vi Thay Đổi (Diff Collection)
Xác định chính xác các file và dòng code bị thay đổi bằng các lệnh git:
```powershell
# Xem tổng quan trạng thái
git status --short

# Nếu kiểm tra uncommitted changes:
git diff HEAD

# Nếu kiểm tra staged changes:
git diff --cached

# Nếu kiểm tra branch hiện tại so với main:
git diff main...HEAD --stat
git diff main...HEAD
```
*Lưu ý: Chỉ tập trung phân tích các file thực sự bị thay đổi, bỏ qua thư mục build (`bin/`, `obj/`, `dist/`, `test-results/`).*

---

### Bước 2: Chạy Pre-flight Checks (Kiểm Chứng Tự Động)
Trước khi soi từng dòng code, chạy kiểm tra tự động xem code có build và pass test hay không:
```powershell
# Backend
just test-backend

# Frontend (nếu có thay đổi trong thư mục frontend/)
cd frontend; npm run build
```
> [!WARNING]
> Nếu build thất bại hoặc unit test bị fail, lập tức dừng và báo cáo lỗi **BLOCKER: Build/Test Failure**. Không tiến hành review các khía cạnh khác cho đến khi code có thể compile và pass toàn bộ test.

---

### Bước 3: Ma Trận Đối Chiếu Chi Tiết (.agents/rules/ Checklist)

Duyệt qua từng file trong diff và đối chiếu nghiêm ngặt với 10 quy tắc sau:

#### Quy Tắc 1: C# Conventions (`.agents/rules/csharp-conventions.md`)
- [ ] **Block-scoped Namespace**: Bắt buộc dùng `namespace AgentCoreSystem.X { ... }`. Cấm tuyệt đối file-scoped namespace (`namespace AgentCoreSystem.X;`).
- [ ] **Primary Constructors**: Bắt buộc dùng Primary Constructor cho Dependency Injection (`public class OrderService(IOrderRepository repo) : IOrderService`).
- [ ] **Copyright Header**: Các file kiến trúc cốt lõi (Base classes, Entities, DbContext, Interceptors, Key Services, Root configs) bắt buộc có copyright header Phong Minh Dang:
  ```csharp
  // ------------------------------------------------------------------------------------------------
  // Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
  // Licensed under the MIT License. See LICENSE in the project root for license information.
  // ------------------------------------------------------------------------------------------------
  ```
- [ ] **XML Docs What-When-Why**: Các public class, method, endpoint bắt buộc có thẻ `<summary>` (What) và `<remarks>` (When & Why), cùng `<param>`, `<returns>`. Cấm comment sáo rỗng hoặc viết bằng tiếng Anh đối phó vô nghĩa.
- [ ] **Naming Conventions**: PascalCase cho Class/Method/Property/Event; camelCase cho parameters và local variables. Không dùng tiền tố `_` trong tham số của Primary Constructor.

#### Quy Tắc 2: Modular Monolith (`.agents/rules/modular-monolith-rules.md`)
- [ ] **Clean Architecture độc lập theo Module**: Mỗi module chỉ chứa `Domain`, `Application`, `Infrastructure`, `Presentation`.
- [ ] **Schema-per-module**: Entity của module phải được map vào schema riêng biệt (vd: `system`, `org`).
- [ ] **Tuyệt Đối Cấm Direct Foreign Key Liên Module**: Không bao giờ tạo FK trực tiếp giữa 2 bảng thuộc 2 schema khác nhau.
- [ ] **Giao Tiếp Liên Module Hợp Lệ**: Bắt buộc qua MediatR Events/Queries hoặc Public Application Interfaces (vd: `ISystemModuleApi`). Cấm inject chéo `DbContext` của module khác.

#### Quy Tắc 3: Error Handling & RFC 7807 (`.agents/rules/error-handling-problem-details.md`)
- [ ] **Result Pattern**: Mọi Use Case Handler trả về `Result<T>` hoặc `Result`. Cấm throw Business Exceptions để điều khiển luồng.
- [ ] **ErrorType Rõ Ràng**: Lỗi phải dùng `Result.Failure(Error.Validation(...))` hoặc `Error.NotFound(...)`, `Error.Conflict(...)`, `Error.Unauthorized(...)`, `Error.Forbidden(...)`.
- [ ] **RFC 7807 Mapping**: Controllers bắt buộc dùng extension `ToProblemDetails()` để trả về HTTP response tương ứng với `ErrorType`.
- [ ] **GlobalExceptionHandler**: Bắt unhandled exceptions, log kèm `traceId`, trả về JSON 500 chuẩn RFC 7807.

#### Quy Tắc 4: Database, EF Core & UUIDv7 (`.agents/rules/database-efcore-uuidv7.md`)
- [ ] **UUIDv7**: Entity kế thừa `BaseEntity<Guid>` hoặc `BaseEntity` và sinh ID bằng `Guid.CreateVersion7()`.
- [ ] **Audit & Soft Delete**: Tự động qua `AuditableEntityInterceptor` (`CreatedDate`, `UpdatedDate`) và `SoftDeleteInterceptor` (`IsDeleted`, `DeletedDate`).
- [ ] **Module Migrations**: Migrations nằm trong thư mục của Module Infrastructure, history table dạng `__EFMigrationsHistory_{ModuleName}`.

#### Quy Tắc 5: Identity & RBAC (`.agents/rules/identity-and-rbac.md`)
- [ ] Quản lý user/role thuộc schema `system`.
- [ ] Kiểm tra phân quyền đúng 3 role: `SuperAdmin`, `Admin`, `Employee` hoặc policy tương ứng. Cấm hardcode bypass role check.

#### Quy Tắc 6: Security & Configuration Fail-Fast (`.agents/rules/security-and-configuration-fail-fast.md`)
- [ ] **Zero Hardcoded Secrets**: Tuyệt đối không chứa JWT secret, DB password, API keys, hoặc admin credentials trong code/templates.
- [ ] **Fail-Fast**: Cấm fallback ngầm định cho các cấu hình trọng yếu. Ném `InvalidOperationException` ngay tại Startup nếu thiếu cấu hình.

#### Quy Tắc 7: OpenAPI Specification OAS v3 (`.agents/rules/openapi-specification.md`)
- [ ] Mọi API Controller action có XML docs `<summary>`, `<remarks>` mô tả chi tiết.
- [ ] Đầy đủ attribute `[ProducesResponseType(typeof(...), StatusCodes.Status200OK)]`, `[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]`, v.v.

#### Quy Tắc 8: Angular Standards (`.agents/rules/angular-standards.md`)
- [ ] **Standalone Components**: `standalone: true` là mặc định. Không dùng NgModules cho features mới.
- [ ] **ChangeDetectionStrategy.OnPush**: Bắt buộc trên 100% components.
- [ ] **Angular Signals**: Quản lý state bằng `signal()`, `computed()`, `effect()`. Không lạm dụng BehaviorSubject không cần thiết.
- [ ] **Control Flow Syntax**: Bắt buộc dùng `@if`, `@for (...; track item.id)`, `@empty`, `@defer`. Cấm dùng `*ngIf`, `*ngFor`.
- [ ] **Lazy Loading**: Route khai báo qua `loadComponent` hoặc `loadChildren`.

#### Quy Tắc 9: Frontend Design Philosophy (`.agents/rules/frontend-design-philosophy.md`)
- [ ] **Dropbox Design Tokens**: Warm Canvas `#F7F5F2`, Primary Blue `#0061FF`, Ink `#1E1919`, Border 1px `#D8D6D3`.
- [ ] **Hallmark Anti-AI-slop**: Không dùng card bóng bẩy, gradient lòe loẹt, bo góc quá đà.
- [ ] **8-State Discipline**: UI có xử lý đầy đủ các trạng thái: Empty, Loading, Populated, Error, Submitting, v.v.

#### Quy Tắc 10: SOLID Architecture (`.agents/rules/solid-architecture.md`)
- [ ] SRP: Mỗi class làm đúng một nhiệm vụ.
- [ ] DIP: Domain/Application không phụ thuộc vào Infrastructure; phụ thuộc thông qua abstractions (interfaces).

---

## 4. Định Dạng Báo Cáo Kết Quả Review

Báo cáo review bắt buộc phải trình bày theo cấu trúc markdown sau:

```markdown
# 📋 Báo Cáo Code Review Đối Chiếu .agents/rules/

**Phạm vi:** [Nhánh / Commit / Working Tree]
**Tổng số file thay đổi:** X files (+Y / -Z dòng)
**Kết quả Pre-flight:** ✅ Build & Tests Passed / ❌ Failed

---

### 🔍 Chi Tiết Đánh Giá Theo Quy Tắc

#### 1. 🔴 BLOCKERS (Vi phạm nghiêm trọng - Bắt buộc sửa trước khi commit/merge)
- **[Tên File:Dòng]**: [Mô tả vi phạm cụ thể]
  - *Quy tắc vi phạm:* [Quy tắc X trong .agents/rules/...]
  - *Mã vi phạm:*
    ```csharp
    // code vi phạm
    ```
  - *Giải pháp khắc phục:*
    ```csharp
    // code chuẩn sửa đổi
    ```

#### 2. 🟡 WARNINGS / ĐỀ XUẤT NÂNG CAO (Nên cải thiện)
- **[Tên File:Dòng]**: [Mô tả chi tiết và gợi ý cải tiến]

#### 3. 🟢 COMPLIANT (Các tiêu chuẩn tuân thủ tốt)
- [Liệt kê các điểm tuân thủ xuất sắc: UUIDv7, Primary Constructor, Signals, OAS docs...]

---

### 🏁 KẾT LUẬN & VERDICT
- **Trạng thái:** [APPROVED / CHANGES REQUESTED]
- **Hành động tiếp theo:** [Hướng dẫn cụ thể cho dev/agent để hoàn thiện]
```

---

## 5. Tự Động Sửa Lỗi (Auto-Fix Mode)
Nếu người dùng yêu cầu: `/review --fix` hoặc "review và sửa luôn", Agent sau khi lập bảng báo cáo sẽ:
1. Trình bày danh sách vi phạm.
2. Thực hiện sửa đổi trực tiếp trên các file vi phạm bằng tool `edit_file`.
3. Chạy lại `just test-backend` và test frontend để bảo đảm code sạch lỗi.
4. Báo cáo lại trạng thái `APPROVED` sau khi đã fix.

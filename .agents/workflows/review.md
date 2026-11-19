# Workflow: /review - Code Review Đối Chiếu .agents/rules/

Workflow tự động kiểm tra, rà soát và đánh giá các thay đổi trong mã nguồn (uncommitted changes, branch commits hoặc PR) so với 10 bộ quy tắc cốt lõi của dự án trong `.agents/rules/`.

## Các Bước Thực Thi

### Bước 1: Thu Thập Changes
- Chạy `git status --short` để xem danh sách file thay đổi.
- Chạy `git diff HEAD` (hoặc `git diff main...HEAD` nếu đang ở feature branch) để lấy toàn bộ nội dung diff.

### Bước 2: Pre-flight Verification
- Chạy `just test-backend` để kiểm tra compile và unit tests.
- Nếu có file trong `frontend/`, chạy `npm --prefix frontend run build`.
- Nếu fail, báo ngay lỗi Blocker trước khi review logic.

### Bước 3: Đối Chiếu Chi Tiết Với .agents/rules/
Đọc và áp dụng checklist từ skill `@[.agents/skills/review/SKILL.md]`:
1. **C# Conventions**: Block-scoped namespaces, Primary constructors, XML Docs `<summary>` + `<remarks>`, Copyright headers.
2. **Modular Monolith**: Schema-per-module, tuyệt đối cấm cross-module direct FKs, giao tiếp qua MediatR/Interfaces.
3. **Error Handling**: Result pattern `Result<T>`, ErrorType rõ ràng, `ToProblemDetails()`, RFC 7807.
4. **Database & UUIDv7**: BaseEntity với UUIDv7, Interceptors, Soft delete.
5. **Identity & RBAC**: Schema `system`, role checks.
6. **Security & Fail-Fast**: Zero hardcoded secrets, throw `InvalidOperationException` khi thiếu config.
7. **OpenAPI Specification**: XML Docs trên Controllers, `[ProducesResponseType]`.
8. **Angular Standards**: Standalone, `ChangeDetectionStrategy.OnPush`, Signals, Control flow (`@if`, `@for`).
9. **Frontend Design Philosophy**: Dropbox tokens (`#F7F5F2`, `#0061FF`, 1px borders), Hallmark 8-state discipline.
10. **SOLID Architecture**: SRP, DIP, Interface Segregation.

### Bước 4: Xuất Báo Cáo
- Xuất bảng báo cáo theo mẫu chuẩn trong `@[.agents/skills/review/SKILL.md]`:
  - 🔴 **BLOCKERS**
  - 🟡 **WARNINGS**
  - 🟢 **COMPLIANT**
  - **VERDICT** (`APPROVED` hoặc `CHANGES REQUESTED`)

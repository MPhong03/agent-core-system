# AGENTS.md - AgentCoreSystem Modular Monolith & Angular Workspace

Chào mừng Agent đến với Codebase AgentCoreSystem. Tài liệu này là chỉ mục điều hướng tối cao (Index of Guidelines), định nghĩa kiến trúc, cấu trúc thư mục, và các quy tắc bắt buộc mà mọi Agent phải tuân thủ nghiêm ngặt trong suốt quá trình phát triển.

---

## 1. Bản quyền & Thông tin Tác giả (Copyright)
Các file kiến trúc cốt lõi/quan trọng (Building blocks, Base classes, DbContext, Key Interceptors/Services, Root configurations) bắt buộc phải có header bản quyền:
```csharp
// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------
```
Đối với TypeScript:
```typescript
/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */
```

---

## 2. Hệ thống Quy tắc Bắt buộc (.agents/rules/)
Trước khi tạo mới hoặc sửa đổi code, Agent **BẮT BUỘC** phải tham chiếu và tuân thủ các quy tắc trong `.agents/rules/`:

1. **[C# Coding Conventions](.agents/rules/csharp-conventions.md)**:
   - Sử dụng **Block-scoped namespaces** (`namespace AgentCoreSystem.Domain { ... }`).
   - Sử dụng **C# Primary Constructors** cho việc khai báo Dependency Injection (`public class Service(IDependency dep) : IService`).
   - **Quy tắc Comment & XML Docs**:
     - Dùng XML doc `/// <summary>` cho class, property, method `public` và `internal`, kèm `<remarks>`, `<param>`, `<returns>`, `<see cref="..."/>`, `<c>...</c>`, `<inheritdoc />`.
     - Viết bằng tiếng Việt, thuật ngữ kỹ thuật giữ tiếng Anh.
     - Tập trung vào **lý do và ràng buộc**, không diễn giải code (vd: «Enricher điền sau phân trang ⇒ không lọc/sắp được»).
     - Dấu hiệu nhận diện: `AI-NOTE:` (ghi chú cho AI/dev đọc sau), `// ── Tiêu đề nhóm ──` (chia nhóm property).
     - Ký hiệu đánh dấu: 🔴 (cảnh báo «đừng đổi cái này vì...»), ⛔ (điều cấm), ⇒ (quan hệ kéo theo).
2. **[Modular Monolith Architecture](.agents/rules/modular-monolith-rules.md)**:
   - Mỗi module là một tập hợp các project Clean Architecture độc lập (Domain, Application, Infrastructure, Presentation).
   - Cơ chế **Schema-per-module** trên cùng một database SQL Server (ví dụ: `system`, `catalog`, `order`).
   - **Tuyệt đối cấm** Foreign Key trực tiếp giữa các module khác nhau. Giao tiếp liên module phải thông qua MediatR Events/Queries hoặc Public Application Interfaces (vd: `ISystemModuleApi`).
3. **[Error Handling & RFC 7807 ProblemDetails](.agents/rules/error-handling-problem-details.md)**:
   - Mọi Use Case thất bại phải trả về `Result.Failure(Error)` với `ErrorType` cụ thể (`Validation`, `NotFound`, `Conflict`, `Unauthorized`, `Forbidden`, `Unexpected`).
   - Controllers sử dụng `ToProblemDetails()` để map lỗi sang chuẩn RFC 7807 ProblemDetails.
   - Bắt Unhandled Exceptions bằng `IExceptionHandler` (`GlobalExceptionHandler`), ghi log kèm `traceId` và trả về JSON 500 chuẩn ProblemDetails.
4. **[Database, EF Core & UUIDv7](.agents/rules/database-efcore-uuidv7.md)**:
   - Mọi Entity kế thừa `BaseEntity<TId>` hoặc `BaseEntity` (mặc định `Guid` với thuật toán **UUIDv7** từ `Guid.CreateVersion7()`).
   - Chống phân mảnh Index B-tree Clustered của SQL Server.
   - Tự động gán `CreatedDate`, `UpdatedDate` qua Interceptor và cơ chế Soft Delete (`!IsDeleted`) qua Global Query Filter.
   - Quản lý EF Migrations riêng biệt theo từng schema (`__EFMigrationsHistory_{ModuleName}`).
5. **[Identity & RBAC](.agents/rules/identity-and-rbac.md)**:
   - Phân hệ `System` sử dụng Microsoft Identity (`IdentityDbContext<ApplicationUser, ApplicationRole, Guid>`) đặt trên schema `system`.
   - 3 Role mặc định: `SuperAdmin`, `Admin`, `Employee`.
6. **[Security & Configuration Fail-Fast](.agents/rules/security-and-configuration-fail-fast.md)**:
   - **Zero Hardcoded Secrets & Data**: Tuyệt đối không lưu trữ khóa bí mật (JWT Secret, API Keys, Passwords, Connection Strings) hoặc thông tin tài khoản admin mẫu trong source code hay UI templates.
   - **Fail-Fast**: Cấm fallback ngầm định cho các cấu hình trọng yếu (CORS, Seed Data, JWT, DB Connections). Ném `InvalidOperationException` ngay tại Startup nếu thiếu cấu hình hoặc không an toàn.
7. **[OpenAPI Specification (OAS)](.agents/rules/openapi-specification.md)**:
   - API Controllers tài liệu hóa theo chuẩn OAS v3: `<summary>` cho operation title, `<remarks>` cho mô tả chi tiết bằng Markdown, `<response code="...">`, và attribute `[ProducesResponseType]`.
8. **[Angular Standards](.agents/rules/angular-standards.md)**:
   - Feature-driven Modular Design với **Standalone Components** làm mặc định.
   - Tối ưu hiệu năng với `ChangeDetectionStrategy.OnPush`, **Angular Signals** (`signal`, `computed`, `effect`) và cú pháp Control Flow mới (`@if`, `@for`, `@defer`).
   - Lazy load toàn bộ Feature modules qua `loadChildren` / `loadComponent`.
   - **Quy tắc Comment & JSDoc**:
     - Đầu file có JSDoc `@file <tên>.ts — mô tả (mã PBI)` kèm các mục `## …` giải thích thiết kế (vd: «Hai vai trò của tab»).
     - Ký hiệu đánh dấu: 🔴 (cảnh báo «đừng đổi cái này vì...»), ⛔ (điều cấm), ⇒ (quan hệ kéo theo), `AI-NOTE:`, `// ── Tiêu đề nhóm ──`.
     - Comment HTML trong template giải thích cell template và cấu trúc UI phức tạp.
9. **[Frontend Design Philosophy (Dropbox Pack)](.agents/rules/frontend-design-philosophy.md)**:
   - Kết hợp triết lý **Hallmark** (Anti-AI-slop, 8-state discipline), **Minimalist UI** (Warm canvas `#F7F5F2`, 1px borders, muted pastels), và **Frontend Design** theo chuẩn [DESIGN.md](DESIGN.md).
10. **[Command Runner (`justfile`)](.agents/documents/justfile-guide.md)**:
   - Sử dụng `just <command>` làm Single Entry Point cho các thao tác Build, Test, Format và quản lý EF Core Migrations đa schema.
   - Gõ `just` hoặc `just --list` tại thư mục gốc để tra cứu nhanh menu lệnh.
11. **[Anti-AI-Slop Standards](.agents/rules/anti-ai-slop.md)**:
   - Hệ thống 14 mã tiêu chuẩn chống code ẩu (`RULE-01` đến `RULE-14`) phân theo 4 trụ cột kiến trúc, xếp thứ tự ưu tiên CRIT $\rightarrow$ WARN:
     - 🏛️ **Hợp đồng & Ranh giới (CRIT)**: Khớp contract FE↔BE (RULE-01, ưu tiên cao nhất), không gán cứng host/port/cờ quyền (RULE-02), đủ khoá i18n và quyền (RULE-03).
     - ⚙️ **Luồng Thực Thi Thực Chất (CRIT)**: Cấm mock/stub trong luồng thật (RULE-04), cấm lộ chức năng rỗng (RULE-05), cấm nuốt lỗi báo thành công giả (RULE-06), quản lý exception/timeout chặt chẽ (RULE-07).
     - 🧹 **Chất Lượng Code & Tái Cấu Trúc (WARN)**: Xóa dead code (RULE-08), chống nhân bản code quá 3 lần - DRY (RULE-09), seam trả hằng phải có lộ trình thay thật (RULE-10).
     - 📝 **Tài Liệu & Kỷ Luật Git (WARN)**: Comment không kể lể lịch sử (RULE-11), tài liệu mô tả đúng code thật (RULE-12), làm theo pattern repo (RULE-13), diff tập trung không lan man (RULE-14).

---

## 3. Hệ thống Skills & Workflows Dự án (.agents/skills/ & .agents/workflows/)
- **`/review`** - **Code Review Đối Chiếu Rules**: `@[.agents/skills/review/SKILL.md]` (hoặc workflow `@[.agents/workflows/review.md]`)
  - **Mục đích**: Tự động rà soát, kiểm tra và đối chiếu các thay đổi (`git diff`, staged changes, hoặc PR) so với toàn bộ 10 bộ quy tắc cốt lõi trong `.agents/rules/` trước khi commit hoặc merge.
  - **Lệnh thực thi**: Gõ `/review`, `@review` hoặc yêu cầu rà soát vi phạm rules.

---

## 4. Tài liệu Kiến trúc Hệ thống (.agents/documents/)
- [Tổng quan Kiến trúc & Data Flow](.agents/documents/architecture-overview.md)
- [Hướng dẫn Quản lý Schema & Migrations trên SQL Server](.agents/documents/database-schema-guide.md)
- [Hướng dẫn Cấu hình Hangfire với MongoDB Storage](.agents/documents/hangfire-mongodb-guide.md)
- [Hướng dẫn Sử dụng 'justfile' Command Runner](.agents/documents/justfile-guide.md)

---

## 5. Quy ước Vị trí Lưu trữ Specs & Plans (.agents/specs/ & .agents/plans/)
Để duy trì tính nhất quán trên toàn bộ đội ngũ kỹ sư và các trợ lý AI (Antigravity, Cursor, Claude Code, Copilot, Codex...), toàn bộ tài liệu đặc tả thiết kế và kế hoạch thực thi phải tuân thủ nghiêm ngặt vị trí lưu trữ sau:

1. **Design Specs (Tài liệu Thiết kế / Đặc tả)**:
   - Vị trí: `.agents/specs/`
   - Đặt tên file: `YYYY-MM-DD-<topic>-design.md` (ví dụ: `.agents/specs/2026-11-21-sprint-2-kanban-tasks-background-jobs-design.md`)
   - Áp dụng khi dùng skill `brainstorming`, `/feature-kickstart`, hoặc bất kỳ luồng thiết kế kỹ thuật nào.
2. **Implementation Plans (Kế hoạch Triển khai)**:
   - Vị trí: `.agents/plans/`
   - Đặt tên file: `YYYY-MM-DD-<feature-name>.md` (ví dụ: `.agents/plans/2026-11-21-sprint-2-kanban-tasks-background-jobs.md`)
   - Áp dụng khi dùng skill `writing-plans`, `subagent-driven-development`, `executing-plans`, hoặc bất kỳ luồng lập kế hoạch nào.
3. ⛔ **Điều Cấm & Cảnh Báo Cho Mọi AI**:
   - ⛔ **CẤM** tạo thư mục `docs/superpowers/` hoặc lưu specs/plans ở thư mục gốc `docs/`.
   - 🔴 **Ghi đè mặc định (Override Defaults)**: Mặc định của các global skill Superpowers (`docs/superpowers/specs/`, `docs/superpowers/plans/`) **bị vô hiệu hóa** và ghi đè hoàn toàn bởi quy ước dự án tại mục này. Mọi AI khi chạy trong repository này bắt buộc phải lưu file vào `.agents/specs/` và `.agents/plans/`.

---

## 6. Cấu trúc Thư mục Dự án (Project Layout)
```text
PROJECT/
├── AGENTS.md                                     # Quy ước tối cao & Chỉ mục dành cho Agent
├── justfile                                      # Single Entry Point cho các lệnh Build, Test, Dev, Migrations
├── .agents/                                      # Không gian làm việc, rules và documents của Agent
│   ├── documents/                                # Tài liệu kiến trúc dài hạn (EF Core, Hangfire, Justfile...)
│   ├── plans/                                    # Kế hoạch thực thi Sprint / Feature (Implementation Plans)
│   ├── rules/                                    # Hệ thống 11 bộ quy chuẩn & Anti-AI-Slop (RULE-01 đến 14)
│   ├── skills/                                   # Kỹ năng cục bộ (Local skills)
│   ├── specs/                                    # Bản đặc tả thiết kế nghiệp vụ (Design Specs)
│   └── workflows/                                # Quy trình tự động hóa cục bộ
│
├── backend/                                      # .NET 9 Modular Monolith
│   ├── AgentCoreSystem.slnx
│   └── src/
│       ├── Host/AgentCoreSystem.Api/                  # Web API Host, Swagger OAS, GlobalExceptionHandler
│       ├── Shared/                               # Building Blocks (Kernel, Application, Infrastructure, Persistence)
│       └── Modules/                              # Phân hệ nghiệp vụ (System, Catalog, Order...)
│
└── frontend/                                     # Angular Standalone Modular Client
    └── src/
        └── app/
            ├── core/                             # Singleton Services, Auth, Interceptors, Layout Shell
            ├── shared/                           # Reusable UI Components, Directives, Pipes
            └── features/                         # Lazy-loaded Feature Slices (Auth, System, Dashboard...)
```

<!-- gitnexus:start -->
# GitNexus — Code Intelligence

This project is indexed by GitNexus as **agent-core-system** (3067 symbols, 7447 relationships, 81 execution flows). Use the GitNexus MCP tools to understand code, assess impact, and navigate safely.

> Index stale? Run `node .gitnexus/run.cjs analyze` from the project root — it auto-selects an available runner. No `.gitnexus/run.cjs` yet? `npx gitnexus analyze` (npm 11 crash → `npm i -g gitnexus`; #1939).

## Always Do

- **MUST run impact analysis before editing any symbol.** Before modifying a function, class, or method, run `impact({target: "symbolName", direction: "upstream"})` and report the blast radius (direct callers, affected processes, risk level) to the user.
- **MUST run `detect_changes()` before committing** to verify your changes only affect expected symbols and execution flows. For regression review, compare against the default branch: `detect_changes({scope: "compare", base_ref: "main"})`.
- **MUST warn the user** if impact analysis returns HIGH or CRITICAL risk before proceeding with edits.
- When exploring unfamiliar code, use `query({search_query: "concept"})` to find execution flows instead of grepping. It returns process-grouped results ranked by relevance.
- When you need full context on a specific symbol — callers, callees, which execution flows it participates in — use `context({name: "symbolName"})`.
- For security review, `explain({target: "fileOrSymbol"})` lists taint findings (source→sink flows; needs `analyze --pdg`).

## Never Do

- NEVER edit a function, class, or method without first running `impact` on it.
- NEVER ignore HIGH or CRITICAL risk warnings from impact analysis.
- NEVER rename symbols with find-and-replace — use `rename` which understands the call graph.
- NEVER commit changes without running `detect_changes()` to check affected scope.

## Resources

| Resource | Use for |
|----------|---------|
| `gitnexus://repo/agent-core-system/context` | Codebase overview, check index freshness |
| `gitnexus://repo/agent-core-system/clusters` | All functional areas |
| `gitnexus://repo/agent-core-system/processes` | All execution flows |
| `gitnexus://repo/agent-core-system/process/{name}` | Step-by-step execution trace |

## CLI

| Task | Read this skill file |
|------|---------------------|
| Understand architecture / "How does X work?" | `.claude/skills/gitnexus/gitnexus-exploring/SKILL.md` |
| Blast radius / "What breaks if I change X?" | `.claude/skills/gitnexus/gitnexus-impact-analysis/SKILL.md` |
| Trace bugs / "Why is X failing?" | `.claude/skills/gitnexus/gitnexus-debugging/SKILL.md` |
| Rename / extract / split / refactor | `.claude/skills/gitnexus/gitnexus-refactoring/SKILL.md` |
| Tools, resources, schema reference | `.claude/skills/gitnexus/gitnexus-guide/SKILL.md` |
| Index, status, clean, wiki CLI commands | `.claude/skills/gitnexus/gitnexus-cli/SKILL.md` |

<!-- gitnexus:end -->

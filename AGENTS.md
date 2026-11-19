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
   - Quy tắc comment XML Docs **What - When - Why**: Khai thác triệt để thẻ `<summary>` (What) và `<remarks>` (When & Why), cùng `<param>`, `<returns>`.
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
9. **[Command Runner (`justfile`)](.agents/documents/justfile-guide.md)**:
   - Sử dụng `just <command>` làm Single Entry Point cho các thao tác Build, Test, Format và quản lý EF Core Migrations đa schema.
   - Gõ `just` hoặc `just --list` tại thư mục gốc để tra cứu nhanh menu lệnh.

---

## 3. Tài liệu Kiến trúc Hệ thống (.agents/documents/)
- [Tổng quan Kiến trúc & Data Flow](.agents/documents/architecture-overview.md)
- [Hướng dẫn Quản lý Schema & Migrations trên SQL Server](.agents/documents/database-schema-guide.md)
- [Hướng dẫn Cấu hình Hangfire với MongoDB Storage](.agents/documents/hangfire-mongodb-guide.md)
- [Hướng dẫn Sử dụng 'justfile' Command Runner](.agents/documents/justfile-guide.md)

---

## 4. Cấu trúc Thư mục Dự án (Project Layout)
```text
PROJECT/
├── AGENTS.md                                     # Quy ước tối cao & Chỉ mục dành cho Agent
├── justfile                                      # Single Entry Point cho các lệnh Build, Test, Dev, Migrations
├── .agents/                                      # Không gian làm việc, rules và documents của Agent
│   ├── rules/
│   ├── skills/
│   └── documents/
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


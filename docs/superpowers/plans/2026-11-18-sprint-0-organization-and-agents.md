# Sprint 0: Nền tảng Tổ chức & Quản lý Agent (Org & Identity) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Xây dựng hoàn chỉnh Phân hệ `Module.Organization` (schema `org`) quản lý cơ cấu Doanh nghiệp, Phòng ban, Kênh nhóm và Hồ sơ Nhân sự AI Agent, kèm giao diện Angular Standalone quản trị trực quan.

**Architecture:** Áp dụng chuẩn Clean Architecture cho `Module.Organization` (Domain, Application, Infrastructure, Presentation) tích hợp vào Modular Monolith Host qua MediatR và EF Core Schema-per-module. Frontend Angular sử dụng Standalone Components và Signals store để quản lý state phản ứng.

**Tech Stack:** .NET 9, EF Core (SQL Server schema `org`, UUIDv7), MediatR, FluentValidation, Asp.Versioning, OpenAPI v3, Angular 19+ Standalone, Signals, SCSS.

**Spec:** [docs/superpowers/specs/2026-11-18-agent-core-system-design.md](file:///c:/Workplaces/PROJECT/AgentCoreSystem/docs/superpowers/specs/2026-11-18-agent-core-system-design.md)

## Global Constraints
- Bắt buộc sử dụng Block-scoped namespaces (`namespace AgentCoreSystem.Module.Organization { ... }`) cho toàn bộ file C#.
- Sử dụng C# Primary Constructors cho việc khai báo Dependency Injection.
- Mọi Entity kế thừa `BaseEntity` (mặc định UUIDv7 từ `Guid.CreateVersion7()`).
- Toàn bộ bảng dữ liệu nằm trên schema SQL Server `org`, bảng lịch sử migration là `__EFMigrationsHistory_Organization`.
- Sử dụng `Result<T>` và `Error` cho Use Cases, Controllers trả về RFC 7807 ProblemDetails khi lỗi và `ApiResponse<T>` khi thành công.
- Giữ nguyên header bản quyền MIT của tác giả Phong Minh Đặng trên các file tạo mới.

## Review Focus
1. Tên phòng ban hoặc kênh nhóm rỗng hoặc trùng lặp trong cùng một khối cha -> Trả về `Result.Failure(Error.Conflict(...))`.
2. AgentProfile thiếu thông tin bắt buộc (SystemPrompt rỗng hoặc ModelName không hợp lệ) -> Bị chặn bởi FluentValidation.
3. Thêm một Member vào Kênh với ID không tồn tại -> Trả về `Result.Failure(Error.NotFound(...))`.
4. Gán trùng Agent vào cùng một Kênh nhóm 2 lần -> Bị chặn bởi Unique Index hoặc validation nghiệp vụ.
5. Frontend Angular xử lý trạng thái Loading và Empty State khi chưa có phòng ban nào -> Hiển thị Empty state component thay vì crash UI.

---

## FEATURE 0.1: KHỞI TẠO DỰ ÁN & DOMAIN ENTITIES (SCHEMA ORG)

### Task 1: Khởi tạo Solution Projects cho Module.Organization & Đăng ký Solution

**Files:**
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Domain/AgentCoreSystem.Module.Organization.Domain.csproj`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Application/AgentCoreSystem.Module.Organization.Application.csproj`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Infrastructure/AgentCoreSystem.Module.Organization.Infrastructure.csproj`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Presentation/AgentCoreSystem.Module.Organization.Presentation.csproj`
- Modify: `backend/AgentCoreSystem.slnx`
- Modify: `backend/src/Host/AgentCoreSystem.Api/AgentCoreSystem.Api.csproj`

**Interfaces:**
- Produces: Project references linking Clean Architecture layers:
  - `Domain` references `AgentCoreSystem.Shared.Kernel`
  - `Application` references `Domain`, `AgentCoreSystem.Shared.Application`
  - `Infrastructure` references `Domain`, `Application`, `AgentCoreSystem.Shared.Persistence`
  - `Presentation` references `Application`, `AgentCoreSystem.Shared.Infrastructure`
  - `Host/Api` references `Infrastructure`, `Presentation`

- [x] **Step 1: Tạo thư mục và csproj cho 4 tầng của Module.Organization**
- [x] **Step 2: Thêm các project vào `backend/AgentCoreSystem.slnx` dưới thư mục `/src/Modules/Organization/`**
- [x] **Step 3: Chạy `dotnet build backend/AgentCoreSystem.slnx` để kiểm tra liên kết project thành công**
- [x] **Step 4: Commit**
  ```bash
  git add backend/src/Modules/Organization/ backend/AgentCoreSystem.slnx backend/src/Host/AgentCoreSystem.Api/AgentCoreSystem.Api.csproj
  git commit -m "feat(org): scaffold Organization module clean architecture projects"
  ```

---

### Task 2: Xây dựng Domain Entities & Enums (Schema org)

**Files:**
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Domain/Enums/MemberType.cs`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Domain/Enums/ModelProviderType.cs`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Domain/Entities/Department.cs`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Domain/Entities/TeamChannel.cs`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Domain/Entities/Member.cs`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Domain/Entities/AgentProfile.cs`
- Create: `backend/tests/AgentCoreSystem.UnitTests/Organization/OrganizationDomainTests.cs`

**Interfaces:**
- Produces:
  - `enum MemberType { Human = 1, Agent = 2 }`
  - `enum ModelProviderType { Gemini = 1, OpenAI = 2, Claude = 3, Ollama = 4 }`
  - `class Department : BaseEntity { string Name, string? Description, ICollection<TeamChannel> Channels }`
  - `class TeamChannel : BaseEntity { Guid DepartmentId, string Name, string? Description, ICollection<Member> Members }`
  - `class Member : BaseEntity { Guid ChannelId, MemberType Type, Guid? UserId, Guid? AgentProfileId, string DisplayName }`
  - `class AgentProfile : BaseEntity { string Name, string? AvatarUrl, ModelProviderType Provider, string ModelName, string SystemPrompt, string? PrivateMemoryNotes, decimal Temperature }`

- [x] **Step 1: Viết Unit Test kiểm tra khởi tạo Entity UUIDv7 và default values**
- [x] **Step 2: Chạy test để xác nhận FAIL**
  Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter FullyQualifiedName~OrganizationDomainTests`
- [x] **Step 3: Triển khai các Enums và Entities trong `AgentCoreSystem.Module.Organization.Domain`**
- [x] **Step 4: Chạy lại test để xác nhận PASS**
- [x] **Step 5: Commit**
  ```bash
  git add backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Domain/ backend/tests/AgentCoreSystem.UnitTests/Organization/
  git commit -m "feat(org): define domain entities and enums for Organization module"
  ```

---

## FEATURE 0.2: CẤU HÌNH PERSISTENCE & EF CORE MIGRATIONS (SCHEMA ORG)

### Task 3: OrganizationDbContext, Fluent API Configurations & Migrations

**Files:**
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Infrastructure/Persistence/OrganizationDbContext.cs`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Infrastructure/Persistence/Configurations/DepartmentConfiguration.cs`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Infrastructure/Persistence/Configurations/TeamChannelConfiguration.cs`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Infrastructure/Persistence/Configurations/MemberConfiguration.cs`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Infrastructure/Persistence/Configurations/AgentProfileConfiguration.cs`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Infrastructure/Persistence/Migrations/`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Produces:
  - `OrganizationDbContext : BaseDbContext` with schema `org`
  - Unique Index: `(DepartmentId, Name)` on `TeamChannel`, `(ChannelId, DisplayName)` on `Member`
  - Extension `IServiceCollection AddOrganizationInfrastructure(this IServiceCollection services, IConfiguration configuration)`

- [x] **Step 1: Định nghĩa `OrganizationDbContext` kế thừa `BaseDbContext` với `builder.HasDefaultSchema("org");`**
- [x] **Step 2: Viết các Fluent API Configuration mapping tables sang schema `org`**
- [x] **Step 3: Tạo EF Core Migration đầu tiên `Initial_Organization_Schema`**
  Run: `dotnet ef migrations add Initial_Organization_Schema --project backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Infrastructure --startup-project backend/src/Host/AgentCoreSystem.Api --output-dir Persistence/Migrations`
- [x] **Step 4: Đăng ký DbContext vào `AddOrganizationInfrastructure` và build test thành công**
- [x] **Step 5: Commit**
  ```bash
  git add backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Infrastructure/
  git commit -m "feat(org): configure OrganizationDbContext and generate initial migrations"
  ```

---

## FEATURE 0.3: APPLICATION CQRS COMMANDS & QUERIES (APPLICATION LAYER)

### Task 4: Quản lý Phòng ban & Kênh nhóm (Department & Channel CQRS)

**Files:**
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Application/Departments/Commands/CreateDepartmentCommand.cs`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Application/Departments/Commands/CreateDepartmentCommandValidator.cs`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Application/Departments/Commands/CreateDepartmentCommandHandler.cs`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Application/Departments/Queries/GetDepartmentsQuery.cs`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Application/Channels/Commands/CreateTeamChannelCommand.cs`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Application/Channels/Queries/GetChannelsByDepartmentQuery.cs`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Application/DependencyInjection.cs`
- Create: `backend/tests/AgentCoreSystem.UnitTests/Organization/DepartmentCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `OrganizationDbContext`, `Result<T>`, `Error`
- Produces:
  - `CreateDepartmentCommand(string Name, string? Description) : IRequest<Result<Guid>>`
  - `CreateTeamChannelCommand(Guid DepartmentId, string Name, string? Description) : IRequest<Result<Guid>>`
  - `GetDepartmentsQuery() : IRequest<Result<IReadOnlyList<DepartmentDto>>>`
  - `GetChannelsByDepartmentQuery(Guid DepartmentId) : IRequest<Result<IReadOnlyList<TeamChannelDto>>>`

- [x] **Step 1: Viết Unit Test cho CreateDepartmentCommandHandler (Happy path & Validation failure)**
- [x] **Step 2: Chạy test để xác nhận FAIL**
- [x] **Step 3: Triển khai DTOs, Commands, Queries, Validators, Handlers và `AddOrganizationApplication`**
- [x] **Step 4: Chạy lại test để xác nhận PASS**
- [x] **Step 5: Commit**
  ```bash
  git add backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Application/ backend/tests/AgentCoreSystem.UnitTests/Organization/
  git commit -m "feat(org): implement Department and TeamChannel CQRS handlers"
  ```

---

### Task 5: Quản lý Hồ sơ Nhân sự AI Agent (AgentProfile CQRS)

**Files:**
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Application/Agents/Commands/CreateAgentProfileCommand.cs`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Application/Agents/Commands/CreateAgentProfileCommandValidator.cs`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Application/Agents/Commands/CreateAgentProfileCommandHandler.cs`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Application/Agents/Commands/AssignMemberToChannelCommand.cs`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Application/Agents/Queries/GetAgentProfilesQuery.cs`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Application/Agents/Queries/GetChannelMembersQuery.cs`
- Create: `backend/tests/AgentCoreSystem.UnitTests/Organization/AgentProfileCommandHandlerTests.cs`

**Interfaces:**
- Produces:
  - `CreateAgentProfileCommand(string Name, string? AvatarUrl, ModelProviderType Provider, string ModelName, string SystemPrompt, decimal Temperature) : IRequest<Result<Guid>>`
  - `AssignMemberToChannelCommand(Guid ChannelId, MemberType Type, Guid? UserId, Guid? AgentProfileId, string DisplayName) : IRequest<Result<Guid>>`
  - `GetAgentProfilesQuery() : IRequest<Result<IReadOnlyList<AgentProfileDto>>>`

- [x] **Step 1: Viết Unit Test cho CreateAgentProfile và AssignMemberToChannel**
- [x] **Step 2: Chạy test để xác nhận FAIL**
- [x] **Step 3: Triển khai Commands, Handlers, Validators kiểm tra SystemPrompt không rỗng, ModelName hợp lệ**
- [x] **Step 4: Chạy lại test để xác nhận PASS**
- [x] **Step 5: Commit**
  ```bash
  git add backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Application/Agents/ backend/tests/AgentCoreSystem.UnitTests/Organization/
  git commit -m "feat(org): implement AgentProfile creation and channel assignment CQRS"
  ```

---

## FEATURE 0.4: WEB API PRESENTATION & HOST INTEGRATION

### Task 6: Controllers, OpenAPI OAS v3 & Host Registration

**Files:**
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Presentation/Controllers/v1/DepartmentsController.cs`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Presentation/Controllers/v1/TeamChannelsController.cs`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Presentation/Controllers/v1/AgentProfilesController.cs`
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Presentation/DependencyInjection.cs`
- Create: `backend/src/Host/AgentCoreSystem.Api/Extensions/OrganizationModuleExtensions.cs`
- Modify: `backend/src/Host/AgentCoreSystem.Api/Program.cs`

**Interfaces:**
- Produces:
  - `POST /api/v1/departments`, `GET /api/v1/departments`
  - `POST /api/v1/departments/{departmentId}/channels`, `GET /api/v1/departments/{departmentId}/channels`
  - `POST /api/v1/agent-profiles`, `GET /api/v1/agent-profiles`
  - `POST /api/v1/channels/{channelId}/members`
  - Full OpenAPI documentation with `<summary>`, `<remarks>`, `[ProducesResponseType]`

- [x] **Step 1: Viết các Controllers v1 kế thừa BaseApiController, sử dụng MediatR `ISender` và `ToProblemDetails()`**
- [x] **Step 2: Tạo `OrganizationModuleExtensions.cs` đăng ký Application, Infrastructure, Presentation vào Host**
- [x] **Step 3: Gọi `builder.Services.AddOrganizationModule(builder.Configuration);` trong `Program.cs`**
- [x] **Step 4: Chạy `dotnet build backend/AgentCoreSystem.slnx` đảm bảo 0 errors, 0 warnings**
- [x] **Step 5: Commit**
  ```bash
  git add backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Presentation/ backend/src/Host/AgentCoreSystem.Api/
  git commit -m "feat(org): expose Organization REST API v1 endpoints with OAS docs"
  ```

---

## FEATURE 0.5: GIAO DIỆN QUẢN TRỊ ANGULAR STANDALONE (SIGNALS)

### Task 7: Angular Models, Service & Signals Store cho Organization

**Files:**
- Create: `frontend/src/app/features/organization/models/organization.models.ts`
- Create: `frontend/src/app/features/organization/services/organization.service.ts`
- Create: `frontend/src/app/features/organization/store/organization.store.ts`

**Interfaces:**
- Produces:
  - Interfaces: `Department`, `TeamChannel`, `Member`, `AgentProfile`
  - `OrganizationService`: `getDepartments()`, `createDepartment()`, `getChannels()`, `createChannel()`, `getAgentProfiles()`, `createAgentProfile()`
  - `OrganizationStore`: Signals `departments = signal<Department[]>([])`, `selectedDepartment = signal<Department | null>(null)`, `agentProfiles = signal<AgentProfile[]>([])`, `loading = signal<boolean>(false)`

- [x] **Step 1: Định nghĩa Typescript models khớp với C# DTOs**
- [x] **Step 2: Viết `OrganizationService` sử dụng Angular `HttpClient`**
- [x] **Step 3: Viết `OrganizationStore` quản lý trạng thái bằng Angular Signals**
- [x] **Step 4: Commit**
  ```bash
  git add frontend/src/app/features/organization/
  git commit -m "feat(frontend): create models, service and signals store for organization feature"
  ```

---

### Task 8: Angular UI Components (Cây Phòng Ban & Cấu Hình Hồ Sơ Agent)

**Files:**
- Create: `frontend/src/app/features/organization/pages/organization-shell/organization-shell.component.ts`
- Create: `frontend/src/app/features/organization/pages/organization-shell/organization-shell.component.html`
- Create: `frontend/src/app/features/organization/pages/organization-shell/organization-shell.component.scss`
- Create: `frontend/src/app/features/organization/components/agent-profile-modal/agent-profile-modal.component.ts`
- Create: `frontend/src/app/features/organization/components/agent-profile-modal/agent-profile-modal.component.html`
- Modify: `frontend/src/app/app.routes.ts`

**Interfaces:**
- Produces:
  - Standalone Component `OrganizationShellComponent` với OnPush:
    - Cột trái: Danh sách Phòng ban & Kênh nhóm (Tree view + Nút thêm mới)
    - Cột phải: Danh sách thành viên Kênh & Modal cấu hình Agent (Chọn model: Gemini/OpenAI, nhập System prompt, thanh trượt Temperature)
  - Lazy-loaded route: `/organization` in `app.routes.ts`

- [x] **Step 1: Xây dựng UI Organization Shell hiển thị cây phòng ban và danh sách kênh**
- [x] **Step 2: Xây dựng Modal tạo/sửa hồ sơ Agent (AgentProfileModalComponent)**
- [x] **Step 3: Đăng ký route lazy-load `/organization` trong `app.routes.ts`**
- [x] **Step 4: Chạy `npm run build` trong thư mục `frontend` đảm bảo build thành công 0 lỗi**
- [x] **Step 5: Commit**
  ```bash
  git add frontend/src/app/features/organization/ frontend/src/app/app.routes.ts
  git commit -m "feat(frontend): implement Organization tree view and Agent profile modal UI"
  ```

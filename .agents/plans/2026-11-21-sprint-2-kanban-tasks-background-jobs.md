# Sprint 2: Bảng Kanban & Tác vụ Chạy Ngầm (Tasks & Background Jobs) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Xây dựng hoàn chỉnh Phân hệ `Module.Tasks` (schema `task`), quản lý vòng đời tác vụ theo bảng Kanban 4 cột (`To Do`, `In Progress`, `In Review`, `Done`), tích hợp Hangfire MongoDB chạy ngầm cho Agent, cơ chế Cổng Nghiệm Thu Con Người (Review Gate: Human-in-the-Loop) nạp phản hồi vào `PrivateMemory` của Agent, SignalR TasksHub thời gian thực (`/hubs/tasks`) và giao diện Angular Standalone Kanban Board kéo thả với `@angular/cdk/drag-drop`.

**Architecture:** Clean Architecture 4 tầng cho `Module.Tasks` tích hợp vào Modular Monolith Host, độc lập hoàn toàn với `Module.Chat` và `Module.Organization` (giao tiếp qua MediatR, không FK chéo schema). Hangfire background job (`ITaskAgentJobRunner`) xử lý tác vụ ngầm cho Agent và xuất artifact ra ổ đĩa cục bộ. Realtime Hub `/hubs/tasks` đồng bộ bảng Kanban. Frontend Angular 19+ Standalone quản lý state bằng Signals và OnPush theo chuẩn Minimalist UI (`#F7F5F2`).

**Tech Stack:** .NET 9, EF Core (SQL Server schema `task`, UUIDv7), Hangfire (MongoDB storage), SignalR, MediatR, FluentValidation, OpenAPI v3, Angular 19+ Standalone, Angular Signals, `@angular/cdk/drag-drop`, Tailwind CSS / SCSS.

**Spec:** [.agents/specs/2026-11-21-sprint-2-kanban-tasks-background-jobs-design.md](file:///c:/Workplaces/PROJECT/AgentCoreSystem/.agents/specs/2026-11-21-sprint-2-kanban-tasks-background-jobs-design.md)

---

## Global Constraints
- Bắt buộc sử dụng Block-scoped namespaces (`namespace AgentCoreSystem.Module.Tasks { ... }`) cho toàn bộ file C#.
- Sử dụng C# Primary Constructors cho việc khai báo Dependency Injection.
- Mọi Entity kế thừa `BaseEntity` (mặc định UUIDv7 từ `Guid.CreateVersion7()`).
- Toàn bộ bảng dữ liệu nằm trên schema SQL Server `task`, bảng lịch sử migration là `__EFMigrationsHistory_Tasks`.
- **CẤM HARDCODE**: Tuyệt đối không hardcode ID, URL hay tên cố định trong code.
- **CẤM AGENT TỰ DUYỆT (SELF-APPROVAL)**: Agent không được phép gọi action `Approve` để chuyển task từ `InReview` sang `Done`. Thao tác duyệt chỉ dành cho `Human`.
- Sử dụng `Result<T>` và `Error` cho Use Cases, Controllers trả về RFC 7807 ProblemDetails khi lỗi và `ApiResponse<T>` khi thành công.
- Giữ nguyên header bản quyền MIT của tác giả Phong Minh Đặng trên các file tạo mới.

---

## Review Focus
1. **Agent tự chuyển sang Done:** Thao tác gọi `ReviewTaskCommand` với quyết định `Approve` nhưng người thực hiện là Agent (`IsAgent == true`) -> Hệ thống từ chối dứt khoát với lỗi `Error.Forbidden` hoặc `Error.Validation`.
2. **Kéo thả trạng thái không hợp lệ (Invalid State Transition):** Thao tác kéo thẻ từ `ToDo` nhảy thẳng sang `Done` (bỏ qua `InProgress` và `InReview`) -> Domain State Machine chặn lại và trả về `Error.Validation`.
3. **Hangfire Job thất bại hoặc Timeout:** Khi Agent LLM bị lỗi kết nối hoặc ngoại lệ trong job ngầm -> Ghi log lỗi vào `TaskActivityLog`, chuyển trạng thái task sang `Failed`, và bắn thông báo qua SignalR thay vì để job treo vô hạn.
4. **Mất kết nối SignalR khi đang kéo thả:** Client mất kết nối mạng tạm thời -> Reconnect tự động và đồng bộ lại danh sách task đầy đủ từ REST API `GET /api/v1/channels/{channelId}/tasks`.
5. **Cập nhật nhận xét vào PrivateMemory của Agent:** Khi con người chọn `RequestRevision` kèm lý do góp ý -> MediatR command gửi sang `Module.Organization` để nối chuỗi nhận xét vào `PrivateMemoryNotes` của Agent Profile tương ứng.

---

## Task 1: Khởi Tạo Dự Án Clean Architecture Cho Module.Tasks

**Files:**
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Domain/AgentCoreSystem.Module.Tasks.Domain.csproj`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Application/AgentCoreSystem.Module.Tasks.Application.csproj`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Infrastructure/AgentCoreSystem.Module.Tasks.Infrastructure.csproj`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Presentation/AgentCoreSystem.Module.Tasks.Presentation.csproj`
- Modify: `backend/AgentCoreSystem.slnx`
- Modify: `backend/src/Host/AgentCoreSystem.Api/AgentCoreSystem.Api.csproj`

**Interfaces:**
- Produces: Cấu trúc 4 project chuẩn Clean Architecture tham chiếu đến `Shared.Kernel`, `Shared.Application`, `Shared.Infrastructure`, `Shared.Persistence`.

- [ ] **Step 1: Tạo 4 file .csproj cho Module.Tasks với đầy đủ các reference**
- [ ] **Step 2: Thêm các project mới vào `AgentCoreSystem.slnx` và thêm reference vào `AgentCoreSystem.Api.csproj`**
- [ ] **Step 3: Chạy `dotnet build` để xác nhận cấu hình project hợp lệ**
  Run: `dotnet build backend/AgentCoreSystem.slnx`
  Expected: Build succeeded with 0 errors.
- [ ] **Step 4: Commit**
  ```bash
  git add backend/src/Modules/Tasks/ backend/AgentCoreSystem.slnx backend/src/Host/AgentCoreSystem.Api/AgentCoreSystem.Api.csproj
  git commit -m "feat(tasks): scaffold clean architecture projects for Tasks module"
  ```

---

## Task 2: Domain Entities, Enums & State Machine (Schema `task`)

**Files:**
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Domain/Enums/TaskItemStatus.cs`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Domain/Enums/TaskPriority.cs`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Domain/Entities/TaskItem.cs`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Domain/Entities/TaskAssignment.cs`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Domain/Entities/TaskArtifact.cs`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Domain/Entities/TaskActivityLog.cs`
- Create: `backend/tests/AgentCoreSystem.UnitTests/Tasks/TaskItemDomainTests.cs`

**Interfaces:**
- Produces:
  - `enum TaskItemStatus { ToDo, InProgress, InReview, Done, Failed }`
  - `enum TaskPriority { Low, Medium, High, Urgent }`
  - `class TaskItem : BaseEntity`:
    - `static Result<TaskItem> Create(Guid channelId, string taskCode, string title, string description, TaskPriority priority, DateTimeOffset? dueDate, Guid creatorMemberId)`
    - `Result MoveTo(TaskItemStatus newStatus, Guid actorMemberId, bool isActorAgent, string? comment = null)`
    - `Result AssignTo(Guid memberId, bool isAgent, string roleInTask, Guid actorMemberId)`
    - `Result AddArtifact(string fileName, string filePath, string fileType, long fileSize, string summary, Guid creatorMemberId)`

- [ ] **Step 1: Viết Unit Tests cho `TaskItemDomainTests` kiểm tra State Machine:**
  - Chuyển `ToDo` -> `InProgress`: Hợp lệ.
  - Chuyển `InProgress` -> `InReview`: Hợp lệ.
  - Chuyển `InReview` -> `Done` bởi Agent: Bị từ chối (`Error.Forbidden`).
  - Chuyển `InReview` -> `Done` bởi Human: Hợp lệ.
  - Chuyển `InReview` -> `InProgress` (Request Revision): Hợp lệ.
  - Nhảy cóc `ToDo` -> `Done`: Bị từ chối (`Error.Validation`).
- [ ] **Step 2: Chạy test để xác nhận FAIL**
  Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter FullyQualifiedName~TaskItemDomainTests`
  Expected: FAIL (types not defined).
- [ ] **Step 3: Triển khai các Enums và Entities trong `AgentCoreSystem.Module.Tasks.Domain`**
- [ ] **Step 4: Chạy test để xác nhận PASS**
  Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter FullyQualifiedName~TaskItemDomainTests`
  Expected: PASS with 100% test success.
- [ ] **Step 5: Commit**
  ```bash
  git add backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Domain/ backend/tests/AgentCoreSystem.UnitTests/Tasks/
  git commit -m "feat(tasks): implement TaskItem domain entities and state machine"
  ```

---

## Task 3: EF Core DbContext, Configurations & Initial Migration (Schema `task`)

**Files:**
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Application/Contracts/ITasksDbContext.cs`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Infrastructure/Persistence/TasksDbContext.cs`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Infrastructure/Persistence/Configurations/TaskItemConfiguration.cs`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Infrastructure/Persistence/Configurations/TaskAssignmentConfiguration.cs`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Infrastructure/Persistence/Configurations/TaskArtifactConfiguration.cs`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Infrastructure/Persistence/Configurations/TaskActivityLogConfiguration.cs`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Infrastructure/Persistence/Migrations/...`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Infrastructure/DependencyInjection.cs`
- Modify: `justfile` (bổ sung command `migrate-tasks`)

**Interfaces:**
- Produces:
  - `interface ITasksDbContext { DbSet<TaskItem> Tasks; DbSet<TaskAssignment> TaskAssignments; DbSet<TaskArtifact> TaskArtifacts; DbSet<TaskActivityLog> TaskActivityLogs; Task<int> SaveChangesAsync(CancellationToken ct); }`
  - EF Core schema `task` và migration history table `__EFMigrationsHistory_Tasks`.

- [ ] **Step 1: Định nghĩa `ITasksDbContext` và các cấu hình Entity Configuration với schema `task`**
- [ ] **Step 2: Triển khai `TasksDbContext` kế thừa `BaseDbContext`, cấu hình bảng lịch sử `__EFMigrationsHistory_Tasks`**
- [ ] **Step 3: Tạo Initial Migration cho Module.Tasks**
  Run: `dotnet ef migrations add InitialTasksMigration --project backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Infrastructure --startup-project backend/src/Host/AgentCoreSystem.Api --context TasksDbContext --output-dir Persistence/Migrations`
- [ ] **Step 4: Cập nhật `justfile` với lệnh `migrate-tasks` và chạy `dotnet build`**
- [ ] **Step 5: Commit**
  ```bash
  git add backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Application/Contracts/ backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Infrastructure/ justfile
  git commit -m "feat(tasks): configure TasksDbContext and generate initial migrations on schema task"
  ```

---

## Task 4: CQRS Commands, Queries & Cross-Module Feedback Loop

**Files:**
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Application/Tasks/DTOs/TaskDto.cs`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Application/Tasks/DTOs/TaskDetailDto.cs`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Application/Tasks/Commands/CreateTaskCommand.cs`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Application/Tasks/Commands/MoveTaskStatusCommand.cs`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Application/Tasks/Commands/AssignTaskCommand.cs`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Application/Tasks/Commands/ReviewTaskCommand.cs`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Application/Tasks/Queries/GetChannelTasksQuery.cs`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Application/Tasks/Queries/GetTaskByIdQuery.cs`
- Modify: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Application/AgentProfiles/Commands/AppendAgentMemoryCommand.cs` (nếu chưa có)
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Application/DependencyInjection.cs`
- Create: `backend/tests/AgentCoreSystem.UnitTests/Tasks/TasksApplicationTests.cs`

**Interfaces:**
- Produces:
  - `record CreateTaskCommand(Guid ChannelId, string Title, string Description, TaskPriority Priority, DateTimeOffset? DueDate, Guid? AssigneeMemberId, bool? IsAgent, Guid CreatorUserId) : ICommand<TaskDto>`
  - `record MoveTaskStatusCommand(Guid TaskId, TaskItemStatus NewStatus, Guid UserId, string? Comment) : ICommand<TaskDto>`
  - `record AssignTaskCommand(Guid TaskId, Guid AssigneeMemberId, bool IsAgent, string RoleInTask, Guid UserId) : ICommand<TaskDto>`
  - `record ReviewTaskCommand(Guid TaskId, bool IsApproved, string? Feedback, Guid UserId) : ICommand<TaskDto>`
  - `record GetChannelTasksQuery(Guid ChannelId) : IQuery<List<TaskDto>>`
  - `record GetTaskByIdQuery(Guid TaskId) : IQuery<TaskDetailDto>`

- [ ] **Step 1: Viết Unit Tests cho CQRS Handlers trong `TasksApplicationTests` (mock `ITasksDbContext` và MediatR)**
- [ ] **Step 2: Chạy test để xác nhận FAIL**
  Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter FullyQualifiedName~TasksApplicationTests`
- [ ] **Step 3: Triển khai các DTOs, Commands, Queries và Handlers trong `Module.Tasks.Application`**
- [ ] **Step 4: Chạy lại test để xác nhận PASS**
  Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter FullyQualifiedName~TasksApplicationTests`
- [ ] **Step 5: Commit**
  ```bash
  git add backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Application/ backend/src/Modules/Organization/ backend/tests/AgentCoreSystem.UnitTests/Tasks/
  git commit -m "feat(tasks): implement CQRS commands, queries, and cross-module feedback loop"
  ```

---

## Task 5: Hangfire Background Job & Agent Task Execution Engine

**Files:**
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Application/Contracts/ITaskAgentJobRunner.cs`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Infrastructure/Jobs/TaskAgentJobRunner.cs`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Infrastructure/Storage/LocalArtifactStorageService.cs`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Application/Contracts/IArtifactStorageService.cs`
- Create: `backend/tests/AgentCoreSystem.UnitTests/Tasks/TaskAgentJobRunnerTests.cs`

**Interfaces:**
- Produces:
  - `interface ITaskAgentJobRunner { Task ExecuteTaskAsync(Guid taskId, CancellationToken ct); }`
  - `interface IArtifactStorageService { Task<string> SaveArtifactAsync(Guid channelId, Guid taskId, string fileName, string content, CancellationToken ct); }`

- [ ] **Step 1: Viết Unit Tests cho `TaskAgentJobRunnerTests` kiểm tra: khi job chạy, gọi `ILLMClient` sinh artifact markdown, lưu file, và đổi task status sang `InReview`**
- [ ] **Step 2: Chạy test để xác nhận FAIL**
- [ ] **Step 3: Triển khai `LocalArtifactStorageService` (lưu file vào `Storage/Artifacts/`) và `TaskAgentJobRunner`**
- [ ] **Step 4: Chạy test để xác nhận PASS**
  Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter FullyQualifiedName~TaskAgentJobRunnerTests`
- [ ] **Step 5: Commit**
  ```bash
  git add backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Application/Contracts/ backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Infrastructure/Jobs/ backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Infrastructure/Storage/ backend/tests/AgentCoreSystem.UnitTests/Tasks/
  git commit -m "feat(tasks): implement Hangfire TaskAgentJobRunner and artifact storage"
  ```

---

## Task 6: SignalR Tasks Realtime Hub, REST API & Host Registration

**Files:**
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Infrastructure/Realtime/SignalRTasksHub.cs`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Application/Contracts/ITasksRealtimeNotifier.cs`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Infrastructure/Realtime/TasksRealtimeNotifier.cs`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Presentation/Controllers/v1/TasksController.cs`
- Create: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Presentation/DependencyInjection.cs`
- Modify: `backend/src/Host/AgentCoreSystem.Api/Program.cs` (Map `/hubs/tasks`, AddTasksModule)

**Interfaces:**
- Produces:
  - Endpoint SignalR: `/hubs/tasks`
  - Events: `TaskCreated`, `TaskStatusChanged`, `TaskAssigned`, `TaskArtifactAdded`, `TaskReviewDecision`
  - REST Endpoints:
    - `GET /api/v1/channels/{channelId}/tasks`
    - `GET /api/v1/tasks/{taskId}`
    - `POST /api/v1/channels/{channelId}/tasks`
    - `PATCH /api/v1/tasks/{taskId}/status`
    - `POST /api/v1/tasks/{taskId}/assign`
    - `POST /api/v1/tasks/{taskId}/review`
    - `GET /api/v1/tasks/{taskId}/artifacts/{artifactId}/download`

- [ ] **Step 1: Triển khai `SignalRTasksHub` và `TasksRealtimeNotifier`**
- [ ] **Step 2: Triển khai `TasksController` với đầy đủ OpenAPI XML Docs tiếng Việt theo chuẩn OAS v3**
- [ ] **Step 3: Đăng ký `AddTasksModule` và ánh xạ `app.MapTasksHub()` trong `Program.cs`**
- [ ] **Step 4: Chạy `dotnet build` và kiểm tra Swagger endpoint**
  Run: `dotnet build backend/AgentCoreSystem.slnx`
- [ ] **Step 5: Commit**
  ```bash
  git add backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Infrastructure/Realtime/ backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Presentation/ backend/src/Host/AgentCoreSystem.Api/
  git commit -m "feat(tasks): expose Tasks REST API v1, SignalR TasksHub, and register module into Host"
  ```

---

## Task 7: Angular Frontend Services, Models & Signals Store

**Files:**
- Create: `frontend/src/app/features/tasks/models/task.model.ts`
- Create: `frontend/src/app/features/tasks/services/tasks-http.service.ts`
- Create: `frontend/src/app/features/tasks/services/tasks-realtime.service.ts`
- Create: `frontend/src/app/features/tasks/store/tasks.store.ts`

**Interfaces:**
- Produces:
  - `interface TaskItemDto { id: string; channelId: string; taskCode: string; title: string; description: string; status: TaskItemStatus; priority: TaskPriority; ... }`
  - `TasksHttpService` gọi REST API v1.
  - `TasksRealtimeService` kết nối SignalR `/hubs/tasks`, xử lý tự động reconnect.
  - `TasksStore` quản lý signals: `tasks()`, `selectedTask()`, `todoTasks()`, `inProgressTasks()`, `inReviewTasks()`, `doneTasks()`.

- [ ] **Step 1: Định nghĩa Types và Models trong `task.model.ts`**
- [ ] **Step 2: Triển khai `TasksHttpService` tương thích `ApiResponse<T>` và `Result`**
- [ ] **Step 3: Triển khai `TasksRealtimeService` với SignalR HubConnection**
- [ ] **Step 4: Triển khai `TasksStore` sử dụng Angular Signals và `computed` cho các cột Kanban**
- [ ] **Step 5: Chạy `npm run build` trong `frontend/` để kiểm tra compile TypeScript**
- [ ] **Step 6: Commit**
  ```bash
  git add frontend/src/app/features/tasks/
  git commit -m "feat(frontend): create tasks models, HTTP service, SignalR realtime service, and signals store"
  ```

---

## Task 8: Angular Kanban Board UI & Human-in-the-Loop Review Gate

**Files:**
- Modify: `frontend/package.json` (thêm `@angular/cdk` nếu chưa có)
- Create: `frontend/src/app/features/tasks/components/kanban-board/kanban-board.component.ts`
- Create: `frontend/src/app/features/tasks/components/kanban-board/kanban-board.component.html`
- Create: `frontend/src/app/features/tasks/components/kanban-board/kanban-board.component.scss`
- Create: `frontend/src/app/features/tasks/components/task-card/task-card.component.ts`
- Create: `frontend/src/app/features/tasks/components/task-card/task-card.component.html`
- Create: `frontend/src/app/features/tasks/components/task-detail-modal/task-detail-modal.component.ts`
- Create: `frontend/src/app/features/tasks/components/review-gate-modal/review-gate-modal.component.ts`
- Modify: `frontend/src/app/features/organization/components/team-channel-detail/team-channel-detail.component.html` (Thêm Tabs: Chat Room | Kanban Board)

**Interfaces:**
- Produces:
  - Giao diện kéo thả Kanban 4 cột (`To Do`, `In Progress`, `In Review`, `Done`).
  - Phân biệt rõ Agent avatar (robot icon & badge) vs Human avatar.
  - Cổng Review Gate: Hiển thị Artifact và nút Approve / Request Revision kèm modal góp ý.

- [ ] **Step 1: Cài đặt `@angular/cdk` để hỗ trợ Drag & Drop**
- [ ] **Step 2: Xây dựng `TaskCardComponent` và `KanbanBoardComponent` với style Minimalist UI (`#F7F5F2`)**
- [ ] **Step 3: Xây dựng `ReviewGateModalComponent` cho phép Duyệt hoặc Yêu cầu làm lại kèm phản hồi**
- [ ] **Step 4: Tích hợp Tabs chuyển đổi giữa ChatRoom và KanbanBoard trong `TeamChannelDetailComponent`**
- [ ] **Step 5: Chạy `npm run build` để xác nhận UI biên dịch thành công**
- [ ] **Step 6: Commit**
  ```bash
  git add frontend/src/app/features/tasks/components/ frontend/src/app/features/organization/ frontend/package.json
  git commit -m "feat(frontend): implement Kanban Board UI, drag-and-drop, and Review Gate modal"
  ```

---

## Task 9: Kiểm Thử Toàn Diện & Verification Quy Tắc (.agents/rules/)

**Files:**
- Create: `frontend/e2e/kanban-realtime.spec.ts`
- Run checks: Unit Tests, Build, Lint, `/review` against rules.

- [ ] **Step 1: Viết kịch bản Playwright E2E `kanban-realtime.spec.ts` kiểm tra luồng tạo task, chuyển cột và Review Gate nghiệm thu**
- [ ] **Step 2: Chạy toàn bộ Unit Tests backend**
  Run: `dotnet test backend/AgentCoreSystem.slnx`
  Expected: Tất cả tests pass 100%.
- [ ] **Step 3: Thực hiện rà soát mã nguồn đối chiếu 11 bộ quy tắc (`.agents/rules/` và `anti-ai-slop.md`)**
- [ ] **Step 4: Commit**
  ```bash
  git add frontend/e2e/
  git commit -m "test: add Playwright E2E Kanban test suite and verify standards"
  ```

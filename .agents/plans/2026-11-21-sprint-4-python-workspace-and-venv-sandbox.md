# Sprint 4: Agent Python Workspace & Isolated Environment (.venv Sandbox) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Xây dựng hệ thống không gian làm việc vật lý độc lập (Workspace) và môi trường ảo Python (`.venv` Sandbox) cách ly an toàn cho từng Agent, mở rộng LLM Function Calling với System Built-in Python Tools, lưu trữ sản phẩm đầu ra (Artifacts) và cung cấp giao diện quản lý file & xem trước kết quả trực quan trên Angular 19.

**Architecture:** Mở rộng phân hệ `Module.Agents` (Clean Architecture, schema `agent`) với `WorkspaceManager` và `PythonVenvRunner`. Quản lý thư mục `Storage/Workspaces/{AgentId}/` độc lập, làm sạch 100% biến môi trường nhạy cảm, thực thi Out-of-Process qua CLI, xử lý timeout process tree và snapshot artifact detection. Tích hợp trực tiếp vào vòng lặp đàm thoại SignalR Chat và Hangfire Background Tasks.

**Tech Stack:** .NET 9, EF Core 9, SQL Server (schema `agent`), System.Diagnostics.Process CLI, Python 3.10+ venv, SignalR, Angular 19+ Standalone, Signals, TailwindCSS, xUnit, FluentAssertions, Playwright.

**Spec:** [.agents/specs/2026-11-21-sprint-4-python-workspace-and-venv-sandbox-design.md](file:///c:/Workplaces/PROJECT/AgentCoreSystem/.agents/specs/2026-11-21-sprint-4-python-workspace-and-venv-sandbox-design.md)

---

## Global Constraints

- **C# Conventions:** Block-scoped namespaces (`namespace AgentCoreSystem.Module.Agents... { ... }`), Primary Constructors cho DI, XML doc `/// <summary>` cho public/internal members, Result Pattern (`Result<T>`, `Error`), ProblemDetails RFC 7807 qua `ToProblemDetails()`.
- **Database & UUIDv7:** Mọi Entity kế thừa `BaseEntity<Guid>`, sinh khóa `Guid.CreateVersion7()`, Schema-per-module (`agent`), migration history riêng `__EFMigrationsHistory_Agents`.
- **Zero In-Process Python & Fail-Fast:** Cấm nhúng Python runtime trực tiếp vào bộ nhớ của .NET host (Python.NET). Bắt buộc chạy out-of-process qua CLI process.
- **Environment Variable Stripping:** Khi tạo process Python, xóa sạch biến môi trường mặc định, cấm truyền ConnectionStrings, JWT Secret hoặc API Keys vào Python subprocess.
- **Path Traversal Guard:** Mọi thao tác truy cập file trong workspace phải được chuẩn hóa canonical path và chặn đứng nếu trỏ ra ngoài `Storage/Workspaces/{AgentId}/`.
- **Angular Standards:** Standalone Components, `ChangeDetectionStrategy.OnPush`, Angular Signals (`signal`, `computed`), Control Flow mới (`@if`, `@for`), lazy load route qua `loadChildren` / `loadComponent`.

---

## Review Focus

1. **Path Traversal Bypass:** User hoặc LLM truyền đường dẫn kiểu `../../appsettings.json` hoặc `/etc/passwd` $\rightarrow$ `WorkspaceManager` phải chặn ngay lập tức và ném `Result.Failure(Error.Validation(...))`.
2. **Environment Secret Leakage:** Trong Python script chạy `import os; print(os.environ)` $\rightarrow$ Output stdout tuyệt đối không chứa connection strings, JWT secret hay LLM API keys.
3. **Infinite Script Hang / Timeout:** Python script chạy `while True: pass` hoặc sleep dài $\rightarrow$ `PythonVenvRunner` phải ngắt sau đúng `TimeoutSeconds` (mặc định 60s) bằng `Kill(entireProcessTree: true)` và trả cờ `IsTimeout = true`.
4. **Artifact Snapshot Accuracy:** Script tạo ra file `chart.png` trong `artifacts/` $\rightarrow$ Hệ thống phải nhận diện chính xác file mới này so với các file đã có từ trước và đóng gói thành `GeneratedArtifacts`.
5. **Chat Python Approval Flow:** Khi Agent yêu cầu chạy code ở chế độ `NeedApprove` $\rightarrow$ Chat Action Card hiển thị khối mã Python có syntax highlighting, cho phép duyệt chạy hoặc từ chối kèm lý do.

---

## Task Breakdown

### Task 1: Workspace Management Core (`IWorkspaceManager`, `WorkspaceManager`, Models)

**Files:**
- Create: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Application/Services/Workspace/IWorkspaceManager.cs`
- Create: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Application/Services/Workspace/WorkspaceModels.cs`
- Create: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Infrastructure/Services/Workspace/WorkspaceManager.cs`
- Modify: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Infrastructure/AgentsInfrastructureServiceExtensions.cs`
- Test: `backend/tests/AgentCoreSystem.UnitTests/Workspace/WorkspaceManagerTests.cs`

**Interfaces:**
- Produces: `IWorkspaceManager` với các phương thức quản lý workspace, canonical path validation, đọc danh sách file, stream file và tính dung lượng.

- [ ] **Step 1: Write failing tests for WorkspaceManager**
  - Test canonical path checking (chặn `../../`).
  - Test tạo đầy đủ 4 thư mục chuẩn (`.venv`, `scripts`, `artifacts`, `logs`).
  - Test liệt kê file và tính toán dung lượng.
- [ ] **Step 2: Run test to verify it fails**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter "FullyQualifiedName~WorkspaceManagerTests"`
  - Expected: FAIL (Types not found).
- [ ] **Step 3: Implement IWorkspaceManager & WorkspaceManager**
  - Đảm bảo kiểm tra path traversal nghiêm ngặt bằng `Path.GetFullPath`.
- [ ] **Step 4: Run test to verify it passes**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter "FullyQualifiedName~WorkspaceManagerTests"`
  - Expected: PASS.
- [ ] **Step 5: Commit**
  - `git commit -m "feat(agents): implement WorkspaceManager and file path traversal guard"`

---

### Task 2: Process Execution Engine & Sandbox Guard (`IPythonSandboxRunner`, `PythonVenvRunner`)

**Files:**
- Create: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Application/Services/Python/IPythonSandboxRunner.cs`
- Create: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Application/Services/Python/PythonExecutionModels.cs`
- Create: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Infrastructure/Services/Python/PythonVenvRunner.cs`
- Modify: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Infrastructure/AgentsInfrastructureServiceExtensions.cs`
- Test: `backend/tests/AgentCoreSystem.UnitTests/Python/PythonVenvRunnerTests.cs`

**Interfaces:**
- Consumes: `IWorkspaceManager`
- Produces: `IPythonSandboxRunner` (`EnsureVenvInitializedAsync`, `InstallPackagesAsync`, `ListInstalledPackagesAsync`, `ExecuteScriptAsync`)

- [ ] **Step 1: Write failing tests for PythonVenvRunner**
  - Test cơ chế lọc biến môi trường (Environment Variable Stripping).
  - Test snapshot artifact so sánh trước/sau khi chạy.
  - Test timeout detection và cancellation.
- [ ] **Step 2: Run test to verify it fails**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter "FullyQualifiedName~PythonVenvRunnerTests"`
  - Expected: FAIL.
- [ ] **Step 3: Implement IPythonSandboxRunner & PythonVenvRunner**
  - Triển khai Process start với `RedirectStandardOutput`, `RedirectStandardError`, xóa biến môi trường nhạy cảm, xử lý timeout với `Kill(entireProcessTree: true)`.
- [ ] **Step 4: Run test to verify it passes**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter "FullyQualifiedName~PythonVenvRunnerTests"`
  - Expected: PASS.
- [ ] **Step 5: Commit**
  - `git commit -m "feat(agents): implement PythonVenvRunner with environment stripping and timeout guard"`

---

### Task 3: Domain Entities, EF Core Schema & Migrations

**Files:**
- Create: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Domain/Entities/AgentWorkspaceConfig.cs`
- Create: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Domain/Entities/ScriptExecutionHistory.cs`
- Create: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Infrastructure/Persistence/Configurations/AgentWorkspaceConfigConfiguration.cs`
- Create: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Infrastructure/Persistence/Configurations/ScriptExecutionHistoryConfiguration.cs`
- Modify: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Infrastructure/Persistence/AgentsDbContext.cs`
- Create Migration: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Infrastructure/Persistence/Migrations/*_AddPythonWorkspaceAndScriptHistory.cs`
- Test: `backend/tests/AgentCoreSystem.UnitTests/Entities/AgentWorkspaceEntitiesTests.cs`

**Interfaces:**
- Produces: DbSets `AgentWorkspaceConfigs`, `ScriptExecutionHistories` trên schema `agent`.

- [ ] **Step 1: Write failing tests for AgentWorkspaceConfig and ScriptExecutionHistory**
  - Test UUIDv7 id generation, validation rules.
- [ ] **Step 2: Run test to verify it fails**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter "FullyQualifiedName~AgentWorkspaceEntitiesTests"`
  - Expected: FAIL.
- [ ] **Step 3: Implement Entities, Configurations, and EF Migration**
  - Khai báo Entities kế thừa `BaseEntity<Guid>`, config EF Core schema `agent`, sinh migration.
- [ ] **Step 4: Run test to verify it passes**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter "FullyQualifiedName~AgentWorkspaceEntitiesTests"`
  - Expected: PASS.
- [ ] **Step 5: Commit**
  - `git commit -m "feat(agents): add AgentWorkspaceConfig and ScriptExecutionHistory schema and migrations"`

---

### Task 4: CQRS Commands, Queries & Application Services

**Files:**
- Create: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Application/Workspace/Queries/GetAgentWorkspaceQuery.cs`
- Create: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Application/Workspace/Commands/InitializeVenvCommand.cs`
- Create: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Application/Workspace/Commands/InstallPackagesCommand.cs`
- Create: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Application/Workspace/Queries/ListWorkspaceFilesQuery.cs`
- Create: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Application/Workspace/Commands/ExecuteScriptCommand.cs`
- Create: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Application/Workspace/Queries/GetScriptHistoryQuery.cs`
- Test: `backend/tests/AgentCoreSystem.UnitTests/Workspace/WorkspaceCqrsTests.cs`

**Interfaces:**
- Consumes: `IWorkspaceManager`, `IPythonSandboxRunner`, `IAgentsDbContext`
- Produces: MediatR Request Handlers xử lý trọn gói use cases.

- [ ] **Step 1: Write failing tests for CQRS Handlers**
  - Test `GetAgentWorkspaceQueryHandler`.
  - Test `ExecuteScriptCommandHandler` lưu history và trả kết quả.
- [ ] **Step 2: Run test to verify it fails**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter "FullyQualifiedName~WorkspaceCqrsTests"`
  - Expected: FAIL.
- [ ] **Step 3: Implement CQRS Handlers**
  - Triển khai MediatR handlers theo chuẩn Result Pattern.
- [ ] **Step 4: Run test to verify it passes**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter "FullyQualifiedName~WorkspaceCqrsTests"`
  - Expected: PASS.
- [ ] **Step 5: Commit**
  - `git commit -m "feat(agents): implement workspace and script execution CQRS commands and queries"`

---

### Task 5: Web API Presentation & Host Integration

**Files:**
- Create: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Presentation/Controllers/AgentWorkspaceController.cs`
- Modify: `backend/src/Host/AgentCoreSystem.Api/Program.cs` (nếu cần cấu hình Storage root path trong `appsettings.json`)
- Test: `backend/tests/AgentCoreSystem.UnitTests/Controllers/AgentWorkspaceControllerTests.cs`

**Interfaces:**
- Produces: Web API Endpoints chuẩn OpenAPI:
  - `GET /api/v1/agents/{id}/workspace`
  - `POST /api/v1/agents/{id}/workspace/init-venv`
  - `GET /api/v1/agents/{id}/workspace/packages`
  - `POST /api/v1/agents/{id}/workspace/install-packages`
  - `GET /api/v1/agents/{id}/workspace/files`
  - `GET /api/v1/agents/{id}/workspace/files/download`
  - `POST /api/v1/agents/{id}/workspace/execute`
  - `GET /api/v1/agents/{id}/workspace/history`

- [ ] **Step 1: Write failing tests for AgentWorkspaceController**
  - Test endpoint mapping và ProblemDetails status codes.
- [ ] **Step 2: Run test to verify it fails**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter "FullyQualifiedName~AgentWorkspaceControllerTests"`
  - Expected: FAIL.
- [ ] **Step 3: Implement AgentWorkspaceController**
  - Sử dụng Primary Constructors, `ToProblemDetails()`, XML docs và ProducesResponseType.
- [ ] **Step 4: Run test to verify it passes**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter "FullyQualifiedName~AgentWorkspaceControllerTests"`
  - Expected: PASS.
- [ ] **Step 5: Commit**
  - `git commit -m "feat(agents): expose AgentWorkspace Web API controller"`

---

### Task 6: System Built-In Tools & LLM Function Calling Integration

**Files:**
- Create: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Application/Services/Tools/SystemBuiltInTools.cs`
- Modify: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Application/Services/MessageDispatcherService.cs`
- Modify: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Infrastructure/Jobs/TaskAgentExecutionJob.cs`
- Test: `backend/tests/AgentCoreSystem.UnitTests/Tools/SystemBuiltInToolsTests.cs`

**Interfaces:**
- Produces: Định nghĩa công cụ `execute_python_code`, `install_python_packages`, `list_workspace_files`.
- Tích hợp vòng lặp Tool Calling trong Chat và Hangfire Task.

- [ ] **Step 1: Write failing tests for SystemBuiltInTools resolution**
  - Test nhận diện tool `execute_python_code` và thực thi qua `IPythonSandboxRunner`.
- [ ] **Step 2: Run test to verify it fails**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter "FullyQualifiedName~SystemBuiltInToolsTests"`
  - Expected: FAIL.
- [ ] **Step 3: Implement SystemBuiltInTools and wiring in Chat/Tasks**
  - Wireup thực thi code khi tool call được xác nhận trong Chat hoặc kích hoạt từ Hangfire.
- [ ] **Step 4: Run test to verify it passes**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter "FullyQualifiedName~SystemBuiltInToolsTests"`
  - Expected: PASS.
- [ ] **Step 5: Commit**
  - `git commit -m "feat(chat): integrate execute_python_code into LLM function calling loop"`

---

### Task 7: Angular Frontend - Workspace & Artifacts Management UI

**Files:**
- Create: `frontend/src/app/features/agents/services/agent-workspace.service.ts`
- Create: `frontend/src/app/features/agents/components/agent-workspace/agent-workspace.component.ts`
- Create: `frontend/src/app/features/agents/components/agent-workspace/agent-workspace.component.html`
- Create: `frontend/src/app/features/agents/components/artifact-preview-modal/artifact-preview-modal.component.ts`
- Create: `frontend/src/app/features/agents/components/artifact-preview-modal/artifact-preview-modal.component.html`
- Modify: `frontend/src/app/features/agents/components/agent-detail/agent-detail.component.html` (thêm tab Workspace)
- Test: `frontend/src/app/features/agents/components/agent-workspace/agent-workspace.component.spec.ts`

**Interfaces:**
- Produces: UI quản lý Workspace, File Explorer, Package Manager và Modal preview ảnh/dữ liệu/code.

- [ ] **Step 1: Write failing unit test for AgentWorkspaceComponent**
  - Test render thông tin venv và danh sách files.
- [ ] **Step 2: Run test to verify it fails**
  - Run: `cd frontend; npm run test -- --include src/app/features/agents/components/agent-workspace/agent-workspace.component.spec.ts`
  - Expected: FAIL.
- [ ] **Step 3: Implement Workspace Service, Components and Modals**
  - Viết component dạng Standalone, OnPush, dùng Signals.
- [ ] **Step 4: Run test to verify it passes**
  - Run: `cd frontend; npm run test -- --include src/app/features/agents/components/agent-workspace/agent-workspace.component.spec.ts`
  - Expected: PASS.
- [ ] **Step 5: Commit**
  - `git commit -m "feat(ui): add Agent Workspace explorer, package manager, and artifact preview modal"`

---

### Task 8: Angular Frontend - Enhanced Chat Action Card for Python

**Files:**
- Modify: `frontend/src/app/features/chat/components/chat-tool-approval-card/chat-tool-approval-card.component.ts`
- Modify: `frontend/src/app/features/chat/components/chat-tool-approval-card/chat-tool-approval-card.component.html`
- Modify: `frontend/src/app/features/chat/components/chat-message-item/chat-message-item.component.html` (hiển thị artifacts đính kèm)
- Test: `frontend/src/app/features/chat/components/chat-tool-approval-card/chat-tool-approval-card.component.spec.ts`

**Interfaces:**
- Produces: Action Card hiển thị khối mã Python đẹp mắt và pill artifacts có thể bấm mở preview.

- [ ] **Step 1: Write failing test for enhanced ChatToolApprovalCard**
  - Test nhận diện tool `execute_python_code` và hiển thị syntax code block.
- [ ] **Step 2: Run test to verify it fails**
  - Run: `cd frontend; npm run test -- --include src/app/features/chat/components/chat-tool-approval-card/chat-tool-approval-card.component.spec.ts`
  - Expected: FAIL.
- [ ] **Step 3: Implement Python code highlight & artifact attachment pills**
  - Tối ưu UI Action Card theo chuẩn Hallmark & Minimalist UI.
- [ ] **Step 4: Run test to verify it passes**
  - Run: `cd frontend; npm run test -- --include src/app/features/chat/components/chat-tool-approval-card/chat-tool-approval-card.component.spec.ts`
  - Expected: PASS.
- [ ] **Step 5: Commit**
  - `git commit -m "feat(ui): enhance Chat Action Card with Python code block and artifact preview pills"`

---

### Task 9: End-to-End Testing (Playwright)

**Files:**
- Create: `frontend/e2e/specs/sprint-4-python-workspace.spec.ts`

- [ ] **Step 1: Write comprehensive Playwright E2E spec**
  - Test mở Workspace tab, kiểm tra trạng thái venv.
  - Test gửi tin nhắn chat kích hoạt tool `execute_python_code`.
  - Test Action Card duyệt chạy script, xác nhận kết quả stdout và mở modal preview artifact.
- [ ] **Step 2: Run E2E test**
  - Run: `cd frontend; npx playwright test e2e/specs/sprint-4-python-workspace.spec.ts --project=chromium`
  - Expected: PASS.
- [ ] **Step 3: Commit**
  - `git commit -m "test(e2e): add Playwright test suite for Sprint 4 Python workspace and artifacts"`
# Sprint 3: No-Code Tool Builder & LLM Function Calling Engine Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Xây dựng hệ sinh thái No-Code REST API Tool Builder, mở rộng LLM Engine hỗ trợ Function Calling và cơ chế phê duyệt tương tác đa chế độ (Auto / Semi-Auto / Need Approve) cho AgentCoreSystem.

**Architecture:** Triển khai phân hệ mới `Module.Agents` (Clean Architecture, schema `agent`) quản lý định nghĩa và phân quyền Tools. Tầng dùng chung `Shared.Infrastructure.Http` cung cấp `IRestApiExecutor` không trạng thái kèm bộ lọc SSRF Protection. Tầng `Shared.Infrastructure.LLM` mở rộng giao thức OpenAI Function Calling. Tầng `Chat` và `Tasks` tích hợp vòng lặp Tool Calling với bảng `chat.PendingToolCalls` và SignalR Action Card.

**Tech Stack:** .NET 9, EF Core 9, SQL Server, SignalR, Hangfire, Angular 19+ Standalone, Angular Signals, RxJS, TailwindCSS, Vitest, xUnit, Playwright.

**Spec:** [.agents/specs/2026-11-21-sprint-3-no-code-tool-builder-and-function-calling-design.md](file:///c:/Workplaces/PROJECT/AgentCoreSystem/.agents/specs/2026-11-21-sprint-3-no-code-tool-builder-and-function-calling-design.md)

---

## Global Constraints

- **C# Conventions:** Block-scoped namespaces (`namespace AgentCoreSystem.Module.Agents... { ... }`), Primary Constructors cho DI, XML doc `/// <summary>` cho public/internal members, Result Pattern (`Result<T>`, `Error`), ProblemDetails RFC 7807 qua `ToProblemDetails()`.
- **Database & UUIDv7:** Mọi Entity kế thừa `BaseEntity<Guid>`, sinh khóa `Guid.CreateVersion7()`, Schema-per-module (`agent`), migration history riêng `__EFMigrationsHistory_Agents`.
- **Zero Hardcoded Secrets & Fail-Fast:** Cấm lưu hardcode URL, API Keys hoặc tokens; mã hóa token trong `HeadersConfig` và mask `••••••••` trên API GET.
- **SSRF Protection:** Chặn 100% request đến `127.0.0.1`, `localhost`, `::1`, link-local `169.254.169.254` và private CIDRs trừ khi được cấu hình whitelist rõ ràng.
- **Angular Standards:** Standalone Components, `ChangeDetectionStrategy.OnPush`, Angular Signals (`signal`, `computed`), Control Flow mới (`@if`, `@for`), lazy load route qua `loadChildren` / `loadComponent`.

---

## Review Focus

1. **SSRF Guard Bypass:** Endpoint URL trỏ tới `http://127.0.0.1:5000` hoặc `http://169.254.169.254/latest/meta-data/` $\rightarrow$ Executor phải chặn ngay lập tức và ném lỗi validation rõ ràng, không gửi request ra ngoài.
2. **Infinite Tool Calling Loop:** LLM liên tục gọi tool không dừng $\rightarrow$ Hệ thống phải ngắt sau tối đa 5 bước lặp (`MaxToolIterations = 5`) và trả thông báo lỗi thay vì treo luồng.
3. **Secret Header Leakage:** Gọi `GET /api/v1/agents/tools/{id}` $\rightarrow$ Giá trị header `Authorization` hoặc `X-Api-Key` phải được che thành `••••••••`, không bao giờ lộ plaintext ra client.
4. **Chat Tool Rejection Handling:** Người dùng bấm `[Từ chối]` trên Action Card $\rightarrow$ Hệ thống không được crash mà phải nạp thông điệp từ chối vào LLM context để Agent đàm thoại tiếp tục khéo léo.
5. **Unauthorized Approval:** User không thuộc kênh chat bấm confirm tool call $\rightarrow$ Hệ thống trả lỗi `403 Forbidden` chuẩn ProblemDetails.

---

## Task Breakdown

### Task 1: Stateless REST API Executor & SSRF Guard (`Shared.Infrastructure.Http`)

**Files:**
- Create: `backend/src/Shared/AgentCoreSystem.Shared.Infrastructure/Http/IRestApiExecutor.cs`
- Create: `backend/src/Shared/AgentCoreSystem.Shared.Infrastructure/Http/RestApiExecutor.cs`
- Create: `backend/src/Shared/AgentCoreSystem.Shared.Infrastructure/Http/ToolExecutionModels.cs`
- Modify: `backend/src/Shared/AgentCoreSystem.Shared.Infrastructure/InfrastructureServiceExtensions.cs`
- Test: `backend/tests/AgentCoreSystem.UnitTests/Http/RestApiExecutorTests.cs`

**Interfaces:**
- Produces: `IRestApiExecutor.ExecuteAsync(RestApiExecutionRequest request, CancellationToken ct) -> Task<RestApiExecutionResult>`

- [ ] **Step 1: Write failing tests for RestApiExecutor**
  - Test valid GET request with path and query parameters substitution.
  - Test POST request with JSON body.
  - Test SSRF protection blocking `127.0.0.1`, `localhost`, and `169.254.169.254`.
  - Test timeout handling.

- [ ] **Step 2: Run tests to verify they fail**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter FullyQualifiedName~RestApiExecutorTests`
  - Expected: FAIL with missing types/implementation.

- [ ] **Step 3: Implement `RestApiExecutionModels` and `RestApiExecutor`**
  - Implement URL template interpolation (e.g. `/api/v1/users/{id}`).
  - Implement SSRF IP validation using `Dns.GetHostAddressesAsync` with private range checks.
  - Implement request dispatching with custom headers, query params, and JSON body.
  - Register `IRestApiExecutor` into DI in `InfrastructureServiceExtensions.cs`.

- [ ] **Step 4: Run tests to verify they pass**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter FullyQualifiedName~RestApiExecutorTests`
  - Expected: PASS

- [ ] **Step 5: Commit**
  - `git add backend/src/Shared/AgentCoreSystem.Shared.Infrastructure backend/tests/AgentCoreSystem.UnitTests`
  - `git commit -m "feat(shared): add RestApiExecutor with SSRF protection"`

---

### Task 2: LLM Tool Calling Spec Extension (`Shared.Infrastructure.LLM`)

**Files:**
- Create: `backend/src/Shared/AgentCoreSystem.Shared.Infrastructure/LLM/ToolCallingModels.cs`
- Modify: `backend/src/Shared/AgentCoreSystem.Shared.Infrastructure/LLM/ILLMClient.cs`
- Modify: `backend/src/Shared/AgentCoreSystem.Shared.Infrastructure/LLM/OpenAiCompatibleClient.cs`
- Test: `backend/tests/AgentCoreSystem.UnitTests/LLM/LlmToolCallingTests.cs`

**Interfaces:**
- Consumes: OpenAI chat completion payload schema.
- Produces: `ILLMClient.ChatWithToolsAsync(string model, IEnumerable<ChatMessagePayload> messages, IEnumerable<LlmToolDefinitionPayload>? tools, decimal? temperature, CancellationToken ct) -> Task<LlmChatResponse>`

- [ ] **Step 1: Write failing tests for LLM Tool Calling**
  - Test serializing `tools` payload to OpenAI compatible schema `[{"type": "function", "function": {...}}]`.
  - Test deserializing response with `tool_calls` array (id, name, arguments).
  - Test standard text response when no tools are called.

- [ ] **Step 2: Run tests to verify they fail**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter FullyQualifiedName~LlmToolCallingTests`
  - Expected: FAIL

- [ ] **Step 3: Implement tool calling models and extend `OpenAiCompatibleClient`**
  - Implement `LlmToolDefinitionPayload`, `LlmToolCallPayload`, `LlmChatResponse`.
  - Add `ChatWithToolsAsync` to `ILLMClient` and `OpenAiCompatibleClient`.

- [ ] **Step 4: Run tests to verify they pass**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter FullyQualifiedName~LlmToolCallingTests`
  - Expected: PASS

- [ ] **Step 5: Commit**
  - `git add backend/src/Shared/AgentCoreSystem.Shared.Infrastructure/LLM backend/tests/AgentCoreSystem.UnitTests/LLM`
  - `git commit -m "feat(llm): extend ILLMClient to support OpenAI tool calling specification"`

---

### Task 3: Module Agents Scaffolding & Persistence (`Domain`, `Infrastructure`)

**Files:**
- Create: 4 Projects under `backend/src/Modules/Agents/`:
  - `AgentCoreSystem.Module.Agents.Domain`
  - `AgentCoreSystem.Module.Agents.Application`
  - `AgentCoreSystem.Module.Agents.Infrastructure`
  - `AgentCoreSystem.Module.Agents.Presentation`
- Modify: `backend/AgentCoreSystem.slnx`
- Entities in Domain:
  - `ToolDefinition.cs`
  - `AgentToolAccess.cs`
  - `ToolExecutionLog.cs`
  - `ToolType.cs` enum
- Modify `AgentProfile.cs` in `Module.Organization.Domain`:
  - Add `ToolExecutionMode` enum (`Auto = 0`, `SemiAuto = 1`, `NeedApprove = 2`)
- Modify `ChatDbContext` in `Module.Chat`:
  - Add `PendingToolCall.cs` entity
- Persistence in `Module.Agents.Infrastructure`:
  - `AgentsDbContext.cs` (schema: `agent`)
  - Entity type configurations
- Migrations:
  - Add EF Migration `Initial_Agents_Schema` for `AgentsDbContext`
  - Add EF Migration for `chat.PendingToolCalls` and `org.AgentProfiles.ToolExecutionMode`
- Test: `backend/tests/AgentCoreSystem.UnitTests/Agents/AgentsDomainTests.cs`

- [ ] **Step 1: Write failing domain unit tests**
  - Test `ToolDefinition.Create` validation (Name regex, non-empty endpoint, valid HTTP method).
  - Test `ToolExecutionMode` defaults to `SemiAuto`.

- [ ] **Step 2: Run tests to verify they fail**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter FullyQualifiedName~AgentsDomainTests`
  - Expected: FAIL

- [ ] **Step 3: Scaffold projects, entities, DbContext and Migrations**
  - Scaffold 4 projects and link references according to Clean Architecture rules.
  - Implement entities, configurations, DbContext with schema `agent`.
  - Add `PendingToolCall` to `ChatDbContext`.
  - Run EF migrations via `dotnet ef`.

- [ ] **Step 4: Run tests to verify they pass and build succeeds**
  - Run: `dotnet build backend/AgentCoreSystem.slnx`
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter FullyQualifiedName~AgentsDomainTests`
  - Expected: PASS

- [ ] **Step 5: Commit**
  - `git add backend/src/Modules/Agents backend/src/Modules/Organization backend/src/Modules/Chat backend/AgentCoreSystem.slnx`
  - `git commit -m "feat(agents): scaffold Module.Agents Clean Architecture and EF schema"`

---

### Task 4: Tool Management CQRS Commands & Queries (`Module.Agents.Application`)

**Files:**
- DTOs: `ToolDefinitionDto.cs`, `ToolExecutionLogDto.cs`, `AgentToolSummaryDto.cs`
- Commands:
  - `CreateToolDefinitionCommand.cs` & Handler
  - `UpdateToolDefinitionCommand.cs` & Handler
  - `DeleteToolDefinitionCommand.cs` & Handler
  - `TestToolDefinitionCommand.cs` & Handler
  - `AssignAgentToolsCommand.cs` & Handler
- Queries:
  - `GetToolDefinitionsQuery.cs` & Handler
  - `GetToolDefinitionByIdQuery.cs` & Handler
  - `GetAgentToolsQuery.cs` & Handler
- Service / Helper:
  - `SecretMaskingHelper.cs` (masking sensitive header values with `••••••••`)
- Test: `backend/tests/AgentCoreSystem.UnitTests/Agents/ToolDefinitionHandlerTests.cs`

- [ ] **Step 1: Write failing tests for CQRS Handlers**
  - Test creating tool definition with valid JSON Schema.
  - Test updating tool definition preserving existing secret headers when masked value is passed.
  - Test executing tool test command via `IRestApiExecutor`.
  - Test assigning tools to an agent.

- [ ] **Step 2: Run tests to verify they fail**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter FullyQualifiedName~ToolDefinitionHandlerTests`
  - Expected: FAIL

- [ ] **Step 3: Implement CQRS Commands, Handlers, Validators and Queries**
  - Implement validation (FluentValidation) for JSON Schema and Endpoint URLs.
  - Implement `SecretMaskingHelper`.
  - Wire up MediatR handlers with Result Pattern.

- [ ] **Step 4: Run tests to verify they pass**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter FullyQualifiedName~ToolDefinitionHandlerTests`
  - Expected: PASS

- [ ] **Step 5: Commit**
  - `git add backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Application backend/tests/AgentCoreSystem.UnitTests/Agents`
  - `git commit -m "feat(agents): implement tool management CQRS commands, queries and secret masking"`

---

### Task 5: Agents Web API Controllers & Host Registration (`Module.Agents.Presentation`)

**Files:**
- Create: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Presentation/Controllers/v1/ToolsController.cs`
- Create: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Presentation/Controllers/v1/AgentToolsController.cs`
- Create: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Presentation/DependencyInjection.cs`
- Modify: `backend/src/Host/AgentCoreSystem.Api/Program.cs`
- Test: `backend/tests/AgentCoreSystem.UnitTests/Agents/ToolsControllerTests.cs`

- [ ] **Step 1: Write failing controller unit tests**
  - Test `GET /api/v1/agents/tools` returns 200 OK with masked headers.
  - Test `POST /api/v1/agents/tools` creates tool and returns 201 Created.
  - Test `POST /api/v1/agents/tools/{id}/test` returns execution results.

- [ ] **Step 2: Run tests to verify they fail**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter FullyQualifiedName~ToolsControllerTests`
  - Expected: FAIL

- [ ] **Step 3: Implement Controllers with OAS v3 Documentation and register in Program.cs**
  - Add XML docs `<summary>`, `<remarks>`, `[ProducesResponseType]`.
  - Use `ToProblemDetails()` for error mapping.
  - Register `builder.Services.AddAgentsModule(builder.Configuration)` in `Program.cs`.

- [ ] **Step 4: Run tests to verify they pass**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter FullyQualifiedName~ToolsControllerTests`
  - Run: `dotnet build backend/AgentCoreSystem.slnx`
  - Expected: PASS

- [ ] **Step 5: Commit**
  - `git add backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Presentation backend/src/Host/AgentCoreSystem.Api`
  - `git commit -m "feat(agents): add Web API controllers for tools and wire into Api host"`

---

### Task 6: Interactive Tool Approval & Dispatcher Integration in Chat (`Module.Chat`)

**Files:**
- Create: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Application/Tools/Commands/ConfirmToolCallCommand.cs` & Handler
- Create: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Application/Tools/Commands/RejectToolCallCommand.cs` & Handler
- Modify: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Infrastructure/Hubs/SignalRChatHub.cs`
- Modify: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Infrastructure/Services/MessageDispatcherService.cs`
- Modify: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Presentation/Controllers/v1/ChatController.cs`
- Test: `backend/tests/AgentCoreSystem.UnitTests/Chat/ToolApprovalDispatcherTests.cs`

- [ ] **Step 1: Write failing tests for Tool Approval Dispatcher**
  - Test Auto mode executes tool immediately without pausing.
  - Test SemiAuto mode pauses on `RequiresConfirmation == true`, creates `PendingToolCall` and triggers SignalR event.
  - Test confirming tool call resumes LLM conversation with tool output.
  - Test rejecting tool call sends user rejection message to LLM.

- [ ] **Step 2: Run tests to verify they fail**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter FullyQualifiedName~ToolApprovalDispatcherTests`
  - Expected: FAIL

- [ ] **Step 3: Implement Tool Call Loop and Approval in `MessageDispatcherService`**
  - Query agent assigned tools via MediatR (`GetAgentToolsQuery`).
  - Convert tools to OpenAI format and pass to `ILLMClient.ChatWithToolsAsync`.
  - If `tool_calls` detected:
    - Check Agent `ToolExecutionMode` and Tool `RequiresConfirmation`.
    - If confirmation needed: Insert `PendingToolCall`, broadcast `ToolApprovalRequested` to SignalR group.
    - If auto: Execute via `IRestApiExecutor`, log to `ToolExecutionLog`, send output back to LLM (up to `MaxToolIterations = 5`).
  - Implement endpoints `/api/v1/chat/tool-calls/{callId}/confirm` and `/reject`.

- [ ] **Step 4: Run tests to verify they pass**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter FullyQualifiedName~ToolApprovalDispatcherTests`
  - Expected: PASS

- [ ] **Step 5: Commit**
  - `git add backend/src/Modules/Chat backend/tests/AgentCoreSystem.UnitTests/Chat`
  - `git commit -m "feat(chat): implement interactive tool call loop and approval flow"`

---

### Task 7: Background Tasks Tool Calling Integration (`Module.Tasks`)

**Files:**
- Modify: `backend/src/Modules/Tasks/AgentCoreSystem.Module.Tasks.Infrastructure/Jobs/TaskAgentExecutionJob.cs`
- Test: `backend/tests/AgentCoreSystem.UnitTests/Tasks/TaskAgentExecutionToolCallingTests.cs`

- [ ] **Step 1: Write failing tests for TaskAgentExecutionJob with tools**
  - Test job executes safe tools automatically during background execution.
  - Test job detects tool requiring confirmation, logs warning in `TaskActivityLog`, transitions Task to `InReview` with explanation.

- [ ] **Step 2: Run tests to verify they fail**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter FullyQualifiedName~TaskAgentExecutionToolCallingTests`
  - Expected: FAIL

- [ ] **Step 3: Integrate tool execution loop into `TaskAgentExecutionJob`**
  - Load tools for the assigned Agent.
  - Execute tool calls matching safe criteria.
  - Handle approval requirement gracefully in non-interactive environment.

- [ ] **Step 4: Run tests to verify they pass**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter FullyQualifiedName~TaskAgentExecutionToolCallingTests`
  - Expected: PASS

- [ ] **Step 5: Commit**
  - `git add backend/src/Modules/Tasks backend/tests/AgentCoreSystem.UnitTests/Tasks`
  - `git commit -m "feat(tasks): integrate safe tool execution in Hangfire background tasks"`

---

### Task 8: Frontend Tools Management UI (`features/agents/tools`)

**Files:**
- Create: `frontend/src/app/features/agents/tools/models/tool.models.ts`
- Create: `frontend/src/app/features/agents/tools/services/tools.service.ts`
- Create: `frontend/src/app/features/agents/tools/components/tool-list/tool-list.component.ts`
- Create: `frontend/src/app/features/agents/tools/components/tool-editor/tool-editor.component.ts`
- Create: `frontend/src/app/features/agents/tools/components/tool-test-dialog/tool-test-dialog.component.ts`
- Create: `frontend/src/app/features/agents/tools/tools.routes.ts`
- Modify: `frontend/src/app/core/layout/main-layout.component.ts` (Thêm mục "Công cụ AI" vào Sidebar)
- Modify: `frontend/src/app/app.routes.ts`
- Test: `frontend/src/app/features/agents/tools/services/tools.service.spec.ts`

- [ ] **Step 1: Write unit tests for ToolsService**
  - Test fetching tools list, creating, updating, deleting tools, and running test execution.

- [ ] **Step 2: Run test to verify failure**
  - Run: `cd frontend; npm test -- --run src/app/features/agents/tools/services/tools.service.spec.ts`
  - Expected: FAIL

- [ ] **Step 3: Implement Tools Angular Feature**
  - Implement `ToolsService` with Signals.
  - Implement `ToolListComponent` with method pills (GET in blue, POST in green, DELETE in red), confirmation badges, and search.
  - Implement `ToolEditorComponent` with JSON schema syntax check, dynamic headers table, and confirmation toggle.
  - Implement `ToolTestDialogComponent` with parameter inputs and JSON response viewer.
  - Add `/tools` route and Sidebar link with icon.

- [ ] **Step 4: Run test to verify passing**
  - Run: `cd frontend; npm test -- --run src/app/features/agents/tools/services/tools.service.spec.ts`
  - Run: `cd frontend; npm run build`
  - Expected: PASS

- [ ] **Step 5: Commit**
  - `git add frontend/src/app/features/agents frontend/src/app/core/layout frontend/src/app/app.routes.ts`
  - `git commit -m "feat(ui): add AI Tools management, editor and test dialog"`

---

### Task 9: Frontend Agent Profile Tools & Execution Mode Configuration

**Files:**
- Modify: `frontend/src/app/features/organization/components/agent-detail/agent-detail.component.ts`
- Modify: `frontend/src/app/features/organization/services/organization.service.ts`
- Test: `frontend/src/app/features/organization/components/agent-detail/agent-detail.component.spec.ts`

- [ ] **Step 1: Write test for Agent Profile Tool Configuration**
  - Test rendering `ToolExecutionMode` selector (`Auto`, `SemiAuto`, `NeedApprove`).
  - Test toggling tool assignment checkboxes and saving.

- [ ] **Step 2: Run test to verify failure**
  - Run: `cd frontend; npm test -- --run src/app/features/organization/components/agent-detail/agent-detail.component.spec.ts`
  - Expected: FAIL

- [ ] **Step 3: Implement Tool Configuration UI in `agent-detail`**
  - Add section/tab "Cấu hình Công cụ (Tools)" in `agent-detail.component.ts`.
  - Radio options with descriptive hints for `ToolExecutionMode`.
  - Grid list of available tools with toggle checkboxes.
  - Wire save logic to `PUT /api/v1/agents/profiles/{agentId}/tools`.

- [ ] **Step 4: Run test to verify passing**
  - Run: `cd frontend; npm test -- --run src/app/features/organization/components/agent-detail/agent-detail.component.spec.ts`
  - Expected: PASS

- [ ] **Step 5: Commit**
  - `git add frontend/src/app/features/organization`
  - `git commit -m "feat(ui): add tool execution mode and tool assignment to agent detail"`

---

### Task 10: Frontend Realtime Chat Interactive Tool Approval Card

**Files:**
- Create: `frontend/src/app/features/chat/components/tool-approval-card/tool-approval-card.component.ts`
- Create: `frontend/src/app/features/chat/components/tool-execution-badge/tool-execution-badge.component.ts`
- Modify: `frontend/src/app/features/chat/components/chat-room/chat-room.component.ts`
- Modify: `frontend/src/app/features/chat/services/chat.service.ts`
- Test: `frontend/src/app/features/chat/components/tool-approval-card/tool-approval-card.component.spec.ts`

- [ ] **Step 1: Write unit tests for ToolApprovalCard**
  - Test displaying Tool Name, endpoint, parameters preview.
  - Test emitting confirm event on clicking [Phê duyệt].
  - Test emitting reject event on clicking [Từ chối].

- [ ] **Step 2: Run test to verify failure**
  - Run: `cd frontend; npm test -- --run src/app/features/chat/components/tool-approval-card/tool-approval-card.component.spec.ts`
  - Expected: FAIL

- [ ] **Step 3: Implement ToolApprovalCard and Chat Integration**
  - Listen for SignalR `ToolApprovalRequested` event in `ChatService`.
  - Render `ToolApprovalCardComponent` inside the chat messages stream.
  - On confirm/reject: call backend APIs `/api/v1/chat/tool-calls/{callId}/confirm` or `/reject`.
  - Show `ToolExecutionBadgeComponent` when tool is running or completed.

- [ ] **Step 4: Run test to verify passing**
  - Run: `cd frontend; npm test -- --run src/app/features/chat/components/tool-approval-card/tool-approval-card.component.spec.ts`
  - Run: `cd frontend; npm run build`
  - Expected: PASS

- [ ] **Step 5: Commit**
  - `git add frontend/src/app/features/chat`
  - `git commit -m "feat(ui): add interactive tool approval card and status badges to chat"`

---

### Task 11: End-to-End Verification & Code Review

**Files:**
- Create: `frontend/e2e/specs/sprint-3-tools-realtime.spec.ts`
- Modify: `justfile` (Thêm lệnh `test-e2e-tools`)

- [ ] **Step 1: Implement Playwright E2E Test**
  - Scenario 1: Admin creates No-Code Tool (GET Weather API) and tests it via Test Dialog.
  - Scenario 2: Admin configures Agent with `SemiAuto` mode and assigns the Tool.
  - Scenario 3: User sends message `@Agent kiểm tra thời tiết Hà Nội` $\rightarrow$ verifies tool is called automatically and response rendered.
  - Scenario 4: Admin creates POST Tool (RequiresConfirmation = true) $\rightarrow$ verifies Action Card displays and [Phê duyệt] triggers execution.

- [ ] **Step 2: Run all backend and frontend unit tests**
  - Run: `just test`
  - Expected: All unit tests PASS without errors.

- [ ] **Step 3: Run Code Review against project rules**
  - Run `/review` or verify compliance with `anti-ai-slop.md`, `csharp-conventions.md`, `modular-monolith-rules.md`, `angular-standards.md`.

- [ ] **Step 4: Commit**
  - `git add frontend/e2e justfile`
  - `git commit -m "test(e2e): add end-to-end test spec for Sprint 3 tools and approval flow"`

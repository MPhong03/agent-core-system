# Sprint 1: Kênh Chat Thời Gian Thực & Bộ Lọc Kích Hoạt (@Mention Router) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Xây dựng hoàn chỉnh Phân hệ `Module.Chat` (schema `chat`), hạ tầng LLM Generic độc lập (OpenAI-compatible protocol, zero hardcoded URLs/provider names), SignalR Realtime Hub cho phép Token Streaming tức thời, `@Mention Router` xử lý hàng đợi tuần tự cho Agent kèm bộ nhớ Sliding Window 20 tin nhắn, và giao diện Angular Chat Room với Autocomplete popup khi gõ `@`.

**Architecture:** Clean Architecture 4 tầng cho `Module.Chat` tích hợp vào Modular Monolith Host qua MediatR, SignalR Hub (`/hubs/chat`), EF Core Schema-per-module (`chat`). Hạ tầng Generic LLM Client đặt tại `Shared.Infrastructure/LLM` nạp cấu hình qua `IOptions<LlmOptions>`. Frontend Angular 19+ Standalone quản lý state bằng Signals và OnPush.

**Tech Stack:** .NET 9, EF Core (SQL Server schema `chat`, UUIDv7), SignalR, MediatR, FluentValidation, OpenAPI v3, Angular 19+ Standalone, Signals, SCSS, Microsoft.AspNetCore.SignalR.Client.

**Spec:** [docs/superpowers/specs/2026-11-19-sprint-1-realtime-chat-and-mention-router-design.md](file:///c:/Workplaces/PROJECT/AgentCoreSystem/docs/superpowers/specs/2026-11-19-sprint-1-realtime-chat-and-mention-router-design.md)

---

## Global Constraints
- Bắt buộc sử dụng Block-scoped namespaces (`namespace AgentCoreSystem.Module.Chat { ... }`) cho toàn bộ file C#.
- Sử dụng C# Primary Constructors cho việc khai báo Dependency Injection.
- Mọi Entity kế thừa `BaseEntity` (mặc định UUIDv7 từ `Guid.CreateVersion7()`).
- Toàn bộ bảng dữ liệu nằm trên schema SQL Server `chat`, bảng lịch sử migration là `__EFMigrationsHistory_Chat`.
- **CẤM HARDCODE**: Tuyệt đối không hardcode URL `http://localhost:20128/v1` hoặc tên `9router` trong code; nạp hoàn toàn qua `LlmOptions` từ cấu hình với cơ chế Fail-Fast.
- Sử dụng `Result<T>` và `Error` cho Use Cases, Controllers trả về RFC 7807 ProblemDetails khi lỗi và `ApiResponse<T>` khi thành công.
- Giữ nguyên header bản quyền MIT của tác giả Phong Minh Đặng trên các file tạo mới.

---

## Review Focus
1. **Lặp vô tận giữa các Agent:** Hai Agent tag chéo tên nhau -> `CallDepth` tăng dần và bị chặn dứt khoát khi `CallDepth >= 3` (không tiếp tục gửi request tới LLM).
2. **Cấu hình LLM thiếu hoặc BaseUrl sai:** Khởi động ứng dụng hoặc gửi tin nhắn khi chưa cấu hình `Llm:Providers` hợp lệ -> Kích hoạt Fail-Fast hoặc trả về `Result.Failure(Error.Unexpected(...))` thay vì làm sập ứng dụng.
3. **Mất kết nối SignalR khi đang stream:** Client mất mạng giữa chừng -> Server hoàn tất stream vào DB an toàn; client reconnect tự động load lại lịch sử đầy đủ từ REST API.
4. **Tag nhiều Agent cùng lúc (`@AgentA @AgentB`):** Xử lý tuần tự (Sequential Queue) -> Agent B bắt đầu sau khi Agent A hoàn tất và Sliding Window của Agent B thấy được câu trả lời của Agent A.
5. **Giả mạo danh tính:** Client gọi Hub method gửi tin nhắn -> Server tự trích xuất `UserId` từ JWT Claims để truy vấn `MemberId` hợp lệ từ `Module.Organization`, từ chối nếu user không thuộc Channel.

---

## Task 1: Hạ Tầng LLM Generic Độc Lập (Shared Infrastructure)

**Files:**
- Create: `backend/src/Shared/AgentCoreSystem.Shared.Infrastructure/LLM/LlmOptions.cs`
- Create: `backend/src/Shared/AgentCoreSystem.Shared.Infrastructure/LLM/ILLMClient.cs`
- Create: `backend/src/Shared/AgentCoreSystem.Shared.Infrastructure/LLM/ILLMProviderFactory.cs`
- Create: `backend/src/Shared/AgentCoreSystem.Shared.Infrastructure/LLM/OpenAiCompatibleClient.cs`
- Create: `backend/src/Shared/AgentCoreSystem.Shared.Infrastructure/LLM/LlmProviderFactory.cs`
- Modify: `backend/src/Shared/AgentCoreSystem.Shared.Infrastructure/DependencyInjection.cs`
- Create: `backend/tests/AgentCoreSystem.UnitTests/LLM/LlmClientTests.cs`

**Interfaces:**
- Produces:
  - `class LlmOptions { string DefaultProvider, Dictionary<string, LlmProviderConfig> Providers }`
  - `class LlmProviderConfig { string ProviderType, string BaseUrl, string? ApiKey, string DefaultModel, int TimeoutSeconds }`
  - `record ChatMessagePayload(string Role, string Content)`
  - `interface ILLMClient { IAsyncEnumerable<string> StreamChatAsync(string model, IEnumerable<ChatMessagePayload> messages, decimal? temperature, CancellationToken ct); }`
  - `interface ILLMProviderFactory { ILLMClient GetClient(string? providerName = null); }`

- [x] **Step 1: Viết Unit Test cho OpenAiCompatibleClient giả lập SSE stream (`data: {"choices":[{"delta":{"content":"Hello"}}]}`)**
- [x] **Step 2: Chạy test để xác nhận FAIL**
  Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter FullyQualifiedName~LlmClientTests`
- [x] **Step 3: Triển khai `LlmOptions`, `OpenAiCompatibleClient` (đọc SSE `text/event-stream`), `LlmProviderFactory` và đăng ký DI**
- [x] **Step 4: Chạy lại test để xác nhận PASS**
- [x] **Step 5: Commit**
  ```bash
  git add backend/src/Shared/AgentCoreSystem.Shared.Infrastructure/LLM/ backend/tests/AgentCoreSystem.UnitTests/LLM/
  git commit -m "feat(llm): implement generic OpenAI-compatible streaming LLM client"
  ```

---

## Task 2: Khởi Tạo Dự Án Clean Architecture Cho Module.Chat

**Files:**
- Create: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Domain/AgentCoreSystem.Module.Chat.Domain.csproj`
- Create: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Application/AgentCoreSystem.Module.Chat.Application.csproj`
- Create: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Infrastructure/AgentCoreSystem.Module.Chat.Infrastructure.csproj`
- Create: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Presentation/AgentCoreSystem.Module.Chat.Presentation.csproj`
- Modify: `backend/AgentCoreSystem.slnx`
- Modify: `backend/src/Host/AgentCoreSystem.Api/AgentCoreSystem.Api.csproj`

**Interfaces:**
- Produces: 4 tầng Clean Architecture của Module Chat kết nối vào Solution và Host API.

- [x] **Step 1: Tạo csproj cho 4 tầng của `Module.Chat` tham chiếu các tầng Shared tương ứng**
- [x] **Step 2: Đăng ký 4 project vào `backend/AgentCoreSystem.slnx` dưới thư mục `/src/Modules/Chat/`**
- [x] **Step 3: Thêm project references vào `AgentCoreSystem.Api.csproj`**
- [x] **Step 4: Chạy `dotnet build backend/AgentCoreSystem.slnx` để xác nhận build thành công**
- [x] **Step 5: Commit**
  ```bash
  git add backend/src/Modules/Chat/ backend/AgentCoreSystem.slnx backend/src/Host/AgentCoreSystem.Api/AgentCoreSystem.Api.csproj
  git commit -m "feat(chat): scaffold clean architecture projects for Chat module"
  ```

---

## Task 3: Domain Entities & Enums Cho Schema `chat`

**Files:**
- Create: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Domain/Entities/ChatMessage.cs`
- Create: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Domain/Entities/MessageMention.cs`
- Create: `backend/tests/AgentCoreSystem.UnitTests/Chat/ChatDomainTests.cs`

**Interfaces:**
- Produces:
  - `class ChatMessage : BaseEntity { Guid ChannelId, Guid SenderMemberId, string SenderDisplayName, bool IsAgent, string Content, int CallDepth, Guid? ParentMessageId, ICollection<MessageMention> Mentions }`
  - `class MessageMention : BaseEntity { Guid MessageId, Guid MentionedMemberId }`

- [x] **Step 1: Viết Unit Test kiểm tra khởi tạo `ChatMessage`, gán UUIDv7, default `CallDepth = 0`, thêm Mention**
- [x] **Step 2: Chạy test để xác nhận FAIL**
  Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter FullyQualifiedName~ChatDomainTests`
- [x] **Step 3: Triển khai các Entities `ChatMessage` và `MessageMention` trong `Module.Chat.Domain`**
- [x] **Step 4: Chạy lại test để xác nhận PASS**
- [x] **Step 5: Commit**
  ```bash
  git add backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Domain/ backend/tests/AgentCoreSystem.UnitTests/Chat/
  git commit -m "feat(chat): define ChatMessage and MessageMention domain entities"
  ```

---

## Task 4: Cấu Hình ChatDbContext & Tạo EF Core Migration `Initial_Chat_Schema`

**Files:**
- Create: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Infrastructure/Persistence/ChatDbContext.cs`
- Create: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Infrastructure/Persistence/Configurations/ChatMessageConfiguration.cs`
- Create: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Infrastructure/Persistence/Configurations/MessageMentionConfiguration.cs`
- Create: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Infrastructure/DependencyInjection.cs`
- Create: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Infrastructure/Persistence/Migrations/`

**Interfaces:**
- Produces:
  - `ChatDbContext : BaseDbContext` với schema mặc định `chat` và bảng lịch sử migration `__EFMigrationsHistory_Chat`
  - Index trên `ChannelId` và `CreatedDate` (đáp ứng truy vấn nhanh Sliding Window 20 tin nhắn)
  - Extension `IServiceCollection AddChatInfrastructure(this IServiceCollection services, IConfiguration configuration)`

- [x] **Step 1: Định nghĩa `ChatDbContext` và các Fluent API Configurations trong schema `chat`**
- [x] **Step 2: Tạo EF Core Migration đầu tiên `Initial_Chat_Schema`**
  Run: `dotnet ef migrations add Initial_Chat_Schema --project backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Infrastructure --startup-project backend/src/Host/AgentCoreSystem.Api --output-dir Persistence/Migrations`
- [x] **Step 3: Đăng ký `AddChatInfrastructure` và build test thành công**
- [x] **Step 4: Commit**
  ```bash
  git add backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Infrastructure/
  git commit -m "feat(chat): configure ChatDbContext and generate initial chat migrations"
  ```

---

## Task 5: Truy Vấn Liên Module & Application CQRS Cho Chat

**Files:**
- Create: `backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Application/Members/Queries/GetMemberByUserIdAndChannelQuery.cs`
- Create: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Application/Messages/DTOs/ChatMessageDto.cs`
- Create: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Application/Messages/Queries/GetChannelMessagesQuery.cs`
- Create: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Application/Messages/Queries/GetChannelMessagesQueryHandler.cs`
- Create: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Application/Messages/Commands/SaveChatMessageCommand.cs`
- Create: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Application/DependencyInjection.cs`
- Create: `backend/tests/AgentCoreSystem.UnitTests/Chat/ChatApplicationTests.cs`

**Interfaces:**
- Produces:
  - `GetMemberByUserIdAndChannelQuery(Guid UserId, Guid ChannelId) : IRequest<Result<MemberDto>>`
  - `GetChannelMessagesQuery(Guid ChannelId, int Limit = 50, Guid? BeforeMessageId = null) : IRequest<Result<IReadOnlyList<ChatMessageDto>>>`
  - `SaveChatMessageCommand(Guid ChannelId, Guid SenderMemberId, string SenderDisplayName, bool IsAgent, string Content, int CallDepth, Guid? ParentMessageId, List<Guid>? MentionIds) : IRequest<Result<ChatMessageDto>>`

- [x] **Step 1: Viết Unit Test cho `GetChannelMessagesQueryHandler` và `SaveChatMessageCommandHandler`**
- [x] **Step 2: Chạy test để xác nhận FAIL**
- [x] **Step 3: Triển khai Query lấy thành viên trong Module Org, triển khai DTOs, Handlers và `AddChatApplication`**
- [x] **Step 4: Chạy lại test để xác nhận PASS**
- [x] **Step 5: Commit**
  ```bash
  git add backend/src/Modules/Organization/AgentCoreSystem.Module.Organization.Application/ backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Application/ backend/tests/AgentCoreSystem.UnitTests/Chat/
  git commit -m "feat(chat): implement cross-module member lookup and chat CQRS handlers"
  ```

---

## Task 6: SignalR Realtime Hub & Message Dispatcher Service (@Mention Router)

**Files:**
- Create: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Infrastructure/Hubs/SignalRChatHub.cs`
- Create: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Application/Contracts/IMessageDispatcherService.cs`
- Create: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Infrastructure/Services/MessageDispatcherService.cs`
- Modify: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Infrastructure/DependencyInjection.cs`
- Create: `backend/tests/AgentCoreSystem.UnitTests/Chat/MessageDispatcherServiceTests.cs`

**Interfaces:**
- Produces:
  - `SignalRChatHub : Hub`: Methods `JoinChannel(channelId)`, `LeaveChannel(channelId)`, `SendMessage(channelId, content, mentionIds)`
  - Hub Events: `ReceiveMessage(ChatMessageDto)`, `ReceiveMessageChunk(Guid messageId, Guid agentMemberId, string deltaText)`, `ReceiveMessageCompleted(Guid messageId, Guid agentMemberId, string fullContent)`
  - `IMessageDispatcherService.DispatchMessageAsync(ChatMessageDto userMessage, List<Guid> mentionedAgentIds, CancellationToken ct)`:
    - Xếp hàng tuần tự (Sequential Queue) các Agent được tag.
    - Truy vấn 20 tin nhắn gần nhất làm Sliding Window Context từ `ChatDbContext`.
    - Ghép `SystemPrompt` từ `AgentProfile` tương ứng.
    - Gọi `ILLMClient.StreamChatAsync(...)`, stream từng chunk ra SignalR Group.
    - Lưu câu trả lời vào DB với `CallDepth = userMessage.CallDepth + 1`.
    - Dừng kích hoạt nếu `CallDepth >= 3`.

- [x] **Step 1: Viết Unit Test kiểm tra `MessageDispatcherService`: xử lý tuần tự nhiều agent, lấy 20 tin nhắn gần nhất, và ngắt khi `CallDepth >= 3`**
- [x] **Step 2: Chạy test để xác nhận FAIL**
- [x] **Step 3: Triển khai `SignalRChatHub` và `MessageDispatcherService`**
- [x] **Step 4: Chạy lại test để xác nhận PASS**
- [x] **Step 5: Commit**
  ```bash
  git add backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Infrastructure/ backend/tests/AgentCoreSystem.UnitTests/Chat/
  git commit -m "feat(chat): implement SignalRChatHub and sequential @Mention MessageDispatcherService"
  ```

---

## Task 7: Presentation Controllers, OpenAPI OAS v3 & Host Mapping

**Files:**
- Create: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Presentation/Controllers/v1/ChatController.cs`
- Create: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Presentation/DependencyInjection.cs`
- Create: `backend/src/Host/AgentCoreSystem.Api/Extensions/ChatModuleExtensions.cs`
- Modify: `backend/src/Host/AgentCoreSystem.Api/Program.cs`
- Modify: `backend/src/Host/AgentCoreSystem.Api/appsettings.json`

**Interfaces:**
- Produces:
  - `GET /api/v1/channels/{channelId}/messages` (trả về danh sách tin nhắn kèm OAS v3 docs)
  - Map SignalR Hub tại `/hubs/chat`
  - Đăng ký `builder.Services.AddChatModule(builder.Configuration)`
  - Template cấu hình `Llm` trong `appsettings.json`

- [x] **Step 1: Viết `ChatController` v1 kế thừa `BaseApiController` với đầy đủ `<summary>`, `<remarks>`, `[ProducesResponseType]`**
- [x] **Step 2: Viết `ChatModuleExtensions.cs` và ánh xạ Hub trong `Program.cs`**
- [x] **Step 3: Thêm cấu hình mẫu `Llm` vào `appsettings.json` với URL và model có thể cấu hình linh hoạt**
- [x] **Step 4: Chạy `dotnet build backend/AgentCoreSystem.slnx` và `dotnet test` toàn bộ solution đảm bảo 100% PASS**
- [x] **Step 5: Commit**
  ```bash
  git add backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Presentation/ backend/src/Host/AgentCoreSystem.Api/
  git commit -m "feat(chat): expose Chat REST API v1, map SignalR Hub, and register module into Host"
  ```

---

## Task 8: Angular Chat Models, HTTP Service & SignalR Realtime Service

**Files:**
- Create: `frontend/src/app/features/chat/models/chat.models.ts`
- Create: `frontend/src/app/features/chat/services/chat-http.service.ts`
- Create: `frontend/src/app/features/chat/services/chat-signalr.service.ts`
- Create: `frontend/src/app/features/chat/store/chat.store.ts`

**Interfaces:**
- Produces:
  - Models: `ChatMessage`, `MessageMention`, `StreamingChunk`
  - `ChatHttpService.getMessages(channelId, limit, before)`
  - `ChatSignalRService`: `connect()`, `joinChannel(channelId)`, `leaveChannel(channelId)`, `sendMessage(channelId, content, mentionIds)`, observables/signals cho incoming messages và chunks
  - `ChatStore`: Signals `messages = signal<ChatMessage[]>([])`, `streamingChunks = signal<Map<string, string>>(new Map())`, `isConnected = signal<boolean>(false)`

- [x] **Step 1: Định nghĩa TypeScript interfaces cho Chat**
- [x] **Step 2: Viết `ChatHttpService` giao tiếp REST API**
- [x] **Step 3: Cài đặt `@microsoft/signalr` (nếu chưa có) và viết `ChatSignalRService` với cơ chế auto-reconnect**
- [x] **Step 4: Viết `ChatStore` quản lý tin nhắn và ghép nối token streaming mượt mà**
- [x] **Step 5: Commit**
  ```bash
  git add frontend/src/app/features/chat/
  git commit -m "feat(frontend): create chat models, HTTP service, SignalR realtime service, and signals store"
  ```

---

## Task 9: Angular Chat Room UI & @Mention Autocomplete Popover

**Files:**
- Create: `frontend/src/app/features/chat/components/chat-bubble/chat-bubble.component.ts`
- Create: `frontend/src/app/features/chat/components/chat-bubble/chat-bubble.component.html`
- Create: `frontend/src/app/features/chat/components/chat-bubble/chat-bubble.component.scss`
- Create: `frontend/src/app/features/chat/components/mention-popover/mention-popover.component.ts`
- Create: `frontend/src/app/features/chat/components/chat-room/chat-room.component.ts`
- Create: `frontend/src/app/features/chat/components/chat-room/chat-room.component.html`
- Create: `frontend/src/app/features/chat/components/chat-room/chat-room.component.scss`
- Modify: `frontend/src/app/features/organization/pages/organization-shell/organization-shell.component.html`
- Modify: `frontend/src/app/features/organization/pages/organization-shell/organization-shell.component.ts`

**Interfaces:**
- Produces:
  - `ChatBubbleComponent`: Phân biệt User vs Agent (badge `AI`, typing cursor, format nội dung markdown)
  - `MentionPopoverComponent`: Popover hiển thị gợi ý danh sách `Member` trong channel khi gõ `@`
  - `ChatRoomComponent`: Tích hợp vào Kênh nhóm khi người dùng chọn 1 Channel từ cây phòng ban:
    - Khu vực hiển thị tin nhắn (tự động cuộn xuống cuối khi có tin mới hoặc đang stream)
    - Input nhập tin nhắn kèm xử lý phím `@` và `Enter` để gửi
- [x] **Step 1: Xây dựng `ChatBubbleComponent` hiển thị bong bóng chat với cursor nhấp nháy khi stream**
- [x] **Step 2: Xây dựng `MentionPopoverComponent` bắt phím `@` và autocomplete tag**
- [x] **Step 3: Xây dựng `ChatRoomComponent` tích hợp SignalR store và HTTP history load**
- [x] **Step 4: Nhúng `ChatRoomComponent` vào `OrganizationShellComponent` bên cạnh channel detail**
- [x] **Step 5: Chạy `npm run build` trong thư mục `frontend` đảm bảo build thành công 0 lỗi**
- [x] **Step 6: Commit**
  ```bash
  git add frontend/src/app/features/chat/ frontend/src/app/features/organization/
  git commit -m "feat(frontend): implement ChatRoom UI, token streaming bubbles, and @mention autocomplete"
  ```

---

## Task 10: Kiểm Thử Toàn Diện (End-to-End & Playwright Test Suite)

**Files:**
- Create: `frontend/e2e/chat-realtime.spec.ts`
- Modify: `justfile`

**Interfaces:**
- Produces:
  - Kịch bản test E2E kiểm tra: Mở Kênh chat -> Lấy lịch sử tin nhắn -> Gõ `@` hiển thị gợi ý -> Gửi tin nhắn -> Nhận tin nhắn mới và kiểm tra luồng stream.

- [x] **Step 1: Viết kịch bản kiểm thử Playwright cho Chat Room và @Mention popover**
- [x] **Step 2: Thêm lệnh `just test-e2e-chat` vào `justfile`**
- [x] **Step 3: Chạy toàn bộ test suite (`just test-all` hoặc `dotnet test` + `npm run build`)**
- [x] **Step 4: Commit**
  ```bash
  git add frontend/e2e/ justfile
  git commit -m "test: add Playwright E2E chat realtime test suite and justfile command"
  ```

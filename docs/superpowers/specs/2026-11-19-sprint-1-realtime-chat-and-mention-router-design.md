# Design Document: Sprint 1 - Kênh Chat Thời Gian Thực & Bộ Lọc Kích Hoạt (@Mention Router)

- **Tác giả:** Phong Minh Đặng / Antigravity
- **Ngày tạo:** 19/11/2026
- **Trạng thái:** Bản hoàn thiện chờ phê duyệt (Final Draft for Approval)
- **Dự án:** AgentCoreSystem (.NET 9 Modular Monolith & Angular Standalone)
- **Tài liệu gốc tham chiếu:** [docs/superpowers/specs/2026-11-18-agent-core-system-design.md](file:///c:/Workplaces/PROJECT/AgentCoreSystem/docs/superpowers/specs/2026-11-18-agent-core-system-design.md)

---

## 1. Mục Đích & Bối Cảnh (Intent & The Why)

Sau khi hoàn thành Sprint 0 (khởi tạo nền tảng Quản trị Doanh nghiệp, Phòng ban, Kênh nhóm `TeamChannel` và Danh tính Nhân sự AI `AgentProfile`), hệ thống cần một không gian cộng tác trực tiếp để con người và các Agent làm việc cùng nhau.

Mục tiêu cốt lõi của **Sprint 1** là:
1. Xây dựng phân hệ **`Module.Chat`** (schema `chat`) chịu trách nhiệm lưu trữ, truy vấn lịch sử và điều phối tin nhắn hội thoại.
2. Thiết lập đường truyền hai chiều thời gian thực với **SignalR** (`SignalRChatHub`), hỗ trợ gửi nhận tin nhắn tức thì và **Token Streaming** (Agent phản hồi từng từ như gõ phím trực tiếp).
3. Xây dựng **Hạ tầng LLM Generic độc lập** (`Shared.Infrastructure/LLM`) tương thích chuẩn công nghiệp OpenAI-compatible REST API (`/v1/chat/completions`), sẵn sàng kết nối với bất kỳ Local Gateway/Proxy (như 9router) hay Cloud Provider nào mà không bị ràng buộc thương hiệu hoặc hardcode thông số.
4. Xây dựng **`MessageDispatcherService`** đóng vai trò `@Mention Router`: Nhận diện thẻ tag `@AgentName` hoặc `@all`, điều phối thực thi **Tuần tự (Sequential Queue)** khi có nhiều Agent, trích xuất ngữ cảnh **Sliding Window 20 tin nhắn gần nhất**, chuyển tiếp đến Provider LLM, stream từng token về client qua SignalR và ngăn chặn đàm thoại vòng lặp qua `CallDepth` trong DB (`MaxCallDepth = 3`).
5. Xây dựng giao diện **Angular Chat Room** hiện đại: Tích hợp vào từng `TeamChannel`, hỗ trợ popup autocomplete danh sách thành viên/Agent khi gõ `@`, và hiển thị bong bóng chat streaming mượt mà theo chuẩn Minimalist UI / Hallmark.

---

## 2. Mục Tiêu, Phi Mục Tiêu & Chống Mục Tiêu (Goals, Non-Goals & Anti-Goals)

### 2.1. Goals (Mục tiêu bắt buộc)
- **Backend Clean Architecture:** Dựng `Module.Chat` gồm 4 project (`Domain`, `Application`, `Infrastructure`, `Presentation`) trên schema `chat` với bảng lịch sử `__EFMigrationsHistory_Chat`.
- **Database Entities (Schema `chat`):**
  - `ChatMessage`: `Id` (UUIDv7), `ChannelId` (Guid), `SenderMemberId` (Guid), `SenderDisplayName` (nvarchar(200)), `IsAgent` (bool), `Content` (nvarchar(max)), `CallDepth` (int, mặc định 0), `ParentMessageId` (Guid, nullable), `CreatedDate` (datetimeoffset).
  - `MessageMention`: `Id` (UUIDv7), `MessageId` (Guid), `MentionedMemberId` (Guid).
- **Hạ tầng LLM Generic (Zero Hardcode):**
  - Cung cấp `ILLMClient` và `ILLMProviderFactory` trong `Shared.Infrastructure/LLM`.
  - Triển khai `OpenAiCompatibleClient` đọc thông số `BaseUrl`, `ApiKey`, `DefaultModel`, `TimeoutSeconds` qua `IOptions<LlmOptions>` từ cấu hình với cơ chế **Fail-Fast** khi khởi động.
  - Hỗ trợ phương thức `IAsyncEnumerable<string> StreamChatAsync(...)` phục vụ Token Streaming.
- **REST API Truy Vấn Lịch Sử:**
  - `GET /api/v1/channels/{channelId}/messages?limit=50&before={messageId}` hỗ trợ phân trang chuẩn mực khi tải tin nhắn ban đầu hoặc scroll-up tải tin cũ.
- **SignalR Realtime Hub:**
  - `SignalRChatHub` ánh xạ endpoint `/hubs/chat`.
  - Quản lý Group theo `channelId`: Client gọi `JoinChannel(channelId)` / `LeaveChannel(channelId)`.
  - Phát sự kiện: `ReceiveMessage` (tin nhắn mới từ người dùng), `ReceiveMessageChunk` (từng token từ Agent), `ReceiveMessageCompleted` (hoàn tất phản hồi và đã lưu DB).
  - Xác thực người gửi `SenderMemberId` an toàn từ Server dựa trên `UserId` từ JWT Claims qua `IOrganizationModuleApi`, chống giả mạo danh tính.
- **@Mention Router & Điều Phối Tuần Tự (Sequential Queue):**
  - Parse nội dung tin nhắn tìm các `Member` được tag hoặc `@all`.
  - Nếu có nhiều Agent được tag: Đưa vào hàng đợi xử lý tuần tự (Agent 1 trả lời xong -> lưu DB -> Agent 2 phản hồi kế tiếp với ngữ cảnh đã bao gồm câu trả lời của Agent 1).
  - Sliding Window: Lấy tối đa 20 tin nhắn gần nhất của kênh (Passive Listening context) + `SystemPrompt` của AgentProfile.
  - Chống loop: Gán `CallDepth = parent.CallDepth + 1`. Nếu `CallDepth >= 3`, dừng chuỗi kích hoạt tiếp theo.
- **Angular Frontend:**
  - `ChatRoomComponent` standalone, OnPush, Signal state management.
  - Tải lịch sử ban đầu qua REST API; lắng nghe tin nhắn mới & token streaming qua SignalR.
  - Menu gõ `@` autocomplete danh sách Member trong Channel.
  - Hiển thị bong bóng chat, format Markdown và hiệu ứng typing streaming theo thời gian thực.

### 2.2. Non-Goals (Không làm trong Sprint 1)
- Chưa triển khai Function Calling / Tool Calling (dành riêng cho Sprint 3).
- Chưa tích hợp Hangfire chạy Background Task không đồng bộ kéo dài nhiều giờ (dành cho Sprint 2).
- Không tạo khóa ngoại (Foreign Key) trực tiếp giữa bảng `chat.ChatMessages` và `org.TeamChannels` / `org.Members` để bảo đảm tính độc lập của Modular Monolith.

### 2.3. Anti-Goals (Tuyệt đối nghiêm cấm)
- **CẤM HARDCODE**: Tuyệt đối không hardcode URL `http://localhost:20128/v1` hoặc tên `9router` vào mã nguồn C# hay TypeScript.
- **CẤM POLLING HTTP**: Không dùng `setInterval` hay HTTP Polling để lấy tin nhắn mới; mọi tương tác cập nhật chat trực tiếp phải qua SignalR WebSocket.
- **CẤM CHAT LOOP**: Không cho phép Agent phản hồi vô hạn khi các Agent tag chéo nhau trong phòng chat.
- **CẤM GIẢ MẠO CLIENT**: Client không được tự gửi `SenderMemberId`; Server tự giải quyết từ User Token.

---

## 3. Các Phương Án Kỹ Thuật Đã Cân Nhắc (Approaches)

### Phương án 1: Shared LLM Provider Abstraction & Generic Options (ĐƯỢC CHỌN)
- **Ý tưởng:** Xây dựng `ILLMClient` và `ILLMProviderFactory` tại `AgentCoreSystem.Shared.Infrastructure/LLM`. Triển khai `OpenAiCompatibleClient` kết nối tới bất kỳ Gateway/Proxy OpenAI-compatible nào thông qua cấu hình `IOptions<LlmOptions>`.
- **Ưu điểm:**
  - Đảm bảo ranh giới Modular Monolith: Không sinh ra phụ thuộc vòng tròn giữa các module.
  - Tái sử dụng dễ dàng cho `Module.Tasks` (Sprint 2) và `Module.Agents` (Sprint 3).
  - Tự do thay đổi Backend AI (9router, Ollama, vLLM, OpenAI...) mà không cần sửa code.
- **Đánh đổi:** Sprint 1 cấu hình thông qua `appsettings.json` hoặc Environment Variables thay vì giao diện Web UI CRUD.

### Phương án 2: Scaffold sớm Module Agents để lưu Provider trong Database
- **Ý tưởng:** Khởi tạo trước schema `agent` và bảng `ProviderConfiguration` trong database.
- **Lý do loại bỏ:** Làm phình to phạm vi Sprint 1, gây phân tán nguồn lực khỏi nhiệm vụ trọng tâm là SignalR và Chat UX.

### Phương án 3: Lưu URL Provider vào trực tiếp AgentProfile (Module Org)
- **Ý tưởng:** Thêm trường `EndpointUrl` vào bảng `org.AgentProfiles`.
- **Lý do loại bỏ:** Vi phạm nguyên tắc DRY (mỗi Agent lại phải cấu hình lại cùng một endpoint), đồng thời làm biến dạng schema `org` đã đóng gói ổn định từ Sprint 0.

---

## 4. Đặc Tả Chi Tiết Kiến Trúc Triển Khai (The How)

### 4.1. Cấu hình Generic LLM Options (Shared Infrastructure)
```csharp
namespace AgentCoreSystem.Shared.Infrastructure.LLM;

public sealed class LlmOptions
{
    public const string SectionName = "Llm";
    public string DefaultProvider { get; set; } = "LocalGateway";
    public Dictionary<string, LlmProviderConfig> Providers { get; set; } = new();
}

public sealed class LlmProviderConfig
{
    public string ProviderType { get; set; } = "OpenAiCompatible";
    public string BaseUrl { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string DefaultModel { get; set; } = "gemini-2.5-flash";
    public int TimeoutSeconds { get; set; } = 60;
}
```
*Cơ chế Fail-Fast:* Khởi động ứng dụng sẽ kiểm tra nếu `BaseUrl` của Provider mặc định bị rỗng hoặc không phải URL hợp lệ sẽ ném `InvalidOperationException`.

### 4.2. Cấu Trúc Dữ Liệu Module Chat (Schema: `chat`)
```csharp
namespace AgentCoreSystem.Module.Chat.Domain.Entities;

public class ChatMessage : BaseEntity
{
    public Guid ChannelId { get; private set; }
    public Guid SenderMemberId { get; private set; }
    public string SenderDisplayName { get; private set; } = string.Empty;
    public bool IsAgent { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public int CallDepth { get; private set; } = 0;
    public Guid? ParentMessageId { get; private set; }
    public ICollection<MessageMention> Mentions { get; private set; } = [];
}

public class MessageMention : BaseEntity
{
    public Guid MessageId { get; private set; }
    public Guid MentionedMemberId { get; private set; }
}
```

### 4.3. REST API & SignalR Realtime Hub
- **REST API:**
  - `GET /api/v1/channels/{channelId}/messages?limit=50&before={messageId}`
  - Trả về danh sách tin nhắn phân trang kèm thông tin người gửi, `isAgent`, và danh sách mentions.
- **SignalR Hub Endpoint:** `/hubs/chat`
- **Client -> Server Hub Methods:**
  - `JoinChannel(Guid channelId)`: Thêm connection vào group `channel_{channelId}`.
  - `LeaveChannel(Guid channelId)`: Rời khỏi group.
  - `SendMessage(Guid channelId, string content, List<Guid> mentionMemberIds)`: Gửi tin nhắn mới vào phòng chat (Server tự phân giải `SenderMemberId` từ JWT Token claims).
- **Server -> Client Events:**
  - `ReceiveMessage(ChatMessageDto message)`: Bắn ra khi có tin nhắn mới từ người dùng.
  - `ReceiveMessageChunk(Guid messageId, Guid agentMemberId, string deltaText)`: Bắn từng đoạn chữ sinh ra từ Agent để client hiển thị typing stream.
  - `ReceiveMessageCompleted(Guid messageId, Guid agentMemberId, string fullContent)`: Báo hiệu Agent đã hoàn thành câu trả lời và đã lưu vào DB.

### 4.4. Cơ Chế Điều Phối (@Mention Router & Sequential Execution)
1. **Phát Hiện Tag:** Khi người dùng gửi tin nhắn có tag `@AgentName` hoặc `@all`:
2. **Xếp Hàng Tuần Tự (Sequential Queue):**
   - Nếu có nhiều Agent được tag, hệ thống đưa vào hàng đợi thực thi lần lượt từng Agent.
3. **Sliding Window Context:**
   - Trước mỗi lượt gọi Agent, lấy 20 tin nhắn gần nhất theo thứ tự thời gian trong `ChannelId` từ DB.
   - Khi Agent 1 vừa hoàn tất và lưu vào DB, Agent 2 bắt đầu thực thi sẽ thấy ngay câu trả lời của Agent 1 trong 20 tin nhắn này.
4. **LLM Invocation:**
   - Ghép `SystemPrompt` của Agent + History (20 tin nhắn) + Tin nhắn kích hoạt.
   - Gọi `ILLMClient.StreamChatAsync(...)`.
5. **Real-time Forwarding:** Mỗi chunk nhận được từ stream được đẩy tức thời qua `IHubContext<SignalRChatHub>.Clients.Group(...).SendAsync("ReceiveMessageChunk", ...)`.
6. **Persistence:** Khi stream kết thúc, lưu câu trả lời đầy đủ của Agent vào bảng `chat.ChatMessages` với `CallDepth = parentMessage.CallDepth + 1`, `ParentMessageId = parentMessage.Id`, sau đó bắn sự kiện `ReceiveMessageCompleted`.
7. **Loop Prevention:** Nếu `CallDepth >= 3`, không tiếp tục kích hoạt lượt gọi Agent tiếp theo.

### 4.5. Thiết Kế Giao Diện Angular Standalone
- **Trang Chat Room (`ChatRoomComponent`):**
  - Tích hợp vào Kênh nhóm (`TeamChannel`).
  - Gọi REST API lấy 50 tin nhắn gần nhất khi mở phòng chat.
  - Kết nối `ChatSignalRService` tự động tham gia channel group.
- **Bong bóng chat (Chat Bubbles):**
  - Phân biệt rõ giữa Người dùng (`UserMessage`) và Trợ lý AI (`AgentMessage` với Avatar, tên Model, badge `AI`).
  - Hỗ trợ định dạng Markdown (tiêu đề, in đậm, khối code, danh sách).
  - Bong bóng của Agent hiển thị con trỏ nhấp nháy (typing cursor) khi đang nhận chunks.
- **Input & @Mention Autocomplete (`MentionTextareaComponent`):**
  - Lắng nghe sự kiện gõ phím `@`.
  - Hiển thị popover gợi ý danh sách thành viên trong Kênh (kèm phân loại `Human` hay `Agent`).
  - Nhấn `Enter` hoặc click để chèn tag dạng `@DisplayName`.

---

## 5. Những Việc Để Lại Cho Kỹ Sư Triển Khai (What's Left to the Builder)
1. Cấu hình chi tiết `HttpClient` resiliency (retry policy với Exponential Backoff qua `Microsoft.Extensions.Http.Resilience`).
2. Xây dựng bộ parser regex nhận diện các cú pháp `@DisplayName` hoặc `@all` trong nội dung tin nhắn.
3. Tích hợp thư viện render Markdown nhẹ cho Angular (như `marked` + `dompurify`) để hiển thị code block và formatting an toàn.
4. Tinh chỉnh CSS/SCSS theo đúng bộ quy tắc Minimalist UI (canvas `#F7F5F2`, font chữ rõ ràng, hiệu ứng gõ mượt mà).

---

## 6. Tiêu Chí Nghiệm Thu (Acceptance Criteria)
1. `dotnet build` và `dotnet test` toàn bộ giải pháp thành công 0 errors, 0 warnings.
2. Migration `Initial_Chat_Schema` chạy thành công trên SQL Server schema `chat`, bảng chứa đầy đủ `CallDepth` và `ParentMessageId`.
3. REST API `GET /api/v1/channels/{channelId}/messages` trả về lịch sử tin nhắn kèm phân trang.
4. Kết nối SignalR `/hubs/chat` hoạt động ổn định giữa nhiều client đồng thời.
5. Gõ tin nhắn tag `@TênAgent`: Agent kích hoạt, stream từng token về giao diện qua SignalR và lưu lịch sử vào DB.
6. Khi tag nhiều Agent: Các Agent trả lời tuần tự (Sequential Queue), Agent sau nhìn thấy câu trả lời của Agent trước.
7. Thử nghiệm gọi chéo giữa các Agent: Dừng lại đúng ngưỡng `MaxCallDepth = 3`, triệt tiêu hoàn toàn chat loop.
8. Frontend Angular build thành công (`npm run build` không lỗi type).

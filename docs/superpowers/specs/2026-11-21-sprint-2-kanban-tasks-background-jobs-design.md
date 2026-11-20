# Design Document: Sprint 2 - Bảng Kanban & Tác vụ Chạy Ngầm (Tasks & Background Jobs)

- **Tác giả:** Phong Minh Đặng / Antigravity
- **Ngày tạo:** 21/11/2026
- **Trạng thái:** Bản hoàn thiện chờ phê duyệt (Final Draft for Approval)
- **Dự án:** AgentCoreSystem (.NET 9 Modular Monolith & Angular Standalone)
- **Tài liệu gốc tham chiếu:** [docs/superpowers/specs/2026-11-18-agent-core-system-design.md](file:///c:/Workplaces/PROJECT/AgentCoreSystem/docs/superpowers/specs/2026-11-18-agent-core-system-design.md)

---

## 1. Mục Đích & Bối Cảnh (Intent & The Why)

Sau khi hoàn thành **Sprint 0** (Cấu trúc tổ chức, Phòng ban, Kênh nhóm `TeamChannel`, Danh tính `AgentProfile`) và **Sprint 1** (Kênh Chat thời gian thực với SignalR và bộ định tuyến `@Mention Router`), hệ thống đã có kênh trao đổi trực tiếp giữa Người dùng và Agent.

Tuy nhiên, trong mô hình cộng tác nhân sự AI tự chủ (**Autonomous AI Workforce**), các công việc phức tạp như *nghiên cứu thị trường, cào dữ liệu, phân tích mã nguồn, sinh tài liệu* không thể bắt người dùng ngồi chờ phản hồi tức thì trên khung chat. Chúng đòi hỏi:
1. **Không gian Quản lý Công việc Trực quan (Task Management & Kanban):** Một bảng Kanban theo từng kênh (`TeamChannel`) để theo dõi rõ ràng ai đang làm gì, việc nào đang chờ, việc nào đang xử lý.
2. **Xử Lý Tác Vụ Bất Đồng Bộ (Background Jobs via Hangfire):** Khi giao việc cho Agent, hệ thống phải đưa vào hàng đợi chạy ngầm (Hangfire worker với MongoDB storage), giải phóng luồng HTTP/SignalR, hỗ trợ tự phục hồi khi lỗi và có khả năng báo cáo tiến độ.
3. **Cổng Nghiệm Thu Con Người (Review Gate - Human-in-the-Loop):** Agent không được tự ý đóng task (`Done`) mà khi hoàn thành phải nộp sản phẩm đầu ra (**Artifacts**) vào trạng thái `InReview`. Con người có thẩm quyền bấm **Duyệt (Approve)** hoặc **Yêu cầu làm lại (Request Revision)**. Góp ý này sẽ được đưa ngược lại vào bộ nhớ cá nhân (`PrivateMemory`) của Agent để rút kinh nghiệm cho các lần sau.

---

## 2. Mục Tiêu, Phi Mục Tiêu & Chống Mục Tiêu (Goals, Non-Goals & Anti-Goals)

### 2.1. Goals (Mục tiêu bắt buộc)
- **Backend Clean Architecture:** Dựng `Module.Tasks` độc lập gồm 4 project (`Domain`, `Application`, `Infrastructure`, `Presentation`) trên schema CSDL riêng `task` với bảng migration `__EFMigrationsHistory_Tasks`.
- **Database Entities (Schema `task`):**
  - `TaskItem`: `Id` (UUIDv7), `ChannelId` (Guid), `TaskCode` (string, vd: `TSK-001`), `Title` (nvarchar(250)), `Description` (nvarchar(max)), `Status` (enum `ToDo`, `InProgress`, `InReview`, `Done`, `Failed`), `Priority` (enum `Low`, `Medium`, `High`, `Urgent`), `DueDate` (DateTimeOffset?), `CreatedDate`, `UpdatedDate`.
  - `TaskAssignment`: `Id` (UUIDv7), `TaskId` (Guid), `AssigneeMemberId` (Guid), `IsAgent` (bool), `RoleInTask` (nvarchar(100)).
  - `TaskArtifact`: `Id` (UUIDv7), `TaskId` (Guid), `FileName` (nvarchar(250)), `FilePath` (nvarchar(1000)), `FileType` (nvarchar(50)), `FileSize` (long), `CreatedByMemberId` (Guid), `Summary` (nvarchar(max)).
  - `TaskActivityLog`: `Id` (UUIDv7), `TaskId` (Guid), `ActorMemberId` (Guid), `ActionType` (string), `FromStatus` (TaskStatus?), `ToStatus` (TaskStatus?), `Comment` (nvarchar(max)).
- **State Machine & Review Gate:**
  - Chuyển trạng thái hợp lệ theo ma trận định sẵn.
  - Khi task ở `InReview`, chỉ có thành viên `Human` (Quản lý) mới có quyền `Approve` hoặc `Request Revision`.
  - Khi `Request Revision`, nhận xét của người dùng được gửi qua MediatR để cập nhật vào `PrivateMemory` của Agent trong Module `Organization`.
- **Hangfire Job Dispatcher:**
  - Đăng ký `TaskAgentExecutionJob` vào hàng đợi Hangfire MongoDB khi task được giao cho Agent và chuyển sang `InProgress`.
  - Tái sử dụng `ILLMClient` (từ `Shared.Infrastructure/LLM`) để Agent thực thi sinh nội dung/báo cáo, tạo `TaskArtifact` và tự động cập nhật task sang `InReview`.
- **SignalR Realtime Hub (`/hubs/tasks`):**
  - Broadcast các sự kiện Kanban theo Group `channel_{channelId}`: `TaskCreated`, `TaskStatusChanged`, `TaskAssigned`, `TaskArtifactAdded`, `TaskReviewCompleted`.
- **Frontend Angular Kanban Board:**
  - Angular 19+ Standalone, OnPush, Signal state management, kéo thả với `@angular/cdk/drag-drop`.
  - 4 cột: `To Do`, `In Progress` (có badge running), `In Review` (cổng Review Gate với Artifact & nút Approve/Revise), `Done`.
  - Tích hợp điều hướng Tabs: `💬 Kênh Chat` $\leftrightarrow$ `📋 Bảng Kanban` trong từng Kênh `TeamChannel`.

### 2.2. Non-Goals (Không làm trong Sprint 2)
- Chưa triển khai Tool Calling / REST API dynamic connector (dành riêng cho Sprint 3).
- Chưa triển khai Python Virtual Environment (.venv sandbox) trên máy chủ (dành riêng cho Sprint 4).
- Không tạo Foreign Key trực tiếp giữa schema `task` với `org` hay `chat` (tuân thủ nguyên tắc Modular Monolith).

### 2.3. Anti-Goals (Tuyệt đối nghiêm cấm)
- **CẤM HARDCODE**: Không hardcode ID, URL, connection strings hay thông số cố định trong code.
- **CẤM POLLING HTTP**: Không dùng `setInterval` hay HTTP Polling trên Angular để tải lại bảng Kanban; mọi cập nhật thẻ phải truyền qua SignalR `/hubs/tasks`.
- **CẤM AGENT TỰ DUYỆT (SELF-APPROVAL)**: Agent tuyệt đối không thể tự chuyển trạng thái từ `InReview` sang `Done`. Thao tác duyệt chỉ dành riêng cho con người (`IsAgent == false`).
- **CẤM NUỐT LỖI HANGFIRE**: Khi Agent chạy lỗi trong Hangfire, phải ghi log chi tiết, cập nhật Task sang `Failed` kèm lý do lỗi vào `TaskActivityLog`.

---

## 3. Các Phương Án Kỹ Thuật Đã Cân Nhắc (Approaches)

### Phương án 1: Tách riêng SignalR TasksHub (`/hubs/tasks`) độc lập với ChatHub (`/hubs/chat`) (ĐƯỢC CHỌN)
- **Ý tưởng:** Module `Tasks` sở hữu riêng `SignalRTasksHub` tại endpoint `/hubs/tasks`.
- **Ưu điểm:**
  - Tuân thủ nghiêm ngặt ranh giới Modular Monolith: Module Tasks không phụ thuộc vào Module Chat.
  - Dễ kiểm thử độc lập và tách service khi cần.
  - Phân tách rõ ràng giữa dòng tải dữ liệu chat (text streaming) và sự kiện state Kanban (kéo thả, đổi trạng thái).
- **Đánh đổi:** Client Angular kết nối 2 SignalR Hubs khi mở trang Kênh nhóm (được quản lý gọn qua Signal Service).

### Phương án 2: Gộp chung SignalR Tasks Events vào ChatHub
- **Lý do loại bỏ:** Vi phạm Single Responsibility Principle, tạo sự phụ thuộc trực tiếp giữa hai module độc lập, gây khó khăn cho việc đóng gói và mở rộng sau này.

### Phương án 3: Lưu trữ Artifacts trực tiếp trong SQL Server dạng `varbinary(max)`
- **Lý do loại bỏ:** Làm phình to dung lượng SQL Server, ảnh hưởng tiêu cực đến hiệu năng backup và I/O.
- **Giải pháp chọn:** Lưu trữ file vật lý trên thư mục lưu trữ cục bộ có tổ chức (`Storage/Artifacts/{ChannelId}/{TaskId}/`), database chỉ lưu metadata đường dẫn, kích thước, mã băm và tóm tắt markdown.

---

## 4. Đặc Tả Chi Tiết Kiến Trúc Triển Khai (The How)

### 4.1. Cấu Trúc Dữ Liệu Module Tasks (Schema: `task`)

```csharp
namespace AgentCoreSystem.Module.Tasks.Domain.Enums
{
    public enum TaskItemStatus
    {
        ToDo = 0,
        InProgress = 1,
        InReview = 2,
        Done = 3,
        Failed = 4
    }

    public enum TaskPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Urgent = 3
    }
}
```

```csharp
namespace AgentCoreSystem.Module.Tasks.Domain.Entities
{
    public class TaskItem : BaseEntity
    {
        public Guid ChannelId { get; private set; }
        public string TaskCode { get; private set; } = string.Empty; // Vd: TSK-001
        public string Title { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public TaskItemStatus Status { get; private set; } = TaskItemStatus.ToDo;
        public TaskPriority Priority { get; private set; } = TaskPriority.Medium;
        public DateTimeOffset? DueDate { get; private set; }
        public DateTimeOffset? StartedDate { get; private set; }
        public DateTimeOffset? CompletedDate { get; private set; }

        public ICollection<TaskAssignment> Assignments { get; private set; } = [];
        public ICollection<TaskArtifact> Artifacts { get; private set; } = [];
        public ICollection<TaskActivityLog> ActivityLogs { get; private set; } = [];

        // Domain Methods kiểm soát State Machine
        public Result MoveTo(TaskItemStatus newStatus, Guid actorMemberId, bool isActorAgent, string? comment = null) { ... }
        public Result AssignTo(Guid memberId, bool isAgent, string roleInTask, Guid actorMemberId) { ... }
        public Result AddArtifact(string fileName, string filePath, string fileType, long fileSize, string summary, Guid creatorMemberId) { ... }
    }

    public class TaskAssignment : BaseEntity
    {
        public Guid TaskId { get; private set; }
        public Guid AssigneeMemberId { get; private set; }
        public bool IsAgent { get; private set; }
        public string RoleInTask { get; private set; } = "Assignee";
    }

    public class TaskArtifact : BaseEntity
    {
        public Guid TaskId { get; private set; }
        public string FileName { get; private set; } = string.Empty;
        public string FilePath { get; private set; } = string.Empty;
        public string FileType { get; private set; } = string.Empty;
        public long FileSize { get; private set; }
        public Guid CreatedByMemberId { get; private set; }
        public string Summary { get; private set; } = string.Empty;
    }

    public class TaskActivityLog : BaseEntity
    {
        public Guid TaskId { get; private set; }
        public Guid ActorMemberId { get; private set; }
        public string ActionType { get; private set; } = string.Empty;
        public TaskItemStatus? FromStatus { get; private set; }
        public TaskItemStatus? ToStatus { get; private set; }
        public string? Comment { get; private set; }
    }
}
```

### 4.2. State Machine & Cổng Nghiệm Thu (Review Gate)

```
        ┌────────────────────────────────────────────────────────┐
        │                                                        │
        ▼                                                        │
    [ To Do ] ────(Start / Assign)────► [ In Progress ]          │
        ▲                                      │                 │
        │                                 (Agent Finish)         │
        │                                      │                 │
        │                                      ▼                 │
        │                             [ In Review ]              │
        │                               (Review Gate)            │
        │                               /           \            │
        │            (Human Request Revision)     (Human Approve)│
        │                     /                       \          │
        └────────────────────                          ▼         │
                                                   [ Done ]      │
                                                                 │
    [ Bất kỳ trạng thái ] ──────(Lỗi / Timeout)──────► [ Failed ]┘
```

**Ràng Buộc Nghiệp Vụ:**
1. Khi Task được chuyển sang `InReview`, chỉ có người dùng **Human** (`IsAgent == false`) mới có quyền thực hiện action:
   - **`Approve`**: Chuyển trạng thái sang `Done`, ghi log nghiệm thu.
   - **`RequestRevision`**: Chuyển trạng thái về lại `InProgress` (hoặc `ToDo`) kèm ghi chú góp ý.
2. **Feedback Loop (Đào tạo Agent):** Khi có `RequestRevision`, hệ thống phát MediatR Command cập nhật nhận xét vào `PrivateMemoryNotes` của Agent Profile (`org.AgentProfiles`) để Agent ghi nhớ sai sót cho các lượt thực thi kế tiếp.

### 4.3. Kiến Trúc Hangfire Background Job
- **Interface:** `ITaskAgentJobRunner`
- **Method:** `Task ExecuteTaskAsync(Guid taskId, CancellationToken cancellationToken);`
- **Quy trình Thực thi:**
  1. Đọc thông tin `TaskItem` và `TaskAssignment` từ CSDL.
  2. Truy vấn danh tính Agent (`SystemPrompt`, `ModelName`, `PrivateMemoryNotes`) qua MediatR Query sang `Module.Organization`.
  3. Khởi tạo câu lệnh yêu cầu gửi tới `ILLMClient` (Generic OpenAI-compatible client).
  4. Nhận kết quả từ LLM, ghi ra file artifact tại `Storage/Artifacts/{ChannelId}/{TaskId}/{artifact_name}.md`.
  5. Cập nhật `TaskArtifact`, chuyển trạng thái task từ `InProgress` sang `InReview`.
  6. Broadcast sự kiện qua SignalR `SignalRTasksHub` để màn hình Kanban của người dùng tự động nhảy thẻ sang cột **Chờ Nghiệm Thu**.

### 4.4. REST API Endpoints (`/api/v1/`)

| Method | Endpoint | Mô tả |
| :--- | :--- | :--- |
| `GET` | `/api/v1/channels/{channelId}/tasks` | Lấy danh sách task của kênh theo các cột Kanban |
| `GET` | `/api/v1/tasks/{taskId}` | Lấy chi tiết 1 task kèm assignments, artifacts và activity logs |
| `POST` | `/api/v1/channels/{channelId}/tasks` | Tạo mới một task |
| `PATCH` | `/api/v1/tasks/{taskId}/status` | Cập nhật trạng thái (Drag-and-Drop trên Kanban) |
| `POST` | `/api/v1/tasks/{taskId}/assign` | Giao việc cho Human hoặc Agent |
| `POST` | `/api/v1/tasks/{taskId}/review` | Nghiệm thu task (Approve hoặc Request Revision) |
| `GET` | `/api/v1/tasks/{taskId}/artifacts/{artifactId}/download` | Tải về sản phẩm đầu ra |

### 4.5. SignalR Realtime Hub (`/hubs/tasks`)

- **Client -> Server:**
  - `JoinChannelTasks(Guid channelId)`: Tham gia group theo dõi Kanban của kênh.
  - `LeaveChannelTasks(Guid channelId)`: Rời khỏi group.
- **Server -> Client Events:**
  - `TaskCreated(TaskDto task)`
  - `TaskStatusChanged(Guid taskId, TaskItemStatus oldStatus, TaskItemStatus newStatus, Guid actorMemberId)`
  - `TaskAssigned(Guid taskId, TaskAssignmentDto assignment)`
  - `TaskArtifactAdded(Guid taskId, TaskArtifactDto artifact)`
  - `TaskReviewDecision(Guid taskId, string decision, string? feedback, Guid reviewerMemberId)`

### 4.6. Angular Kanban Frontend Slices

- Thư mục: `frontend/src/app/features/tasks/`
- **Cấu trúc Components:**
  - `KanbanBoardComponent`: Component chính chứa 4 cột Drag & Drop (`cdkDropListGroup`, `cdkDropList`, `cdkDrag`).
  - `KanbanColumnComponent`: Cột hiển thị task theo trạng thái (`To Do`, `In Progress`, `In Review`, `Done`).
  - `TaskCardComponent`: Thẻ task hiển thị mã task, tiêu đề, badge ưu tiên, avatar người nhận (Human hoặc Agent robot badge), progress indicator khi Hangfire đang chạy.
  - `TaskDetailModalComponent`: Modal xem đầy đủ mô tả, lịch sử hoạt động, danh sách artifacts kèm nút tải/xem trước.
  - `ReviewGateModalComponent`: Modal duyệt nghiệm thu dành riêng cho Quản lý khi task ở cột `In Review`.
- **State Management:**
  - `TasksStore` quản lý Signals: `tasks()`, `columns()`, `selectedTask()`, `isSignalRConnected()`.
  - Tích hợp `TasksRealtimeService` kết nối SignalR `/hubs/tasks`.

---

## 5. Kế Hoạch Kiểm Thử & Tiêu Chí Nghiệm Thu (Verification)

1. **Unit Tests (Backend):**
   - Kiểm tra toàn bộ quy tắc chuyển trạng thái hợp lệ/bất hợp lệ của `TaskItem.MoveTo`.
   - Kiểm tra ràng buộc bảo vệ: Agent không được phép tự gọi `Approve`.
   - Kiểm tra tính toán `TaskCode` tự tăng (`TSK-001`, `TSK-002`...).
2. **Integration Tests (Backend):**
   - Đăng ký Hangfire Job khi giao việc cho Agent.
   - Truy vấn Cross-module lấy Member & AgentProfile qua MediatR.
   - EF Core Migration trên schema `task` và bảng `__EFMigrationsHistory_Tasks`.
3. **E2E Playwright Tests (Frontend & Realtime):**
   - Mở giao diện Kênh `#tech-research`, chuyển qua tab Bảng Kanban.
   - Tạo mới 1 task "Khảo sát thị trường" gán cho Agent `ResearchBot`.
   - Xác minh task hiển thị ở cột `To Do` $\rightarrow$ chuyển sang `In Progress`.
   - Mô phỏng Hangfire hoàn thành $\rightarrow$ thẻ tự động nhảy sang cột `In Review` kèm file artifact `report.md`.
   - Con người bấm `Approve` $\rightarrow$ thẻ nhảy sang cột `Done`.

---

## 6. Kế Hoạch Triển Khai Theo Từng Bước (Implementation Phases)

1. **Phase 1: Foundation & Scaffold Module Tasks**
   - Scaffold 4 projects trong `backend/src/Modules/Tasks/`.
   - Cấu hình EF Core DbContext trên schema `task`, tạo Migration đầu tiên.
2. **Phase 2: Domain Logic & CQRS Commands/Queries**
   - Viết Domain Entities, State Machine, Review Gate rules.
   - Viết CQRS Handlers cho Create, MoveStatus, Assign, Review, AddArtifact.
3. **Phase 3: Hangfire Background Job & SignalR TasksHub**
   - Triển khai `TaskAgentExecutionJob` chạy ngầm qua Hangfire MongoDB.
   - Triển khai `SignalRTasksHub` tại `/hubs/tasks` và Controllers REST API v1.
4. **Phase 4: Angular Kanban Board UI & Realtime Integration**
   - Cài đặt `@angular/cdk/drag-drop` và xây dựng Store, Models, HTTP & SignalR Services.
   - Dựng giao diện Kanban Board, Review Gate Modal theo phong cách Minimalist UI.
5. **Phase 5: Kiểm Thử Toàn Diện & Verification**
   - Viết Unit Tests, Integration Tests và Playwright E2E Test Suite.
   - Rà soát toàn bộ code theo `/review` đối chiếu 11 bộ quy tắc trong `.agents/rules/`.

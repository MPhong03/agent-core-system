# AgentCoreSystem: The Self-Hosted AI Workforce Platform
## Tài liệu Pitching & Kick-Off Dự án (Project Design & Execution Spec)

- **Tác giả / Sáng lập:** Phong Minh Đặng
- **Ngày cập nhật:** 18/11/2026
- **Trạng thái:** Bản hoàn thiện chờ phê duyệt (Final Draft for Approval)
- **Mô hình triển khai:** Single-Tenant / Self-Hosted Private Instance (Dành cho Cá nhân & Doanh nghiệp vừa và nhỏ - SMEs)
- **Kiến trúc kỹ thuật:** .NET 9 Modular Monolith & Angular Standalone (Signals, OnPush)

---

# PHẦN 1: BẢN PITCHING DỰ ÁN (PITCH DECK & STRATEGY)

## 1. Tầm nhìn Sản phẩm (Product Vision)
**AgentCoreSystem** là một **Nền tảng Môi trường làm việc AI Tự chủ & Riêng tư (Self-Hosted AI Workforce Platform)**. 

Thay vì ép buộc người dùng vào một phần mềm nghiệp vụ đóng khung, AgentCoreSystem cung cấp một hệ điều hành cộng tác nơi người dùng (Quản lý / Founder) có thể tự do thành lập và điều phối các **Biệt đội AI (AI Squads)** đa năng theo nhu cầu: từ Nghiên cứu công nghệ, Check & xử lý email, Thu thập dữ liệu web, cho đến Tự động hóa vận hành.

Tất cả diễn ra trong một giao diện quen thuộc (Chat + Kanban) trên hạ tầng riêng của doanh nghiệp, đảm bảo dữ liệu nhạy cảm không bao giờ bị đưa lên cloud của bên thứ ba.

## 2. Bối cảnh Thị trường & Đối thủ Thực tế (Realistic Market Landscape)
Thị trường AI Automation hiện nay phân hóa thành 3 nhóm đối thủ rõ rệt:
1. **Nhóm Nối dây / Workflow-centric (Dify, Flowise, n8n):** Rất mạnh về logic nhưng giao diện dạng kéo-thả khối (node-based) dành riêng cho kỹ sư. Nhân viên văn phòng, quản lý không thể tiếp cận và sử dụng tự nhiên hàng ngày.
2. **Nhóm AI Employee trên Cloud (Lindy.ai, Relevance AI):** Giao diện thân thiện nhưng đóng kín (SaaS Cloud), chi phí thuê bao rất đắt đỏ (hàng trăm $/tháng) và buộc doanh nghiệp phải đẩy toàn bộ dữ liệu nội bộ (email, tài chính, khách hàng) lên máy chủ của họ.
3. **Nhóm Khổng lồ (Microsoft Copilot Studio, Salesforce Agentforce):** Chi phí bản quyền trên trời, phức tạp và bị khóa chặt (vendor lock-in) vào hệ sinh thái riêng.

👉 **Khoảng trống Độc tôn của AgentCoreSystem:**
> **"Nền tảng Điều hành Biệt đội AI Tự chủ & Riêng tư (Private AI Workforce Hub)"**
> - **Self-Hosted / Single-Tenant:** Tự cài đặt trên Server/VPS riêng hoặc On-Premise, toàn quyền kiểm soát dữ liệu 100%.
> - **Giao tiếp Tự nhiên (Chat & Kanban):** Điều hành qua hội thoại nhóm và bảng công việc, không cần biết nối dây kỹ thuật.
> - **Khả năng Tùy biến Không giới hạn:** Người dùng tự do thiết lập bao nhiêu nhóm, bao nhiêu agent tùy ý cho bất kỳ nghiệp vụ nào.

## 3. Triết lý Thiết kế Cốt lõi (Core Product Principles)
1. **Nhân sự AI Độc lập (Autonomous Agent Anatomy):**
   - Mỗi Agent có **Bộ não riêng** (Model LLM tùy chọn: Gemini, OpenAI, Claude, Local LLM qua Ollama).
   - Có **System Prompt** định hình tính cách, vai trò chuyên môn.
   - Có **Bộ nhớ riêng (Private Memory)** lưu trữ thói quen, bài học kinh nghiệm cá nhân.
   - Được phân quyền sử dụng chính xác những **Công cụ (Tools)** được chỉ định.
2. **Ngữ cảnh Hợp tác Nhóm (Shared Group Memory):**
   - Khi tham gia vào một Nhóm/Kênh làm việc, Agent chia sẻ và sử dụng **Bộ nhớ chung của Nhóm** (tài liệu, lịch sử trao đổi, quyết định chung).
3. **Hệ sinh thái Công cụ No-Code / Low-Code:**
   - Người dùng tự cấu hình REST API Connectors trên giao diện (URL, Method, Headers, Schema) để mở rộng năng lực cho Agent (đọc email, cào web, gọi API bên ngoài) mà không cần lập trình lại backend.
4. **Không gian làm việc & Cách ly Thực thi (Python Workspace & Isolated venv):**
   - Mỗi Agent được hệ thống cấp một thư mục làm việc riêng biệt: `Storage/Workspaces/{AgentId}/`.
   - Agent được quyền tự sinh mã Python và thực thi script để giải quyết các bài toán phức tạp (tính toán, xử lý file, phân tích số liệu).
   - **Tự trị môi trường phụ thuộc:** Nếu cần cài thêm thư viện (`pip install`), Agent tự khởi tạo và sử dụng **Virtual Environment (`.venv`) riêng biệt** ngay trong workspace của nó, không làm ảnh hưởng hay xung đột với môi trường máy chủ.

## 4. Mô hình Thương mại hóa (Business Model)
- **Bản quyền Self-Hosted / On-Premise:** Bán gói phần mềm cài đặt trọn gói (One-time license hoặc Annual maintenance) cho các doanh nghiệp vừa và nhỏ (SMEs).
- **Dịch vụ Managed Private Cloud:** Cung cấp dịch vụ triển khai và quản trị hạ tầng VPS riêng biệt cho khách hàng không chuyên kỹ thuật.
- **Thư viện Mẫu (Templates & Connectors Store):** Cung cấp các bộ cấu hình nhóm mẫu dựng sẵn (Squad Recipes: Nhóm Lọc & Xử lý Mail, Nhóm Cào dữ liệu, Nhóm Nghiên cứu thị trường) và các No-code API Connectors.

---

# PHẦN 2: ĐẶC TẢ THIẾT KẾ KỸ THUẬT (TECHNICAL DESIGN SPEC)

## 1. Ràng buộc Kiến trúc & Tiêu chuẩn Hệ thống (Architecture Constraints)
- **Kiến trúc Lõi:** .NET 9 Modular Monolith, Clean Architecture.
- **Mô hình Khách hàng:** **Single-Tenant** (Một tổ chức/công ty trên mỗi bản cài đặt, tối ưu hóa cho mối quan hệ giữa Nhân viên con người và các Nhóm Agent).
- **Cơ sở dữ liệu:** SQL Server với chiến lược **Schema-per-module** (`org`, `chat`, `task`, `agent`). Mọi entity kế thừa `BaseEntity` với khóa UUIDv7 (`Guid.CreateVersion7()`).
- **Xử lý nền & Realtime:** Hangfire (MongoDB storage) cho Background Tasks; SignalR cho Real-time Chat và Kanban updates.
- **Frontend:** Angular 19+ Standalone Components, Signal state management, OnPush change detection.
- **Quy ước C#:** Block-scoped namespaces, Primary Constructors, Result Pattern (`Result<T>`, `Error`), RFC 7807 ProblemDetails.

## 2. Chi tiết 4 Phân hệ Nghiệp vụ (Module Architecture)

### 2.1. Module `Organization` (Schema: `org`)
- **Entities:**
  - `Department`: Phòng ban / Khối chức năng (Kỹ thuật, Vận hành, Nghiên cứu...).
  - `TeamChannel`: Kênh làm việc theo nhóm (ví dụ: `#tech-research`, `#email-processing`, `#data-crawler`).
  - `Member`: Đại diện thành viên trong kênh (`MemberType`: `Human` | `Agent`).
  - `AgentProfile`: Danh tính Agent (Tên, Avatar, ModelProvider, ModelName, SystemPrompt, PrivateMemoryNotes).

### 2.2. Module `Chat` (Schema: `chat`)
- **Entities:**
  - `ChatMessage`: Nội dung, thời gian gửi, ChannelId, SenderMemberId.
  - `MessageMention`: Bảng liên kết lưu các thành viên được tag `@mention`.
- **Dịch vụ Lõi:**
  - `SignalRChatHub`: Đẩy tin nhắn tức thời tới Angular client.
  - `MessageDispatcherService`:
    - Chỉ kích hoạt LLM cho Agent khi có gắn thẻ `@AgentName` hoặc `@all`.
    - Các Agent khác trong kênh giữ trạng thái **Passive Listening** (chỉ tải Sliding Window 20 tin nhắn gần nhất khi được kích hoạt sau này).
    - Kiểm soát độ sâu gọi chéo: `MaxCallDepth = 3` kèm phát hiện vòng lặp để triệt tiêu chat loop.

### 2.3. Module `Tasks` (Schema: `task`)
- **Entities:**
  - `TaskItem`: Mã task, Tiêu đề, Mô tả, Trạng thái (`ToDo`, `InProgress`, `InReview`, `Done`, `Failed`), Priority, ChannelId.
  - `TaskAssignment`: Giao việc cho Agent hoặc Human chịu trách nhiệm.
  - `TaskArtifact`: Sản phẩm đầu ra (file kết quả, báo cáo, dữ liệu trích xuất).
- **Vận hành:** Tác vụ được thực thi ngầm qua Hangfire; tự động cập nhật trạng thái lên bảng Kanban qua SignalR.

### 2.4. Module `Agents` (Schema: `agent`)
- **Entities:**
  - `ToolDefinition`: Tên tool, Type (`RestApi`), EndpointUrl, HttpMethod, HeadersConfig, ParametersSchema (JSON Schema chuẩn LLM Tool Calling).
  - `AgentToolAccess`: Phân quyền Agent nào được dùng Tool nào.
- **Thành phần Kỹ thuật Lõi:**
  - `ILLMProviderFactory`: Adapter kết nối linh hoạt tới Gemini API, OpenAI API, Claude API, Ollama.
  - `RestApiToolExecutor`: Trình gọi API động theo cấu hình No-Code và tham số do LLM sinh ra.
  - `WorkspaceManager`: Quản lý thư mục ổ đĩa vật lý `Storage/Workspaces/{AgentId}/`.
  - `PythonVenvRunner`:
    - Tự động kiểm tra/tạo `.venv` trong workspace của Agent (`python -m venv .venv`).
    - Thực thi script Python với timeout cứng (ví dụ: 60s), cô lập biến môi trường, bắt log stdout/stderr.

## 3. Luồng Nghiệp vụ Con người & Agent (Human-in-the-Loop)
1. **Giao việc & Kích hoạt:** Quản lý giao việc trên Kênh chat qua cú pháp `@AgentA ...` hoặc tạo trực tiếp 1 Task trên bảng Kanban.
2. **Nghiệm thu (Review Gate):** Agent làm xong đưa Task vào cột `InReview`. Quản lý xem kết quả -> bấm **Approve** (Duyệt) hoặc **Request Revision** (Yêu cầu làm lại kèm nhận xét).
3. **Đào tạo & Phản hồi (Feedback Loop):** Nhận xét sau nghiệm thu được trích xuất lưu vào `PrivateMemory` của Agent để Agent cải thiện cho các task sau.
4. **Cứu nguy (Escalation):** Khi gặp bế tắc (lỗi API, thiếu dữ liệu, rủi ro), Agent chủ động tag `@Manager` trong chat để xin chỉ đạo.

---

# PHẦN 3: LỘ TRÌNH TRIỂN KHAI THEO SPRINTS (SPRINT ROADMAP)

- **Sprint 0: Nền tảng Tổ chức & Quản lý Agent (Org & Identity)**
  - Dựng `Module.Organization` (Domain, Application, Infrastructure, Presentation, schema `org`).
  - Giao diện Angular: Quản lý cấu trúc Phòng ban, Kênh nhóm; Cấu hình hồ sơ Agent (chọn model, nhập prompt, mô tả vai trò).
- **Sprint 1: Kênh Chat Thời Gian Thực & Bộ Lọc Kích Hoạt (@Mention Router)**
  - Dựng `Module.Chat` (schema `chat`), tích hợp SignalR Realtime Hub.
  - Xây dựng `MessageDispatcherService` nhận diện `@mention`, cơ chế Passive Listening qua Sliding Window.
  - Giao diện Angular: Chat Room tương tác thời gian thực, autocomplete khi gõ `@`.
- **Sprint 2: Bảng Kanban & Tác vụ Chạy Ngầm (Tasks & Background Jobs)**
  - Dựng `Module.Tasks` (schema `task`), quản lý vòng đời TaskItem (To Do -> In Progress -> In Review -> Done).
  - Tích hợp Hangfire Job thực thi task không đồng bộ; đẩy cập nhật Kanban qua SignalR.
  - Giao diện Angular: Bảng Kanban song hành cùng Kênh Chat.
- **Sprint 3: No-Code Tool Builder & LLM Function Calling Engine**
  - Dựng `Module.Agents` (Phần 1, schema `agent`): `ToolDefinition`, `AgentToolAccess`.
  - Xây dựng `RestApiToolExecutor` và kết nối Function Calling với LLM Provider.
  - Giao diện Angular: Màn hình tạo No-Code Tool (nhập URL, method, headers, schema).
- **Sprint 4: Agent Python Workspace & Môi trường Cách ly (.venv Sandbox)** [HOÀN THÀNH]
  - Dựng `Module.Agents` (Phần 2): `WorkspaceManager` và `PythonVenvRunner`.
  - Tạo workspace riêng cho từng Agent, hỗ trợ tự sinh script và chạy trong `.venv` độc lập.
  - Giao diện Angular: Quản lý Workspace của Agent, xem kết quả đầu ra (Artifacts).
- **Sprint 5: Tri thức & Bộ nhớ Dài hạn (Long-Term Memory & Knowledge / KAG & RAG)** [ĐANG TRIỂN KHAI]
  - Dựng `Module.Knowledge` (schema `knowledge`): `KnowledgeDocument`, `DocumentChunk`, `AgentMemoryNote`.
  - In-process / Local Vector Store với Cosine Similarity, tích hợp Embedding qua 9router / Ollama.
  - Giao diện Angular: Upload & quản lý tài liệu theo Kênh / Agent, xem danh sách tri thức được kích hoạt trong hội thoại.
- **Sprint 6: Nghiệm thu & Giám sát Tác vụ (Human-in-the-Loop & Feedback Loop)**
  - Vòng đời nghiệm thu TaskItem: Review Gate (Approve / Request Revision kèm nhận xét).
  - Tự động nạp nhận xét nghiệm thu vào Private Memory của Agent để cải thiện hiệu suất.
  - Cơ chế Escalation: Agent chủ động tag `@Manager` khi gặp sự cố, lỗi API hoặc bế tắc.
- **Sprint 7: Mẫu Biệt đội & Chợ Công cụ (Squad Recipes & Connectors Catalog)**
  - Quản lý Squad Templates: Đóng gói và Import / Export cấu hình Biệt đội (Department, Channel, Agents, Prompts, Tools).
  - Thư viện mẫu dựng sẵn: Inbox Zero Squad, Tech Research Squad, Web Scraper Squad.
- **Sprint 8: Đóng gói Triển khai & Tự lưu trữ (Production Packaging & Self-Hosted Installer)**
  - Containerization toàn diện qua Docker Compose (.NET API Host, Angular Client, SQL Server, MongoDB, Python venv runtime).
  - Script cài đặt 1-click tự động hóa khởi tạo cơ sở dữ liệu và biến môi trường cho môi trường VPS / On-Premise.

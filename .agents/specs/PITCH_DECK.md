# PITCH DECK: AGENT CORESYSTEM
## The Self-Hosted AI Workforce Platform

> **Định vị:** Nền tảng điều hành các Biệt đội AI Tự chủ & Riêng tư dành cho Cá nhân và Doanh nghiệp vừa & nhỏ (SMEs).  
> **Sáng lập & Kiến trúc sư trưởng:** Phong Minh Đặng  
> **Ngày lập:** 18/11/2026

---

## SLIDE 1: TỔNG QUAN & ĐỊNH VỊ (EXECUTIVE SUMMARY)

```text
       ┌────────────────────────────────────────────────────────┐
       │                   AGENT CORESYSTEM                     │
       │         The Self-Hosted AI Workforce Platform          │
       ├────────────────────────────────────────────────────────┤
       │  "Biến mỗi nhân viên thành một Đội trưởng điều hành    │
       │   các Biệt đội AI chuyên môn hóa trên hạ tầng riêng"   │
       └────────────────────────────────────────────────────────┘
```

- **Vấn đề cốt lõi:** Các công cụ AI hiện nay hoặc là chatbot hỏi-đáp rời rạc, hoặc là hệ thống đám mây đắt đỏ đòi hỏi doanh nghiệp phải tải dữ liệu nhạy cảm lên server bên thứ ba, hoặc là công cụ nối dây phức tạp dành riêng cho lập trình viên.
- **Giải pháp của chúng tôi:** Một nền tảng tự lưu trữ (Self-Hosted), nơi người dùng tự do thành lập các **Biệt đội AI (AI Squads)** đa năng (nghiên cứu công nghệ, xử lý email, cào dữ liệu...), điều hành qua giao diện quen thuộc (**Chat + Kanban**), với hệ sinh thái công cụ **No-Code** và môi trường chạy **Python `.venv` cách ly**.

---

## SLIDE 2: NỖI ĐAU THỊ TRƯỜNG (THE PAIN POINTS)

| Nỗi đau | Thực trạng hiện nay | Hậu quả |
| :--- | :--- | :--- |
| **1. Mối lo Rò rỉ Dữ liệu** | Dùng AI Cloud (ChatGPT, Claude, Lindy) buộc phải đưa email, tài liệu tài chính, bí mật kinh doanh lên cloud ngoài. | Doanh nghiệp ngần ngại ứng dụng AI cho các tác vụ quan trọng vì rủi ro lộ bí mật kinh doanh. |
| **2. Rào cản Kỹ thuật** | Các nền tảng tự động hóa mạnh (n8n, Dify, Flowise) yêu cầu kéo-thả node, nối dây logic phức tạp. | Chỉ kỹ sư phần mềm mới cấu hình được; quản lý, nhân viên văn phòng bị bỏ lại phía sau. |
| **3. Thiếu Tính Tổ chức & Quản lý** | Chatbot 1-1 không giải quyết được chuỗi công việc dài hạn, không có trạng thái giao việc, không theo dõi tiến độ. | Người dùng phải ngồi chờ màn hình, copy-paste kết quả thủ công giữa các công cụ. |
| **4. Chi phí Đắt đỏ & Khóa chặt** | Thuê AI Employee đám mây tốn hàng trăm USD/tháng/bot; giải pháp tập đoàn (Copilot Studio, Agentforce) giá trên trời. | SMEs không đủ ngân sách tiếp cận, bị khóa chặt vào hệ sinh thái của một nhà cung cấp lớn. |

---

## SLIDE 3: MA TRẬN PHÂN TÍCH ĐỐI THỦ (COMPETITIVE MATRIX)

```text
  Riêng tư / Tự chủ (Self-Hosted)
                ▲
                │
                │        ★ AGENT CORESYSTEM
                │        (Trải nghiệm Chat/Kanban + Self-Hosted + Python .venv)
   n8n / Dify   │
   (Nối dây)    │
                │
◄───────────────┼────────────────────────► Trải nghiệm Người dùng
Kéo-thả Node   │                         (Giao tiếp tự nhiên / Chat & Kanban)
(Dành cho Dev)  │
                │        Lindy.ai / Relevance AI
                │        (Cloud SaaS đắt đỏ, không làm chủ dữ liệu)
                │
                │        Copilot Studio / Agentforce
                │        (Doanh nghiệp lớn, giá cực cao)
                ▼
  Phụ thuộc Cloud bên ngoài
```

**Lợi thế cạnh tranh của AgentCoreSystem:**
1. **Làm chủ dữ liệu 100%:** Cài đặt On-Premise hoặc Private VPS của khách hàng.
2. **Trải nghiệm Nhân sự số:** Giao tiếp qua Kênh chat nhóm (`@mention`), theo dõi qua Kanban.
3. **Mở rộng không giới hạn:** Tự tạo công cụ bằng No-Code REST API, không giới hạn số lượng Agent.

---

## SLIDE 4: GIẢI PHÁP & TRIẾT LÝ NỀN TẢNG (THE SOLUTION)

Chúng tôi không cung cấp một "phần mềm kế toán" hay "phần mềm viết content" đóng khung. Chúng tôi cung cấp **Môi trường vận hành Lực lượng lao động AI (Workforce Operating Environment)**:

1. **Nhân sự AI Độc lập (Autonomous Agent):**
   - **Brain:** Tùy chọn Model cho từng Agent (Gemini Flash giá rẻ cho việc đọc mail; Gemini Pro/Claude Sonnet cho việc nghiên cứu logic sâu).
   - **System Prompt & Role:** Định hình chuyên môn, tính cách, KPI.
   - **Private Memory:** Bộ nhớ riêng lưu thói quen và lời dặn của Quản lý sau mỗi lần nghiệm thu.
   - **Tool RBAC:** Phân quyền chặt chẽ: Agent nào chỉ được dùng công cụ đó.
2. **Ngữ cảnh Nhóm (Shared Group Memory):**
   - Agent tham gia nhóm nào thì dùng chung tài liệu, lịch sử hội thoại của nhóm đó.
3. **Kích hoạt có định hướng (Targeted Dispatcher):**
   - Chỉ Agent được tag `@AgentName` mới bật LLM làm việc. Các Agent khác giữ trạng thái "Lắng nghe thụ động" (Passive Listening) -> **Triệt tiêu 100% bệnh nói leo và vòng lặp tốn token**.

---

## SLIDE 5: TRẢI NGHIỆM SẢN PHẨM (PRODUCT EXPERIENCE)

### Giao diện Song hành: Chat Room & Kanban Task Board

```text
┌───────────────────────────────────────┬──────────────────────────────────────────┐
│  KÊNH: #tech-research                 │  BẢNG KANBAN TIẾN ĐỘ                     │
├───────────────────────────────────────┼──────────────────────────────────────────┤
│ [Quản lý (Bạn)]: @AgentCào @AgentPhân │ [TO DO]                                  │
│ tích, khảo sát kiến trúc .NET 9 mới   │  - Khảo sát Angular Signals              │
│ nhất và so sánh hiệu năng.            │                                          │
│                                       │ [IN PROGRESS] ⚙️                         │
│ [AgentCào]: Nhận lệnh. Tôi đang tạo   │  - Task #88: Khảo sát .NET 9             │
│ Task #88 và dùng tool cào docs...     │    (@AgentCào, @AgentPhân tích)          │
│                                       │    [Tool: Web_Scraper running...]        │
│ [AgentPhân tích]: Tôi đã nhận data    │                                          │
│ từ @AgentCào, đang chạy script        │ [IN REVIEW] 🔍 (Chờ bạn duyệt)           │
│ tổng hợp số liệu trong .venv...       │  - Task #87: Lọc 100 email chào hàng     │
│                                       │                                          │
│ [AgentPhân tích]: @Quản lý, báo cáo   │ [DONE] ✅                                │
│ đã hoàn thành, mời sếp nghiệm thu!    │  - Task #86: Tóm tắt tin công nghệ tuần  │
└───────────────────────────────────────┴──────────────────────────────────────────┘
```

- **Tác vụ nền không gián đoạn:** Agent làm việc qua Hangfire ngầm, người dùng không phải đợi màn hình.
- **Nghiệm thu (Review Gate):** Quản lý duyệt (Approve) hoặc yêu cầu chỉnh sửa (Request Revision).

---

## SLIDE 6: KHẢ NĂNG THỰC THI & CÁCH LY MÔI TRƯỜNG (EXECUTION & SANDBOX)

```text
┌────────────────────────────────────────────────────────────────────────┐
│  HỆ SINH THÁI CÔNG CỤ (DYNAMIC TOOLING & ISOLATED SANDBOX)             │
│                                                                        │
│  1. NO-CODE REST API BUILDER (90% Tác vụ thông thường)                 │
│     ├── Cấu hình trên Web UI: URL, Method, Headers, Request Schema     │
│     └── Tự động sinh Function Calling cho LLM (An toàn tuyệt đối)      │
│                                                                        │
│  2. PYTHON WORKSPACE & ISOLATED .VENV (Tác vụ tính toán phức tạp)      │
│     ├── Thư mục riêng: `Storage/Workspaces/{AgentId}/`                 │
│     ├── Agent tự sinh script Python (phân tích, trích xuất dữ liệu)    │
│     └── TỰ LẬP VIRTUAL ENVIRONMENT (.venv):                            │
│         - Nếu cần thư viện ngoài (pandas, beautifulsoup4...):          │
│           Agent tự chạy `python -m venv .venv` và `pip install` nội bộ │
│         - KHÔNG làm bẩn hoặc xung đột môi trường Python máy chủ!       │
└────────────────────────────────────────────────────────────────────────┘
```

---

## SLIDE 7: CÁC TRƯỜNG HỢP SỬ DỤNG MẪU (SQUAD RECIPES / USE CASES)

Người dùng có thể tự do biến hóa hệ thống cho bất kỳ nhu cầu nào:

### 1. Biệt đội Xử lý Email (Inbox Zero Squad)
- **Cấu hình:** 1 Agent Quét thư + 1 Agent Phân loại & Tóm tắt + 1 Agent Soạn nháp phản hồi.
- **Công cụ:** No-Code REST API kết nối Gmail / Outlook IMAP.
- **Kết quả:** Mỗi sáng tổng hợp 1 bảng Kanban các email khẩn cấp cần sếp ký/trả lời, kèm sẵn văn bản trả lời nháp.

### 2. Biệt đội Nghiên cứu & Cào Dữ liệu (Tech & Market Research Squad)
- **Cấu hình:** Agent Thu thập (Cào bài viết, tài liệu kỹ thuật, GitHub repos) + Agent Phân tích & Viết tổng hợp.
- **Công cụ:** Web Scraper REST API + Python script chạy trong `.venv` để parse dữ liệu và vẽ biểu đồ.
- **Kết quả:** Bản báo cáo nghiên cứu hoàn chỉnh đính kèm file Markdown/PDF trên bảng Kanban.

### 3. Biệt đội Vận hành Doanh nghiệp (Operations Squad)
- **Cấu hình:** Agent Đọc chứng từ + Agent So khớp dữ liệu CRM/ERP + Agent Lập báo cáo sai lệch.
- **Kết quả:** Tự động phát hiện chênh lệch số liệu và đẩy vào cột `InReview` chờ Quản lý duyệt.

---

## SLIDE 8: KIẾN TRÚC KỸ THUẬT (ENTERPRISE-GRADE ARCHITECTURE)

Được xây dựng trên nền tảng kỹ thuật chuẩn mực của một Kỹ sư Hệ thống:

```text
┌────────────────────────────────────────────────────────────────────────┐
│ FRONTEND: Angular 19+ (Standalone, Signals, OnPush Change Detection)   │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ REST API v1 + SignalR Realtime Hub
┌───────────────────────────────────▼────────────────────────────────────┐
│ BACKEND: .NET 9 Modular Monolith (Clean Architecture per Module)       │
│ ├─ Module.Organization (Schema: org)  : Company, Departments, Channels │
│ ├─ Module.Chat         (Schema: chat) : Messages, Mentions, Dispatcher │
│ ├─ Module.Tasks        (Schema: task) : Kanban, Assignments, Artifacts │
│ └─ Module.Agents       (Schema: agent): LLM Engine, Tools, .venv Runner│
├────────────────────────────────────────────────────────────────────────┤
│ SHARED INFRASTRUCTURE & STORAGE                                        │
│ ├─ SQL Server (Schema-per-module, UUIDv7 clustered index)              │
│ ├─ MongoDB (Hangfire Durable Background Processing)                    │
│ └─ Agent Workspaces (Thư mục ổ đĩa vật lý cô lập theo từng AgentId)   │
└────────────────────────────────────────────────────────────────────────┘
```

---

## SLIDE 9: MÔ HÌNH THƯƠNG MẠI HÓA (BUSINESS MODEL)

```text
┌───────────────────────────┬───────────────────────────┬───────────────────────────┐
│     ON-PREMISE LICENSE    │    MANAGED PRIVATE VPS    │    SQUAD RECIPES STORE    │
├───────────────────────────┼───────────────────────────┼───────────────────────────┤
│ Dành cho SMEs tự host:    │ Dành cho khách hàng       │ Chợ Mẫu & Tích hợp:       │
│ - Bán License vĩnh viễn   │ không chuyên kỹ thuật:    │ - Bán các gói cấu hình    │
│   kèm gói bảo trì năm.    │ - Thu phí duy trì server  │   sẵn (Email, Scraping,   │
│ - Không giới hạn số Agent │   và cài đặt hàng tháng.  │   Finance, Dev team).     │
│   và Task chạy nội bộ.    │ - Khách hàng tự nạp API   │ - Bộ No-Code API          │
│                           │   Key hoặc trọn gói.      │   Connectors dựng sẵn.    │
└───────────────────────────┴───────────────────────────┴───────────────────────────┘
```

---

## SLIDE 10: LỘ TRÌNH 8 SPRINTS TRIỂN KHAI (EXPANDED ROADMAP)

```text
GIAI ĐOẠN 1: KHUNG NỀN TẢNG MVP (ĐÃ HOÀN THÀNH 100%)
[Sprint 0] ──► [Sprint 1] ──► [Sprint 2] ──► [Sprint 3] ──► [Sprint 4]
 Tổ chức &       Kênh Chat      Bảng Kanban     No-Code Tool     Python .venv
 Danh tính       & Realtime     & Hangfire      Builder & LLM     Workspace &
   Agent        Dispatcher        Tasks       Function Calling     Sandbox
 (Schema org)   (Schema chat)  (Schema task)   (Schema agent)   (Execution)

GIAI ĐOẠN 2: TRI THỨC, QUẢN TRỊ & THƯƠNG MẠI HÓA (ĐANG TRIỂN KHAI)
[Sprint 5] ───────────────► [Sprint 6] ───────────────► [Sprint 7] ───────────────► [Sprint 8]
 Tri thức & KAG/RAG          Nghiệm thu & Duyệt          Mẫu Biệt đội dựng sẵn        Đóng gói Docker
 (Long-Term Memory)          (Human-in-the-Loop)         (Squad Recipes Store)        & Self-Hosted Ship
```

- **Giai đoạn 1 (Sprint 0 - 4 - Đã hoàn thành):** Xây dựng bộ khung nền tảng khép kín: Tổ chức -> Chat realtime -> Kanban background tasks -> No-Code Tool Calling -> Python .venv Sandbox.
- **Giai đoạn 2 (Sprint 5 - 8 - Mở rộng):** Nâng cấp Agent thành nhân sự số chuyên nghiệp: Đọc hiểu tài liệu nội bộ (KAG/RAG), cơ chế phê duyệt nghiệm thu tin cậy (Human-in-the-Loop), kho mẫu Biệt đội dựng sẵn (Squad Recipes) và đóng gói cài đặt trọn gói bằng Docker 1-click.

---

```text
               "AgentCoreSystem - Your Private Digital Workforce"
         Xây dựng nền tảng vững chắc để biến AI thành đồng đội thực sự.
```

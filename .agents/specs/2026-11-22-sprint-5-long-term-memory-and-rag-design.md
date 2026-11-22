# Design Document: Sprint 5 - Long-Term Memory, Knowledge Base & KAG/RAG Engine

- **Tác giả:** Phong Minh Đặng / Antigravity
- **Ngày tạo:** 22/11/2026
- **Trạng thái:** Bản hoàn thiện chờ phê duyệt (Final Draft for Approval)
- **Dự án:** AgentCoreSystem (.NET 9 Modular Monolith & Angular Standalone)
- **Tài liệu gốc tham chiếu:** [.agents/specs/2026-11-18-agent-core-system-design.md](file:///c:/Workplaces/PROJECT/AgentCoreSystem/.agents/specs/2026-11-18-agent-core-system-design.md) & [PITCH_DECK.md](file:///c:/Workplaces/PROJECT/AgentCoreSystem/.agents/specs/PITCH_DECK.md)

---

## 1. Mục Đích & Bối Cảnh (Intent & The Why)

Sau khi hoàn thành 5 Sprint cốt lõi (Sprint 0 đến Sprint 4):
- Hệ thống đã vận hành trơn tru chu trình giao tiếp thời gian thực (Chat SignalR), quản lý tiến độ ngầm (Tasks Kanban & Hangfire), gọi công cụ REST API động (Function Calling), và tính toán/thực thi code an toàn trong môi trường ảo (.venv Sandbox).
- Các thử nghiệm thực tế với **9router Gateway** và **Gemini 3.8 Low** cho thấy Biệt đội 5 Agent có thể phối hợp nhịp nhàng để giải quyết các bài toán nghiên cứu chuyên sâu.

Tuy nhiên, hệ thống đang bộc lộ điểm nghẽn lớn nhất của một **Nhân sự AI chuyên nghiệp**:
1. **Hiện tượng "Mất trí nhớ ngắn hạn" (Amnesia):** Agent chỉ tiếp nhận ngữ cảnh trong phạm vi Sliding Window 20 tin nhắn gần nhất. Sau khi kết thúc phiên làm việc, Agent không lưu giữ được bất kỳ bài học kinh nghiệm, quy tắc ngầm hay sở thích làm việc nào đã được người dùng chỉ dạy.
2. **Thiếu hiểu biết về Tri thức Nội bộ (Document Grounding):** Khi người dùng giao các tác vụ đặc thù (nghiên cứu chính sách nội bộ, đối chiếu hợp đồng, tra cứu bảng giá, đọc tài liệu đặc tả API riêng), Agent hoàn toàn mù tịt vì không có cơ chế nạp và tra cứu tài liệu (PDF, Markdown, TXT).
3. **Bài toán KAG / RAG cho Self-Hosted On-Premise:** Doanh nghiệp không muốn đẩy tài liệu nội bộ lên các dịch vụ Cloud Vector của bên thứ ba (Pinecone, OpenAI Vector Store). Hệ thống cần một giải pháp **Local-First / In-Process Vector Search** nhẹ nhàng, bảo mật 100%, không đòi hỏi dựng thêm hạ tầng cụm máy chủ cồng kềnh.

Sprint 5 sẽ giải quyết triệt để bài toán trên bằng cách xây dựng **Module Knowledge** mới (schema `knowledge`), cung cấp 2 tầng bộ nhớ: **Shared Channel Knowledge (Tri thức dùng chung theo Kênh)** và **Private Agent Memory (Ký ức cá nhân & Bài học kinh nghiệm của Agent)**.

---

## 2. Mục Tiêu, Phi Mục Tiêu & Chống Mục Tiêu (Goals, Non-Goals & Anti-Goals)

### 2.1. Goals (Mục tiêu bắt buộc)

- **Kiến trúc Modular Monolith - Module `Knowledge` mới:**
  - Tạo project mới độc lập: `AgentCoreSystem.Modules.Knowledge` (Domain, Application, Infrastructure, Presentation) với schema cơ sở dữ liệu `knowledge`.
  - Quản lý EF Core Migrations riêng (`__EFMigrationsHistory_Knowledge`) thông qua `justfile`.
- **Thực thể Dữ liệu (Entities) & Ranh giới (Boundaries):**
  - `KnowledgeDocument`: Đại diện tài liệu tải lên (`Id`, `Title`, `FileName`, `ContentType`, `FileSizeBytes`, `Scope` [`Global`|`Channel`|`Agent`], `TargetId` [`Guid?`], `Status` [`Pending`|`Processing`|`Ready`|`Failed`], `ChunkCount`, `ErrorMessage`, `CreatedDate`).
  - `DocumentChunk`: Các phân đoạn văn bản nhỏ (`Id`, `KnowledgeDocumentId`, `ChunkIndex`, `Content`, `TokenCount`, `MetadataJson`, `EmbeddingVectorJson`).
  - `AgentMemoryItem`: Ký ức bài học riêng của Agent (`Id`, `AgentProfileId`, `Category` [`LessonLearned`|`Preference`|`Constraint`|`Fact`], `Content`, `Importance` [1-5], `EmbeddingVectorJson`, `SourceTaskId`, `SourceMessageId`).
- **Xử lý Tài liệu & Cắt đoạn (Document Ingestion & Chunking Pipeline):**
  - Hỗ trợ các định dạng: Markdown (`.md`), Plain Text (`.txt`), và PDF (`.pdf` via lightweight PDF parser/extractor).
  - Thuật toán Recursive Character Chunking với kích thước chunk chuẩn (ví dụ: 500-1000 characters, overlap 100 characters) để bảo toàn ngữ nghĩa trọn vẹn của từng câu văn.
  - Tác vụ cắt đoạn và sinh embedding chạy nền hoàn toàn thông qua **Hangfire Background Job** (`ProcessDocumentJob`) để không làm nghẽn HTTP Request.
- **Hạ tầng Vector Search In-Process & Embedding Provider:**
  - Giao diện `IEmbeddingService`: Hỗ trợ sinh vector từ **9router** (`http://localhost:20128/v1/embeddings`), **Ollama**, hoặc **OpenAI** (hỗ trợ model chuẩn như `text-embedding-004`, `nomic-embed-text`, `text-embedding-3-small`).
  - Trình tìm kiếm `IVectorSearchEngine` / `LocalCosineVectorStore`: Tính toán độ tương đồng Cosine Similarity trực tiếp bằng thuật toán vector hóa in-memory siêu tốc (SIMD-accelerated dot product khi có thể), lọc theo Scope (`ChannelId` hoặc `AgentProfileId`) và xếp hạng Top-K kết quả phù hợp nhất kèm ngưỡng Similarity Threshold (mặc định $\ge 0.65$).
- **Tích hợp Tự động vào Kênh Chat & Điều phối (@Mention Router):**
  - **Tự động bơm ngữ cảnh (Grounding Injection):** Khi tin nhắn người dùng tag `@AgentName`, `MessageDispatcherService` gọi query tri thức liên quan:
    - Lấy top 3-5 chunks từ Shared Channel Knowledge.
    - Lấy top 3-5 ghi chú từ Private Agent Memory.
    - Đóng gói thành các thẻ XML có cấu trúc `<knowledge_context>` và `<agent_private_memory>` tiêm trực tiếp vào System Prompt của Agent trước khi gọi LLM.
  - **Công cụ Chủ động (Agent Tool Calling):** Cung cấp thêm System Tool `search_knowledge_base(query: string, scope?: string)` để Agent có thể chủ động tra cứu sâu thêm kho tri thức khi cần.
- **Frontend Angular 19+ Standalone:**
  - **Tab Kho Tri Thức trong Kênh Chat:** Quản lý tài liệu được ghim cho Kênh, nút Upload file kéo thả (Drag-and-Drop), thanh trạng thái tiến trình (Pending $\rightarrow$ Processing $\rightarrow$ Ready), xem danh sách các chunks được trích xuất.
  - **Tab Ký Ức trong Hồ Sơ Agent (`/agents/:id`):** Hiển thị danh sách các bài học kinh nghiệm (`AgentMemoryItem`), cho phép người dùng thêm thủ công, chỉnh sửa hoặc xóa bỏ ký ức cũ.
  - **Khối trích dẫn nguồn (Source Citations UI) trong Chat:** Khi Agent trả lời dựa trên tài liệu, hiển thị huy hiệu trích dẫn (ví dụ: `[Nguồn: Báo cáo KAG v1 - Chunk #3]`) giúp người dùng kiểm chứng ngay lập tức.

### 2.2. Non-Goals (Không làm trong Sprint này)
- Không dựng thêm container hạ tầng ngoài (như Qdrant, Milvus, Chroma) để giữ bản cài đặt tinh gọn, nhẹ nhàng.
- Không OCR chữ viết tay hoặc scan ảnh mờ trong file PDF (chỉ trích xuất text-layer chuẩn).
- Không làm tính năng tự động suy luận xóa ký ức mâu thuẫn phức tạp (sẽ dành cho chặng sau).

### 2.3. Anti-Goals (Điều tuyệt đối cấm)
- ⛔ **CẤM** gửi tài liệu nội bộ thô lên các dịch vụ đám mây công cộng không được người dùng cấu hình (chỉ gửi text chunks đến Embedding Provider được chỉ định trong Settings).
- ⛔ **CẤM** tính toán Cosine Similarity hoặc Embedding trực tiếp trên luồng chính của HTTP Request làm treo giao diện người dùng. Mọi tác vụ đọc file và sinh vector đều phải qua Hangfire.
- ⛔ **CẤM** hardcode kích thước chiều vector (Vector Dimensions), phải tự động tương thích từ 384, 768 đến 1536 chiều tùy theo model embedding được chọn.

---

## 3. Kiến Trúc Kỹ Thuật Chi Tiết (Technical Architecture)

```text
┌────────────────────────────────────────────────────────────────────────┐
│ FRONTEND: Angular 19+ (Standalone, Signals, OnPush)                    │
│ ├─ Channel Knowledge Tab: Upload PDF/MD, Chunk Explorer, Status Badges │
│ ├─ Agent Memory Tab: Bài học kinh nghiệm cá nhân, CRUD Memory Items    │
│ └─ Chat Citations: Hiển thị trích dẫn nguồn tài liệu tham chiếu        │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ REST API v1
┌───────────────────────────────────▼────────────────────────────────────┐
│ MODULE KNOWLEDGE (Schema: knowledge)                                   │
│ ├─ Domain: KnowledgeDocument, DocumentChunk, AgentMemoryItem           │
│ ├─ Application: Ingestion, Recursive Chunking, Vector Search Queries   │
│ ├─ Infrastructure: KnowledgeDbContext, LocalCosineVectorStore          │
│ └─ Presentation: KnowledgeDocumentsController, AgentMemoriesController │
├───────────────────────────────────┬────────────────────────────────────┤
│ TÍCH HỢP LIÊN MODULE (Via MediatR Queries / Public Interfaces)         │
│ ├─ Module Chat: MessageDispatcherService gọi IKnowledgeService         │
│ ├─ Module Tasks: Tự động lưu nghiệm thu Task vào AgentMemoryItem       │
│ └─ Module Agents: System Tool `search_knowledge_base`                   │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
┌───────────────────────────────────▼────────────────────────────────────┐
│ BACKGROUND PROCESSING & STORAGE                                        │
│ ├─ SQL Server: Schema `knowledge` (Tài liệu, Chunks, Embeddings JSON)  │
│ ├─ Hangfire (MongoDB): `ProcessDocumentJob` (Extract -> Chunk -> Embed)│
│ └─ Embedding Gateway: 9router / Ollama / OpenAI (/v1/embeddings)       │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Kế Hoạch Triển Khai Chi Tiết (Sprint 5 Task Breakdown)

- **Task 5.1:** Khởi tạo Module `Knowledge` (.NET 9 Clean Architecture) & Đăng ký Host Composition.
- **Task 5.2:** Thiết kế Entities & EF Core Migrations (schema `knowledge`).
- **Task 5.3:** Triển khai Text Extractor (MD, TXT, PDF) & Recursive Chunking Pipeline.
- **Task 5.4:** Triển khai `IEmbeddingService` kết nối 9router / Ollama / OpenAI.
- **Task 5.5:** Triển khai `IVectorSearchEngine` (In-Process Cosine Similarity & Top-K Ranking).
- **Task 5.6:** Hangfire Background Processing (`ProcessDocumentJob`) & API Endpoints cho Upload/Documents.
- **Task 5.7:** Quản lý Private Agent Memory (CRUD & Semantic Search).
- **Task 5.8:** Tích hợp Context Injection vào `MessageDispatcherService` & System Tool `search_knowledge_base`.
- **Task 5.9:** Giao diện Angular: Tab Channel Knowledge (Upload & Chunk Explorer) & Tab Agent Memory.
- **Task 5.10:** Kiểm thử Tích hợp Toàn diện (Unit Tests, Integration Tests & End-to-End Chat Grounding Verification).

---

## 5. Tiêu Chuẩn Nghiệm Thu (Acceptance Criteria)

1. Tải lên 1 file tài liệu kỹ thuật (PDF hoặc Markdown) dung lượng bất kỳ vào Kênh Chat, Hangfire xử lý trơn tru thành các chunks và vector tương ứng.
2. Khi người dùng tag `@Agent` hỏi câu hỏi liên quan đến nội dung tài liệu vừa tải, Agent trả lời chính xác thông tin từ tài liệu và đính kèm trích dẫn nguồn.
3. Khi người dùng dặn dò Agent một quy tắc hoặc Agent hoàn thành việc tốt, ký ức được lưu vào Private Memory và được áp dụng lại trong các phiên chat tiếp theo.
4. Mọi test suite Backend và Frontend đều 100% green. Không vi phạm bất kỳ quy tắc nào trong `.agents/rules/`.

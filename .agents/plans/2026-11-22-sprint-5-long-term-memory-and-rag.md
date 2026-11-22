# Sprint 5: Long-Term Memory, Knowledge Base & KAG/RAG Engine Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Xây dựng phân hệ Tri thức & Bộ nhớ Dài hạn (`Module.Knowledge`, schema `knowledge`) cung cấp 2 tầng bộ nhớ: Shared Channel Knowledge (tài liệu PDF, MD, TXT dùng chung cho Kênh) và Private Agent Memory (ký ức kinh nghiệm cá nhân của Agent), tích hợp công cụ cắt đoạn tự động (Recursive Chunking), sinh Vector Embedding qua 9router/Local-first, và công cụ tìm kiếm ngữ nghĩa In-Process Cosine Vector Search để tự động bơm tri thức (Grounding Injection) vào hội thoại đàm thoại của Biệt đội AI.

**Architecture:** Tạo mới phân hệ `AgentCoreSystem.Module.Knowledge` tuân thủ nghiêm ngặt mô hình Modular Monolith Clean Architecture (Domain, Application, Infrastructure, Presentation) với schema cơ sở dữ liệu `knowledge`. Tác vụ trích xuất text và sinh vector embedding được xử lý bất đồng bộ qua Hangfire (`ProcessDocumentJob`). Tìm kiếm ngữ nghĩa được thực thi in-process bằng Cosine Similarity siêu tốc không cần dựng thêm cụm server ngoài. Tích hợp liên module qua MediatR và Public Interfaces với `Module.Chat` và `Module.Agents`. Giao diện Angular 19+ Standalone quản lý tài liệu, chunk explorer và hiển thị trích dẫn nguồn (Source Citations).

**Tech Stack:** .NET 9, EF Core 9, SQL Server (schema `knowledge`), UglyToad.PdfPig (PDF extraction), Hangfire (MongoDB storage), 9router/OpenAI/Ollama Embeddings API, In-Process Vector Search (SIMD/LINQ Cosine Similarity), Angular 19+ Standalone, Signals, TailwindCSS, xUnit, FluentAssertions.

**Spec:** [.agents/specs/2026-11-22-sprint-5-long-term-memory-and-rag-design.md](file:///c:/Workplaces/PROJECT/AgentCoreSystem/.agents/specs/2026-11-22-sprint-5-long-term-memory-and-rag-design.md)

---

## Global Constraints

- **C# Conventions:** Block-scoped namespaces (`namespace AgentCoreSystem.Module.Knowledge... { ... }`), Primary Constructors cho DI, XML doc `/// <summary>` cho public/internal members, Result Pattern (`Result<T>`, `Error`), ProblemDetails RFC 7807 qua `ToProblemDetails()`.
- **Database & UUIDv7:** Mọi Entity kế thừa `BaseEntity<Guid>`, sinh khóa `Guid.CreateVersion7()`, Schema-per-module (`knowledge`), migration history riêng `__EFMigrationsHistory_Knowledge`.
- **Modular Monolith Boundaries:** Tuyệt đối cấm Foreign Key trực tiếp sang schema khác (`org`, `chat`, `agent`, `task`). Chỉ tham chiếu qua Guid Id (`ChannelId`, `AgentProfileId`).
- **Fail-Fast & Local-First:** Mọi cấu hình liên quan đến Storage path hoặc Embedding endpoint phải Fail-Fast nếu không hợp lệ. Cấm gửi tài liệu nội bộ ra cloud công cộng không được cấu hình.
- **Asynchronous Ingestion:** Mọi tác vụ đọc file nặng (PDF parse) và gọi embedding vector đều phải chạy nền qua Hangfire, cấm chặn main HTTP request thread.
- **Angular Standards:** Standalone Components, `ChangeDetectionStrategy.OnPush`, Angular Signals (`signal`, `computed`), Control Flow mới (`@if`, `@for`), lazy load route qua `loadChildren` / `loadComponent`.

---

## Review Focus

1. **Memory & Channel Isolation (Zero Cross-Tenant Leak):** Tài liệu của Kênh A không bao giờ được xuất hiện trong kết quả tìm kiếm ngữ nghĩa của Kênh B.
2. **Corrupted / Encrypted PDF Handling:** Khi người dùng upload file PDF bị lỗi, hỏng cấu trúc hoặc có mật khẩu, Hangfire Job phải bắt ngoại lệ êm đẹp, cập nhật `Status = Failed` kèm `ErrorMessage` rõ ràng, không làm crash worker.
3. **Embedding Vector Dimension Agnostic:** Hệ thống phải hỗ trợ linh hoạt các kích thước vector (384, 768, 1536 chiều) tùy thuộc vào model embedding (Google `text-embedding-004`, `nomic-embed-text`, OpenAI `text-embedding-3-small`).
4. **Cosine Similarity Boundary Checks:** Đảm bảo xử lý an toàn trường hợp vector có norm bằng 0 (tránh chia cho 0) và lọc chính xác theo ngưỡng tương đồng tối thiểu (`minThreshold >= 0.65`).
5. **Chat Token Budget Preservation:** Giới hạn tổng số lượng token của context trích dẫn được bơm vào LLM prompt (tối đa 2,000 tokens) để không làm tràn cửa sổ ngữ cảnh của model.

---

## Task Breakdown

### Task 1: Module Knowledge Scaffolding & Host Composition

**Files:**
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Domain/AgentCoreSystem.Module.Knowledge.Domain.csproj`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Application/AgentCoreSystem.Module.Knowledge.Application.csproj`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Infrastructure/AgentCoreSystem.Module.Knowledge.Infrastructure.csproj`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Presentation/AgentCoreSystem.Module.Knowledge.Presentation.csproj`
- Modify: `backend/AgentCoreSystem.slnx`
- Modify: `backend/src/Host/AgentCoreSystem.Api/AgentCoreSystem.Api.csproj`
- Modify: `backend/src/Host/AgentCoreSystem.Api/Program.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Infrastructure/KnowledgeModuleRegistration.cs`

**Interfaces:**
- Produces: `AddKnowledgeModule()` extension method cho Host API.

- [ ] **Step 1: Create the 4 Clean Architecture projects for Module Knowledge**
- [ ] **Step 2: Add references between Domain, Application, Infrastructure, Presentation and Shared BuildingBlocks**
- [ ] **Step 3: Register projects into `AgentCoreSystem.slnx`**
- [ ] **Step 4: Register `AddKnowledgeModule()` into `Program.cs`**
- [ ] **Step 5: Verify build succeeds**
  - Run: `dotnet build backend/AgentCoreSystem.slnx`
  - Expected: Build succeeded with 0 errors.
- [ ] **Step 6: Commit**
  - `git commit -m "feat(knowledge): scaffold Module.Knowledge Clean Architecture projects and host registration"`

---

### Task 2: Domain Entities & Database Migrations (Schema `knowledge`)

**Files:**
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Domain/Entities/KnowledgeDocument.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Domain/Entities/DocumentChunk.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Domain/Entities/AgentMemoryItem.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Domain/Enums/KnowledgeScope.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Domain/Enums/DocumentStatus.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Domain/Enums/MemoryCategory.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Infrastructure/Persistence/KnowledgeDbContext.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Infrastructure/Persistence/Configurations/KnowledgeDocumentConfiguration.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Infrastructure/Persistence/Configurations/DocumentChunkConfiguration.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Infrastructure/Persistence/Configurations/AgentMemoryItemConfiguration.cs`
- Modify: `justfile` (thêm recipe `ef-add-knowledge` và `ef-update-knowledge`)
- Test: `backend/tests/AgentCoreSystem.UnitTests/Knowledge/KnowledgeEntityTests.cs`

**Interfaces:**
- Produces: `KnowledgeDbContext` với 3 DbSet trên schema `knowledge`, migrations table `__EFMigrationsHistory_Knowledge`.

- [ ] **Step 1: Write unit tests for Domain Entities creation & state validation**
- [ ] **Step 2: Implement Entities, Enums & EF Core EntityTypeConfigurations**
- [ ] **Step 3: Implement `KnowledgeDbContext` with schema `knowledge`**
- [ ] **Step 4: Run unit tests to verify entities behave correctly**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter "FullyQualifiedName~KnowledgeEntityTests"`
- [ ] **Step 5: Add EF Core Migration `Initial_Knowledge_Schema`**
  - Run: `dotnet ef migrations add Initial_Knowledge_Schema --project backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Infrastructure --startup-project backend/src/Host/AgentCoreSystem.Api --context KnowledgeDbContext --output-dir Persistence/Migrations`
- [ ] **Step 6: Commit**
  - `git commit -m "feat(knowledge): implement domain entities and EF Core initial migration for schema knowledge"`

---

### Task 3: Text Extractors & Recursive Character Chunking Pipeline

**Files:**
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Application/Services/Extraction/ITextExtractor.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Application/Services/Extraction/PlainTextExtractor.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Application/Services/Extraction/MarkdownTextExtractor.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Infrastructure/Services/Extraction/PdfPigTextExtractor.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Application/Services/Chunking/ITextChunker.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Application/Services/Chunking/RecursiveTextChunker.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Application/Services/Chunking/ChunkingOptions.cs`
- Test: `backend/tests/AgentCoreSystem.UnitTests/Knowledge/TextExtractorTests.cs`
- Test: `backend/tests/AgentCoreSystem.UnitTests/Knowledge/RecursiveTextChunkerTests.cs`

**Interfaces:**
- Produces: `ITextExtractor` (`ExtractTextAsync(Stream stream, string contentType)`)
- Produces: `ITextChunker` (`List<ChunkResult> ChunkText(string rawText, ChunkingOptions options)`)

- [ ] **Step 1: Write failing tests for PlainText, Markdown, and PDF extractors**
- [ ] **Step 2: Write failing tests for RecursiveTextChunker** (kiểm tra cắt đoạn theo đoạn văn, câu, từ và xử lý overlap 100 ký tự)
- [ ] **Step 3: Implement extractors (tích hợp UglyToad.PdfPig cho file PDF)**
- [ ] **Step 4: Implement `RecursiveTextChunker`**
- [ ] **Step 5: Run tests and verify all pass**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter "FullyQualifiedName~TextExtractorTests|FullyQualifiedName~RecursiveTextChunkerTests"`
- [ ] **Step 6: Commit**
  - `git commit -m "feat(knowledge): implement text extraction and recursive chunking pipeline"`

---

### Task 4: Embedding Provider Gateway (`IEmbeddingService`)

**Files:**
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Application/Services/Embedding/IEmbeddingService.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Application/Services/Embedding/EmbeddingModels.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Infrastructure/Services/Embedding/HttpEmbeddingService.cs`
- Modify: `backend/src/Host/AgentCoreSystem.Api/appsettings.json` (thêm mục `EmbeddingProvider`)
- Test: `backend/tests/AgentCoreSystem.UnitTests/Knowledge/EmbeddingServiceTests.cs`

**Interfaces:**
- Produces: `IEmbeddingService` (`GenerateEmbeddingAsync(string text)`, `GenerateBatchEmbeddingsAsync(IReadOnlyList<string> texts)`)

- [ ] **Step 1: Write unit tests for `HttpEmbeddingService` with mocked `HttpMessageHandler`**
  - Test sinh embedding thành công từ response JSON chuẩn OpenAI/9router (`/v1/embeddings`).
  - Test batch embeddings.
  - Test xử lý lỗi HTTP 401/500/timeout và trả về `Result.Failure`.
- [ ] **Step 2: Implement `HttpEmbeddingService` tương thích 9router, Ollama và OpenAI**
- [ ] **Step 3: Register `IEmbeddingService` trong DI container**
- [ ] **Step 4: Run tests and verify all pass**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter "FullyQualifiedName~EmbeddingServiceTests"`
- [ ] **Step 5: Commit**
  - `git commit -m "feat(knowledge): implement HttpEmbeddingService for 9router and local embeddings"`

---

### Task 5: In-Process Vector Search Engine (`IVectorSearchEngine`, `LocalCosineVectorStore`)

**Files:**
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Application/Services/VectorSearch/IVectorSearchEngine.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Application/Services/VectorSearch/VectorSearchModels.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Infrastructure/Services/VectorSearch/LocalCosineVectorStore.cs`
- Test: `backend/tests/AgentCoreSystem.UnitTests/Knowledge/LocalCosineVectorStoreTests.cs`

**Interfaces:**
- Produces: `IVectorSearchEngine` (`SearchSimilarChunksAsync`, `SearchSimilarMemoriesAsync`)

- [ ] **Step 1: Write unit tests for Cosine Similarity calculations**
  - Test độ tương đồng giữa 2 vector giống hệt = 1.0.
  - Test độ tương đồng vuông góc = 0.0.
  - Test an toàn khi norm = 0 (tránh NaN / DivideByZero).
  - Test lọc theo Scope (`ChannelId`, `AgentProfileId`) và Threshold.
- [ ] **Step 2: Implement `LocalCosineVectorStore` tối ưu hóa LINQ/SIMD dot product**
- [ ] **Step 3: Run tests and verify all pass**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter "FullyQualifiedName~LocalCosineVectorStoreTests"`
- [ ] **Step 4: Commit**
  - `git commit -m "feat(knowledge): implement LocalCosineVectorStore for in-process semantic search"`

---

### Task 6: Hangfire Ingestion Job & Documents API

**Files:**
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Application/Jobs/IProcessDocumentJob.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Infrastructure/Jobs/ProcessDocumentJob.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Application/Features/Documents/UploadKnowledgeDocument/UploadKnowledgeDocumentCommand.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Application/Features/Documents/GetKnowledgeDocuments/GetKnowledgeDocumentsQuery.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Application/Features/Documents/GetDocumentChunks/GetDocumentChunksQuery.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Application/Features/Documents/DeleteKnowledgeDocument/DeleteKnowledgeDocumentCommand.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Presentation/Controllers/v1/KnowledgeDocumentsController.cs`
- Test: `backend/tests/AgentCoreSystem.UnitTests/Knowledge/DocumentFeaturesTests.cs`

**Interfaces:**
- Produces: REST API endpoints:
  - `POST /api/v1/knowledge/documents/upload`
  - `GET /api/v1/knowledge/documents?scope={scope}&targetId={targetId}`
  - `GET /api/v1/knowledge/documents/{id}/chunks`
  - `DELETE /api/v1/knowledge/documents/{id}`

- [ ] **Step 1: Write tests for CQRS handlers (Upload, Get, Delete)**
- [ ] **Step 2: Implement `ProcessDocumentJob` với Hangfire: đọc file -> extract text -> chunk -> batch embed -> save chunks -> update document status**
- [ ] **Step 3: Implement CQRS handlers and `KnowledgeDocumentsController`**
- [ ] **Step 4: Run tests and verify all pass**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter "FullyQualifiedName~DocumentFeaturesTests"`
- [ ] **Step 5: Commit**
  - `git commit -m "feat(knowledge): implement Hangfire ingestion job and KnowledgeDocumentsController"`

---

### Task 7: Private Agent Memory Management & API

**Files:**
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Application/Features/Memories/CreateAgentMemory/CreateAgentMemoryCommand.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Application/Features/Memories/GetAgentMemories/GetAgentMemoriesQuery.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Application/Features/Memories/DeleteAgentMemory/DeleteAgentMemoryCommand.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Presentation/Controllers/v1/AgentMemoriesController.cs`
- Test: `backend/tests/AgentCoreSystem.UnitTests/Knowledge/AgentMemoryFeaturesTests.cs`

**Interfaces:**
- Produces: REST API endpoints:
  - `POST /api/v1/knowledge/memories`
  - `GET /api/v1/knowledge/memories?agentProfileId={agentProfileId}`
  - `DELETE /api/v1/knowledge/memories/{id}`

- [ ] **Step 1: Write tests for Agent Memory CQRS handlers**
- [ ] **Step 2: Implement CQRS handlers (tự động sinh embedding cho nội dung ký ức khi tạo mới)**
- [ ] **Step 3: Implement `AgentMemoriesController`**
- [ ] **Step 4: Run tests and verify all pass**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter "FullyQualifiedName~AgentMemoryFeaturesTests"`
- [ ] **Step 5: Commit**
  - `git commit -m "feat(knowledge): implement AgentMemoriesController and memory management handlers"`

---

### Task 8: Chat Grounding Injection & System Tool `search_knowledge_base`

**Files:**
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Application/Contracts/IKnowledgeModuleApi.cs`
- Create: `backend/src/Modules/Knowledge/AgentCoreSystem.Module.Knowledge.Application/Services/KnowledgeModuleApi.cs`
- Modify: `backend/src/Modules/Chat/AgentCoreSystem.Module.Chat.Infrastructure/Services/MessageDispatcherService.cs`
- Modify: `backend/src/Modules/Agents/AgentCoreSystem.Module.Agents.Infrastructure/Services/Tools/AgentToolExecutor.cs`
- Test: `backend/tests/AgentCoreSystem.UnitTests/Knowledge/GroundingInjectionTests.cs`

**Interfaces:**
- Consumes: `IKnowledgeModuleApi` inside `MessageDispatcherService`
- Produces: System Built-in Tool `search_knowledge_base(query: string, scope?: string)`

- [ ] **Step 1: Write tests for `MessageDispatcherService` grounding context injection**
  - Verify that when channel has documents and user mentions `@Agent`, relevant chunks are injected as `<knowledge_context>` in system prompt.
  - Verify private memory is injected as `<agent_private_memory>`.
- [ ] **Step 2: Implement `IKnowledgeModuleApi` for inter-module communication**
- [ ] **Step 3: Update `MessageDispatcherService` to query and inject knowledge context**
- [ ] **Step 4: Register `search_knowledge_base` built-in tool in `AgentToolExecutor`**
- [ ] **Step 5: Run tests and verify all pass**
  - Run: `dotnet test backend/tests/AgentCoreSystem.UnitTests --filter "FullyQualifiedName~GroundingInjectionTests"`
- [ ] **Step 6: Commit**
  - `git commit -m "feat(chat): inject knowledge grounding and register search_knowledge_base tool"`

---

### Task 9: Angular UI - Knowledge & Memory Management

**Files:**
- Create: `frontend/src/app/core/services/knowledge.service.ts`
- Create: `frontend/src/app/core/models/knowledge.model.ts`
- Create: `frontend/src/app/features/chat/components/channel-knowledge-tab/channel-knowledge-tab.component.ts`
- Create: `frontend/src/app/features/chat/components/channel-knowledge-tab/channel-knowledge-tab.component.html`
- Create: `frontend/src/app/features/chat/components/chunk-explorer-modal/chunk-explorer-modal.component.ts`
- Create: `frontend/src/app/features/chat/components/chunk-explorer-modal/chunk-explorer-modal.component.html`
- Create: `frontend/src/app/features/system/components/agent-memory-tab/agent-memory-tab.component.ts`
- Create: `frontend/src/app/features/system/components/agent-memory-tab/agent-memory-tab.component.html`
- Modify: `frontend/src/app/features/chat/pages/chat-room/chat-room.component.html` (thêm tab Kho Tri Thức trong side drawer của Kênh)
- Modify: `frontend/src/app/features/system/pages/agent-detail/agent-detail.component.html` (thêm tab Ký Ức)
- Modify: `frontend/src/app/features/chat/components/message-item/message-item.component.html` (hiển thị trích dẫn nguồn Citations)
- Test: `frontend/src/app/core/services/knowledge.service.spec.ts`

**Interfaces:**
- Produces: Angular Components với OnPush, Signals state và drag-and-drop file uploader.

- [ ] **Step 1: Write unit tests for `KnowledgeService`**
- [ ] **Step 2: Implement `KnowledgeService` and models**
- [ ] **Step 3: Implement `ChannelKnowledgeTabComponent` với upload file và progress status badges**
- [ ] **Step 4: Implement `ChunkExplorerModalComponent` để xem chi tiết các chunks được trích xuất**
- [ ] **Step 5: Implement `AgentMemoryTabComponent` để quản lý bài học kinh nghiệm cá nhân**
- [ ] **Step 6: Integrate tabs into Chat Room drawer và Agent Detail page**
- [ ] **Step 7: Run frontend unit tests to verify all pass**
  - Run: `npm --prefix frontend test -- --watch=false`
- [ ] **Step 8: Commit**
  - `git commit -m "feat(frontend): implement Channel Knowledge tab, Agent Memory tab, and chunk explorer modal"`

---

### Task 10: End-to-End Verification & Golden Master Test

**Files:**
- Test: `backend/tests/AgentCoreSystem.IntegrationTests/Knowledge/KnowledgeIntegrationTests.cs`

- [ ] **Step 1: Write integration test covering full end-to-end lifecycle:**
  - 1. Upload tài liệu Markdown hoặc PDF vào Kênh `#tech-research`.
  - 2. Chạy `ProcessDocumentJob` phân đoạn text và sinh vector embeddings.
  - 3. Gửi tin nhắn `@Agent` hỏi nội dung trong tài liệu.
  - 4. Kiểm chứng response của Agent chứa chính xác thông tin từ tài liệu và có thẻ trích dẫn nguồn.
- [ ] **Step 2: Run all backend tests**
  - Run: `dotnet test backend/AgentCoreSystem.slnx`
- [ ] **Step 3: Run all frontend tests**
  - Run: `npm --prefix frontend test -- --watch=false`
- [ ] **Step 4: Commit**
  - `git commit -m "test(knowledge): add end-to-end integration test for knowledge grounding and citations"`

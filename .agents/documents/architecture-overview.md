# Kiến trúc Tổng thể Hệ thống (Architecture Overview)

## 1. Mô hình Modular Monolith + Clean Architecture
Hệ thống được thiết kế theo mô hình **Modular Monolith**, kết hợp các ưu điểm của Monolith (dễ phát triển, triển khai đơn giản, transaction nhất quán) với ưu điểm của Microservices (tách biệt ranh giới nghiệp vụ, độc lập dữ liệu, sẵn sàng tách thành Microservices độc lập khi cần).

```mermaid
graph TD
    Client["Angular Frontend (Standalone, Signals)"] -->|REST API v1| Host["AgentCoreSystem.Api"]
    
    subgraph HostProject ["Host (Gateway & Composition Root)"]
        Host --> Swagger["Swagger OAS v3 Explorer"]
        Host --> Versioning["Asp.Versioning (v1 Default)"]
        Host --> GlobalEx["GlobalExceptionHandler (RFC 7807)"]
        Host --> HangfireDash["Hangfire Dashboard"]
    end
    
    Host --> ModSystem["Module: System"]
    Host --> ModFuture["Future Modules (Catalog, Order...)"]
    
    subgraph ModSystemDetail ["Module System (Clean Architecture)"]
        Sys_Pres["Presentation (v1 Controllers, OpenAPI OAS)"] --> Sys_App["Application (CQRS, MediatR Handlers)"]
        Sys_App --> Sys_Dom["Domain (Identity, Entities, UUIDv7)"]
        Sys_Infra["Infrastructure (SystemDbContext, Jobs)"] --> Sys_Dom
        Sys_Infra --> Sys_App
    end
    
    subgraph Storage ["Databases & Infrastructure"]
        SQLServer[("SQL Server\n- Schema: system\n- Schema: catalog\n- Schema: order")]
        MongoDB[("MongoDB\n- Hangfire Background Jobs")]
        MemoryCache["In-Memory Cache (Swapable to Redis)"]
    end
    
    Sys_Infra --> SQLServer
    Host --> MongoDB
    Host --> MemoryCache
```

## 2. Luồng Xử lý một Request (CQRS + Problem Details)
1. **Client** gửi HTTP request (ví dụ: `POST /api/v1/users`).
2. **Host** định tuyến qua API Versioning v1 đến `UsersController`.
3. Controller đóng gói dữ liệu thành `CreateUserCommand` và gửi đến MediatR `ISender`.
4. **MediatR Pipeline** lần lượt chạy qua các Behavior:
   - `LoggingBehavior`: Log thời gian bắt đầu.
   - `ValidationBehavior`: Chạy FluentValidation. Nếu có lỗi, trả về `Result.Failure(Error.Validation(...))`.
   - `PerformanceBehavior`: Cảnh báo nếu handler chạy quá 500ms.
5. **Handler** tiếp nhận command, thực hiện nghiệp vụ thông qua Domain Entities và Repositories.
6. Handler trả về `Result<Guid>`.
7. **Controller** dùng `ToProblemDetails()` nếu có lỗi hoặc trả về `ApiResponse<Guid>` nếu thành công.
8. Nếu có lỗi ngoại lệ đột xuất, `GlobalExceptionHandler` bắt và sinh mã 500 RFC 7807 kèm `traceId`.


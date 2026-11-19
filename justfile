# =============================================================================
# AgentCoreSystem Command Runner (justfile)
# Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>
# =============================================================================

# Cấu hình shell mặc định trên Windows sử dụng PowerShell
set windows-shell := ["powershell.exe", "-NoProfile", "-Command"]

# Mặc định: Hiển thị danh sách tất cả các lệnh khả dụng
default:
    @just --list

# -----------------------------------------------------------------------------
# 1. Development & Running
# -----------------------------------------------------------------------------

# Chạy Backend Web API (dotnet run)
api:
    dotnet run --project backend/src/Host/AgentCoreSystem.Api

# Chạy Backend Web API với chế độ Hot Reload (dotnet watch)
watch-api:
    dotnet watch --project backend/src/Host/AgentCoreSystem.Api

# Chạy Frontend Angular dev server (ng serve)
ui:
    cd frontend; npm start

# Chạy Backend API ngầm và Frontend song song (dành cho dev fullstack)
dev:
    powershell -NoProfile -Command "Start-Process powershell -ArgumentList '-NoExit', '-Command', 'dotnet watch --project backend/src/Host/AgentCoreSystem.Api'"
    cd frontend; npm start

# -----------------------------------------------------------------------------
# 2. Build & Compilation
# -----------------------------------------------------------------------------

# Build toàn bộ Solution Backend và Frontend
build: build-backend build-ui

# Build riêng Backend .NET 9 solution
build-backend:
    dotnet build backend/AgentCoreSystem.slnx

# Build riêng Frontend Angular production bundle
build-ui:
    cd frontend; npm run build

# -----------------------------------------------------------------------------
# 3. Testing
# -----------------------------------------------------------------------------

# Chạy toàn bộ Unit Tests của cả Backend và Frontend
test: test-backend test-ui

# Chạy Unit Tests Backend (.NET 9 xUnit)
test-backend:
    dotnet test backend/AgentCoreSystem.slnx

# Chạy Unit Tests Frontend (Vitest)
test-ui:
    cd frontend; npm test -- --no-watch

# Chạy End-to-End Tests với Playwright (Headless)
test-e2e:
    cd frontend; npx playwright test

# Chạy End-to-End Tests với giao diện trực quan Playwright UI Runner
test-e2e-ui:
    cd frontend; npx playwright test --ui

# Chạy End-to-End Tests cho Chat Realtime
test-e2e-chat:
    cd frontend; npx playwright test e2e/specs/sprint-1-chat-realtime.spec.ts

# -----------------------------------------------------------------------------
# 4. EF Core Migrations (Hỗ trợ Kiến trúc Schema-per-module)
# -----------------------------------------------------------------------------

# Thêm Migration mới cho một Module (Ví dụ: just ef-add System AddUserAvatar)
ef-add module name:
    dotnet ef migrations add {{name}} \
        --project backend/src/Modules/{{module}}/AgentCoreSystem.Module.{{module}}.Infrastructure \
        --startup-project backend/src/Host/AgentCoreSystem.Api \
        --context {{module}}DbContext \
        --output-dir Persistence/Migrations

# Cập nhật Database cho một Module (Ví dụ: just ef-update System)
ef-update module:
    dotnet ef database update \
        --project backend/src/Modules/{{module}}/AgentCoreSystem.Module.{{module}}.Infrastructure \
        --startup-project backend/src/Host/AgentCoreSystem.Api \
        --context {{module}}DbContext

# Xóa Migration gần nhất chưa áp dụng (Ví dụ: just ef-remove System)
ef-remove module:
    dotnet ef migrations remove \
        --project backend/src/Modules/{{module}}/AgentCoreSystem.Module.{{module}}.Infrastructure \
        --startup-project backend/src/Host/AgentCoreSystem.Api \
        --context {{module}}DbContext

# Liệt kê danh sách Migrations của một Module (Ví dụ: just ef-list System)
ef-list module:
    dotnet ef migrations list \
        --project backend/src/Modules/{{module}}/AgentCoreSystem.Module.{{module}}.Infrastructure \
        --startup-project backend/src/Host/AgentCoreSystem.Api \
        --context {{module}}DbContext

# -----------------------------------------------------------------------------
# 5. Code Quality, Formatting & Cleanup
# -----------------------------------------------------------------------------

# Tự động format toàn bộ mã nguồn C# và Frontend
format: format-backend format-ui

# Format mã nguồn C# (.NET format)
format-backend:
    dotnet format backend/AgentCoreSystem.slnx

# Format mã nguồn Angular/TypeScript/SCSS (Prettier)
format-ui:
    cd frontend; npx prettier --write "src/**/*.{ts,html,scss}"

# Dọn dẹp thư mục bin/obj và cache build
clean:
    powershell -NoProfile -Command "Get-ChildItem -Path backend -Include bin,obj -Recurse -Directory | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue; Write-Host 'Đã dọn dẹp thư mục bin/obj!' -ForegroundColor Green"

# -----------------------------------------------------------------------------
# 6. Infrastructure & Containers
# -----------------------------------------------------------------------------

# Khởi động các container hạ tầng (SQL Server, MongoDB...)
infra-up:
    docker compose up -d

# Dừng các container hạ tầng
infra-down:
    docker compose down

# Angular Development Standards

Tài liệu quy định kiến trúc và tiêu chuẩn lập trình hiện đại cho Frontend Angular.

---

## 1. Triết lý Thiết kế: Standalone-First & Feature-Driven
- Không sử dụng `NgModule` cho các component hoặc feature mới. Toàn bộ là **Standalone Components**.
- Phân chia module theo **Feature Slices**:
  - `core/`: Chứa các Singleton Services (Auth, Layout, HTTP Interceptors, Guards).
  - `shared/`: Chứa các Presentational / Dumb UI Components (Buttons, Dialogs, Tables, Pipes, Directives).
  - `features/`: Chứa các Bounded Feature Slices (vd: `auth/`, `system/`, `dashboard/`). Mỗi feature có file routing riêng (vd: `system.routes.ts`) được nạp lazy qua `loadChildren`.

---

## 2. Reactivity Model: Angular Signals
- Mặc định sử dụng **Angular Signals** (`signal()`, `computed()`, `effect()`) để quản lý State cục bộ trong Component và Feature Services.
- Sử dụng **RxJS** độc quyền cho các tác vụ bất đồng bộ (HTTP requests, WebSocket events), sau đó chuyển đổi sang Signal qua `.subscribe()` hoặc `toSignal()`.
- Áp dụng triệt để `ChangeDetectionStrategy.OnPush` trên 100% component để loại bỏ việc kích hoạt kiểm tra lại toàn bộ component tree không cần thiết.

---

## 3. Cú pháp Control Flow Mới
Bắt buộc sử dụng cú pháp Control Flow mới của Angular thay thế hoàn toàn cho `*ngIf`, `*ngFor`:
- `@if (condition) { ... } @else { ... }`
- `@for (item of items; track item.id) { ... } @empty { ... }`
- `@switch (status) { @case ('Active') { ... } @default { ... } }`
- `@defer (on viewport) { ... } @placeholder { ... } @loading { ... }` (Lazy load các widget nặng khi người dùng cuộn đến).

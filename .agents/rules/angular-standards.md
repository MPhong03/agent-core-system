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

---

## 4. Quy tắc Comment & Documentation (TypeScript & HTML Templates)

### 4.1. File Header JSDoc
Mỗi file TypeScript (`.ts`) trong Angular **BẮT BUỘC** có khối JSDoc mở đầu theo cấu trúc:
```typescript
/**
 * @file <tên_file>.ts — Mô tả chức năng ngắn gọn (Mã PBI nếu có)
 *
 * ## <Tiêu đề mục giải thích thiết kế 1>
 * Giải thích lý do kiến trúc, cấu trúc luồng dữ liệu hoặc hành vi đặc thù.
 * Ví dụ: «Hai vai trò của tab», «Cơ chế đồng bộ Signal với Router param»...
 *
 * ## <Tiêu đề mục giải thích thiết kế 2>
 * Các ràng buộc, quan hệ kéo theo (⇒) hoặc cảnh báo kỹ thuật.
 */
```

### 4.2. Ngôn ngữ & Ký hiệu Đánh dấu Đặc biệt
- **Ngôn ngữ**: Viết bằng **tiếng Việt**, giữ nguyên thuật ngữ kỹ thuật tiếng Anh (vd: *Signals*, *OnPush*, *RxJS*, *lazy loading*, *interceptor*).
- **Tập trung vào Lý do & Ràng buộc**: Không diễn giải code hiển nhiên. Nêu rõ nguyên nhân đằng sau quyết định thiết kế.
- **Ký hiệu trực quan**:
  - 🔴 : Cảnh báo «đừng đổi cái này vì...» (nguy cơ vỡ layout, race condition, hoặc lỗi OnPush detection).
  - ⛔ : Điều cấm (vd: *cấm gọi mutate trực tiếp signal object*, *cấm subscribe lồng nhau*).
  - ⇒ : Quan hệ kéo theo / hệ quả logic (vd: *reset search keyword ⇒ trang chuyển về 1*).

### 4.3. Phân Nhóm Thuộc tính & Ghi chú AI
- **`AI-NOTE:`**: Ghi chú dành riêng cho AI Agent hoặc Developer bảo trì đọc sau.
- **`// ── Tiêu đề nhóm ──`**: Chia rõ các nhóm Signals, State, Injections, Lifecycle, Actions.

### 4.4. Comment trong HTML Template
- Bắt buộc có comment HTML `<!-- ... -->` để giải thích:
  - Các **Cell Template** phức tạp trong Datatable/Grid.
  - Các khối `@defer` tải lười.
  - Các điều kiện `@if` phức tạp hoặc cấu trúc UI có ràng buộc nghiệp vụ ngầm.

#### Ví dụ Template HTML:
```html
<!-- Cell Template: Hiển thị trạng thái tài khoản kèm màu theo Role.
     🔴 Lưu ý: Không bọc hàm tính toán phức tạp tại đây để tránh re-evaluation khi CD OnPush chạy -->
<ng-template #statusCell let-user="user">
  <span class="badge" [class.badge-active]="user.isActive">
    {{ user.isActive ? 'Hoạt động' : 'Đã khóa' }}
  </span>
</ng-template>
```

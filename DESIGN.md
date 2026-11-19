# DESIGN SYSTEM SPECIFICATION: DROPBOX PACK (DESIGN.md)

> **Design Direction:** Creative-professional, editorial clarity, confident primary blue, and warm paper-like canvas.  
> **Source:** [Dropbox Design Pack via designmd-store.com](https://designmd-store.com/packs/dropbox)  
> **Audience:** AI Coding Agents, UI/UX Designers, and Frontend Developers.

---

## 1. Core Principles (Triết lý Thiết kế)

1. **Warm Canvas, Not Clinical White:**  
   Sử dụng nền màu giấy ấm (`#F7F5F2` - Coconut / Warm Paper) cho sidebar và nền tổng thể ứng dụng để tạo cảm giác dễ chịu, tự nhiên, thay vì màu trắng tinh lạnh lẽo `#FFFFFF`.
2. **Dropbox Confident Blue:**  
   Sử dụng mã màu xanh Dropbox độc quyền (`#0061FF`) làm điểm nhấn hành động duy nhất (Primary CTA, link, trạng thái active). Tránh lạm dụng quá nhiều màu sắc sặc sỡ cạnh tranh thị giác.
3. **High-Contrast Typography (Dropbox Ink):**  
   Tiêu đề và văn bản chính sử dụng màu than đen ấm `#1E1919`, mang lại độ tương phản cao, nét chữ sắc nét chuẩn editorial.
4. **Anti-AI-Slop Surfaces:**  
   Tuyệt đối **KHÔNG** dùng dải màu gradient lòe loẹt, viền neon phát sáng, hoặc đổ bóng mờ ảo (blurry heavy drop shadows). Sử dụng đường viền thanh mảnh 1px (`#D8D6D3` / `#EDECEA`) và bán kính bo góc tinh tế (6px - 8px).

---

## 2. Design Tokens (Bảng Mã Màu & Quy chuẩn)

### 2.1. Primary Action & Interactive
- **Primary Blue:** `#0061FF` (Buttons, Links, Active Navigation, Focus Ring)
- **Blue Hover / Dark:** `#0048BD` (Trạng thái rê chuột và nhấn giữ)
- **Blue Wash / Selection:** `#B4D1FF` (Vùng chọn, item đang active trong danh sách)
- **Blue Subtle Light:** `#F0F5FF` (Nền badge thông tin nhẹ nhàng)

### 2.2. Surfaces & Backgrounds
- **App Canvas (Warm Off-White):** `#F7F5F2` (Nền trang tổng thể, Sidebar, Tree view background)
- **Card / Surface (Pure White):** `#FFFFFF` (Bề mặt thẻ Card, Modal dialog, Table content)
- **Elevated Surface:** `#FFFFFF` (Kèm viền `1px solid #D8D6D3` và đổ bóng nhẹ: `0 1px 3px rgba(0, 0, 0, 0.05)`)

### 2.3. Neutral Scale & Typography
- **Text Primary (Dropbox Ink):** `#1E1919` (Tiêu đề h1-h6, nhãn quan trọng, văn bản đọc chính)
- **Text Secondary:** `#637282` (Mô tả phụ, metadata thời gian, breadcrumb)
- **Text Muted:** `#9EA9B2` (Placeholder input, icon không hoạt động)
- **Border Default:** `#D8D6D3` (Viền bao card, ô input, phân cách hàng)
- **Border Subtle:** `#EDECEA` (Đường kẻ mờ giữa các mục trong list)

### 2.4. Semantic & Status Indicators
- **Success:** `#0AC27D` (Kèm nền `#E6F9F2` cho badge hoàn thành/đồng bộ)
- **Error / Danger:** `#C0392B` (Kèm nền `#FDF0EE` cho cảnh báo lỗi, nút xóa)
- **Warning:** `#F5A623` (Kèm nền `#FEF6EB` cho cảnh báo chờ duyệt)
- **Info:** `#0061FF` (Thông báo điều hướng)

### 2.5. Spacing, Radii & Typography Scale
- **Font Family:** `Inter`, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif
- **Border Radius:**
  - `sm`: `4px` (Tags, small badges)
  - `md`: `6px` hoặc `8px` (Buttons, Inputs, Cards, Dropdowns)
  - `lg`: `12px` (Modal dialogs, Large drawers)
- **Typography Scale:**
  - Heading 1: `24px` / `line-height: 32px` / `font-weight: 600`
  - Heading 2: `20px` / `line-height: 28px` / `font-weight: 600`
  - Heading 3: `16px` / `line-height: 24px` / `font-weight: 600`
  - Body Text: `14px` / `line-height: 20px` / `font-weight: 400`
  - Caption / Meta: `12px` / `line-height: 16px` / `font-weight: 500`

---

## 3. Component Specs (Quy chuẩn Thành phần Giao diện)

### 3.1. Primary Button
```scss
button.btn-primary {
  background-color: #0061FF;
  color: #FFFFFF;
  border: 1px solid #0061FF;
  border-radius: 6px;
  padding: 8px 16px;
  font-weight: 500;
  font-size: 14px;
  cursor: pointer;
  transition: background-color 0.15s ease, border-color 0.15s ease;

  &:hover {
    background-color: #0048BD;
    border-color: #0048BD;
  }
}
```

### 3.2. Secondary / Outline Button
```scss
button.btn-secondary {
  background-color: #FFFFFF;
  color: #1E1919;
  border: 1px solid #D8D6D3;
  border-radius: 6px;
  padding: 8px 16px;
  font-weight: 500;
  font-size: 14px;
  cursor: pointer;
  transition: background-color 0.15s ease, border-color 0.15s ease;

  &:hover {
    background-color: #F7F5F2;
    border-color: #637282;
  }
}
```

### 3.3. Input Field
```scss
input.form-control, textarea.form-control, select.form-control {
  background-color: #FFFFFF;
  border: 1px solid #D8D6D3;
  border-radius: 6px;
  padding: 8px 12px;
  font-size: 14px;
  color: #1E1919;
  transition: border-color 0.15s ease, box-shadow 0.15s ease;

  &::placeholder {
    color: #9EA9B2;
  }

  &:focus {
    outline: none;
    border-color: #0061FF;
    box-shadow: 0 0 0 2px rgba(0, 97, 255, 0.2);
  }
}
```

### 3.4. Badges (Human vs Agent)
```scss
.badge-human {
  background-color: #F0F5FF;
  color: #0061FF;
  border: 1px solid #B4D1FF;
  border-radius: 4px;
  padding: 2px 8px;
  font-size: 12px;
  font-weight: 500;
}

.badge-agent {
  background-color: #E6F9F2;
  color: #0AC27D;
  border: 1px solid rgba(10, 194, 125, 0.3);
  border-radius: 4px;
  padding: 2px 8px;
  font-size: 12px;
  font-weight: 500;
}
```

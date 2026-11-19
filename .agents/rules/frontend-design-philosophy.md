# Frontend Design Philosophy: Hallmark + Minimalist UI + Frontend Design (Dropbox Pack)

> **Mục tiêu:** Định hình tiêu chuẩn thiết kế giao diện người dùng (UI/UX) cho toàn bộ ứng dụng Client của AgentCoreSystem, loại bỏ triệt để các định kiến giao diện AI dập khuôn (Anti-AI-Slop), hướng đến vẻ đẹp biên tập tối giản (Editorial Utilitarian Minimalism) theo chuẩn **Dropbox Pack** ([DESIGN.md](../../DESIGN.md)).

---

## 1. Bộ Ba Trụ Cột Thiết Kế (The Trinity)

1. **Hallmark (Anti-AI-Slop & Kỷ luật Tương tác):**
   - Loại bỏ hoàn toàn các dải màu gradient lòe loẹt, viền neon phát sáng, đổ bóng mờ ảo (blurry heavy drop shadows).
   - **Kỷ luật 8 Trạng thái Tương tác (Mandatory 8 States):** Mọi tương tác cốt lõi (button, input, select, card) bắt buộc phải có thiết lập chi tiết cho: `default`, `:hover`, `:active`, `:focus-visible`, `disabled`, `loading`, `error`, `success`.
   - **Nói không với nội dung giả tạo:** Không dùng emoji làm biểu tượng chính thức, không dùng avatar sến súa hay copy sáo rỗng ("Seamless", "Elevate").

2. **Minimalist UI (Chủ nghĩa Tối giản Biên tập & Khối Bento):**
   - **Warm Canvas:** Sử dụng nền màu giấy ấm (`#F7F5F2` - Coconut / Warm Paper) cho sidebar và cấu trúc khung ngoài để mang lại sự dịu mắt và cảm giác cao cấp.
   - **Đường nét & Viền 1px:** Mọi đường phân cách, viền card đều là nét mảnh 1px (`#D8D6D3` hoặc `#EDECEA`). Bán kính bo góc gọn gàng từ 6px đến 8px.
   - **Pastel làm điểm nhấn:** Dùng màu pastel nhẹ nhàng cho tags/badges (Pale Green `#E6F9F2`, Pale Blue `#F0F5FF`) thay vì các mảng màu rực rỡ chói lọi.

3. **Frontend Design (Hài hòa Không gian & Phân tầng Kiểu chữ):**
   - **Dropbox Ink (`#1E1919`):** Tiêu đề và chữ đọc chính sử dụng màu than đen ấm với độ tương phản cao, sắc nét.
   - **Dropbox Confident Blue (`#0061FF`):** Điểm nhấn hành động duy nhất cho Primary CTA, link và trạng thái active.
   - **Typography Hierarchy:** Phân tầng cỡ chữ rành mạch, kết hợp font hệ thống sans-serif thanh thoát cùng font monospace cho ID/metadata.

---

## 2. Quy tắc Triển khai trên Angular

- **Standalone & Signals:** Mọi component sử dụng `ChangeDetectionStrategy.OnPush`, quản lý state phản ứng bằng Angular Signals (`signal`, `computed`).
- **SCSS Tokens:** Kế thừa biến màu và quy chuẩn từ [DESIGN.md](../../DESIGN.md). Không viết cứng mã màu inline trong template.
- **Empty & Loading States:** Luôn xử lý trạng thái chờ (skeleton loader hoặc spinner tối giản) và trạng thái trống (Empty state minh họa thanh lịch với thông điệp rõ ràng).

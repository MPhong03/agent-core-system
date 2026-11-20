/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file organization.routes.ts — Định tuyến phân hệ Tổ chức & Phòng ban (Sprint 0 - PBI-ORG-01)
 *
 * ## Kiến trúc Định tuyến
 * Phân hệ Tổ chức (Organization) quản lý cấu trúc cây Phòng ban (Department), Kênh làm việc (TeamChannel)
 * và phân bổ Thành viên (Member) gồm cả Người dùng nội bộ lẫn Hồ sơ Agent AI.
 *
 * ## Ràng buộc Kỹ thuật
 * 🔴 Lazy Loading: Toàn bộ component con nạp thông qua `loadComponent` độc lập.
 * ⇒ Truy cập route rỗng `''` điều hướng thẳng tới OrganizationShellComponent.
 */

import { Routes } from '@angular/router';

// ── Cấu hình Routes Phân hệ Organization ──
export const ORGANIZATION_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/organization-shell/organization-shell.component').then(
        (m) => m.OrganizationShellComponent,
      ),
  },
];

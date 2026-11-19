/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { Component, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-roles-list',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="roles-page">
      <div class="page-header">
        <h2>Quản lý Vai trò &amp; Phân quyền (RBAC)</h2>
        <p>3 Role mặc định của hệ thống: SuperAdmin, Admin, Employee</p>
      </div>

      <div class="roles-grid">
        <div class="role-card">
          <div class="role-badge superadmin">SuperAdmin</div>
          <h3>Quản trị viên Tối cao</h3>
          <p>
            Toàn quyền trên toàn bộ hệ thống. Có thể cấu hình cơ sở hạ tầng, quản lý tất cả các phân
            hệ và bypass kiểm tra quyền hạn.
          </p>
        </div>

        <div class="role-card">
          <div class="role-badge admin">Admin</div>
          <h3>Quản trị viên</h3>
          <p>
            Quản lý tài khoản người dùng, thiết lập phân hệ và theo dõi hoạt động vận hành của chi
            nhánh/phân hệ.
          </p>
        </div>

        <div class="role-card">
          <div class="role-badge employee">Employee</div>
          <h3>Nhân viên Tác nghiệp</h3>
          <p>
            Nhân viên thực hiện các nghiệp vụ thông thường trong phạm vi chức năng được cấp phép.
          </p>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .roles-page {
        display: flex;
        flex-direction: column;
        gap: 20px;
      }
      .page-header h2 {
        margin: 0 0 4px 0;
        color: #0f172a;
        font-size: 1.4rem;
      }
      .page-header p {
        margin: 0;
        color: #64748b;
        font-size: 0.85rem;
      }
      .roles-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
        gap: 16px;
      }
      .role-card {
        background: #fff;
        padding: 24px;
        border-radius: 10px;
        border: 1px solid #e2e8f0;
      }
      .role-badge {
        display: inline-block;
        padding: 4px 10px;
        border-radius: 9999px;
        font-size: 0.75rem;
        font-weight: 700;
        margin-bottom: 12px;
      }
      .role-badge.superadmin {
        background: #fee2e2;
        color: #991b1b;
      }
      .role-badge.admin {
        background: #dbeafe;
        color: #1e40af;
      }
      .role-badge.employee {
        background: #f1f5f9;
        color: #475569;
      }
      .role-card h3 {
        margin: 0 0 8px 0;
        color: #0f172a;
        font-size: 1.1rem;
      }
      .role-card p {
        margin: 0;
        color: #64748b;
        font-size: 0.875rem;
        line-height: 1.5;
      }
    `,
  ],
})
export class RolesListComponent {}

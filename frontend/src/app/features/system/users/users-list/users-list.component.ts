/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { Component, ChangeDetectionStrategy, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserService } from '../services/user.service';
import { UserDto } from '../models/user.model';
import { HasRoleDirective } from '../../../../shared/directives/has-role.directive';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { UserFormModalComponent } from '../user-form-modal/user-form-modal.component';
import { ChangePasswordModalComponent } from '../change-password-modal/change-password-modal.component';

@Component({
  selector: 'app-users-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    HasRoleDirective,
    TranslatePipe,
    UserFormModalComponent,
    ChangePasswordModalComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-container">
      <div class="page-header">
        <div>
          <h2>{{ 'USERS.TITLE' | translate }}</h2>
          <p>{{ 'USERS.SUBTITLE' | translate }}</p>
        </div>
        <button
          class="btn-primary"
          (click)="openCreateModal()"
          *appHasRole="['SuperAdmin', 'Admin']"
        >
          ➕ {{ 'USERS.BTN_ADD' | translate }}
        </button>
      </div>

      <!-- Filter Bar -->
      <div class="filter-card">
        <input
          type="text"
          [(ngModel)]="searchKeyword"
          (keyup.enter)="onSearch()"
          [placeholder]="'USERS.SEARCH_PLACEHOLDER' | translate"
          class="input-search"
        />
        <button (click)="onSearch()" class="btn-secondary">
          {{ 'USERS.BTN_SEARCH' | translate }}
        </button>
      </div>

      <!-- Data Table -->
      <div class="table-card">
        @if (userService.isLoading()) {
          <div class="loading-state">Đang tải dữ liệu...</div>
        } @else {
          <table class="data-table">
            <thead>
              <tr>
                <th>{{ 'USERS.COL_NAME' | translate }}</th>
                <th>{{ 'USERS.COL_EMAIL' | translate }}</th>
                <th>{{ 'USERS.COL_ROLE' | translate }}</th>
                <th>{{ 'USERS.COL_STATUS' | translate }}</th>
                <th>{{ 'USERS.COL_CREATED' | translate }}</th>
                <th class="text-right">{{ 'USERS.COL_ACTIONS' | translate }}</th>
              </tr>
            </thead>
            <tbody>
              @for (user of userService.users(); track user.id) {
                <tr>
                  <td>
                    <div class="user-cell">
                      <div class="avatar">{{ user.fullName.slice(0, 2).toUpperCase() }}</div>
                      <div>
                        <div class="user-name">{{ user.fullName }}</div>
                        <div class="user-id">UUIDv7: {{ user.id.slice(0, 8) }}...</div>
                      </div>
                    </div>
                  </td>
                  <td>{{ user.email }}</td>
                  <td>
                    <div class="roles-tags">
                      @for (role of user.roles; track role) {
                        <span class="role-tag" [class.superadmin]="role === 'SuperAdmin'">{{
                          role
                        }}</span>
                      }
                    </div>
                  </td>
                  <td>
                    <span class="status-badge" [class.active]="user.isActive">
                      {{
                        user.isActive
                          ? ('USERS.STATUS_ACTIVE' | translate)
                          : ('USERS.STATUS_LOCKED' | translate)
                      }}
                    </span>
                  </td>
                  <td>{{ user.createdDate | date: 'dd/MM/yyyy HH:mm' }}</td>
                  <td class="text-right actions-cell">
                    <button
                      class="btn-action"
                      title="Đổi mật khẩu"
                      (click)="openPasswordModal(user)"
                    >
                      🔑
                    </button>
                    <button
                      class="btn-action"
                      title="Sửa"
                      (click)="openEditModal(user)"
                      *appHasRole="['SuperAdmin', 'Admin']"
                    >
                      ✏️
                    </button>
                    <button
                      class="btn-action text-danger"
                      title="Xóa mềm"
                      (click)="confirmDelete(user)"
                      *appHasRole="['SuperAdmin', 'Admin']"
                    >
                      🗑️
                    </button>
                  </td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="6" class="empty-state">
                    {{ 'USERS.EMPTY' | translate }}
                  </td>
                </tr>
              }
            </tbody>
          </table>
        }

        <div class="table-footer">
          <span
            >{{ 'USERS.TOTAL' | translate }} <strong>{{ userService.totalCount() }}</strong></span
          >
        </div>
      </div>

      <!-- Modals -->
      @if (showUserModal()) {
        <app-user-form-modal
          [userToEdit]="selectedUserForEdit()"
          (saved)="onModalSaved()"
          (closed)="showUserModal.set(false)"
        >
        </app-user-form-modal>
      }

      @if (showPasswordModal()) {
        <app-change-password-modal
          [targetUser]="selectedUserForPassword()"
          (saved)="onModalSaved()"
          (closed)="showPasswordModal.set(false)"
        >
        </app-change-password-modal>
      }
    </div>
  `,
  styles: [
    `
      .page-container {
        display: flex;
        flex-direction: column;
        gap: 16px;
      }
      .page-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
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
      .btn-primary {
        background-color: #2563eb;
        color: #fff;
        border: none;
        padding: 10px 18px;
        border-radius: 6px;
        font-weight: 600;
        cursor: pointer;
      }
      .btn-secondary {
        background-color: #f1f5f9;
        color: #334155;
        border: 1px solid #cbd5e1;
        padding: 10px 16px;
        border-radius: 6px;
        font-weight: 600;
        cursor: pointer;
      }
      .filter-card {
        background: #fff;
        padding: 14px;
        border-radius: 8px;
        border: 1px solid #e2e8f0;
        display: flex;
        gap: 10px;
      }
      .input-search {
        flex: 1;
        padding: 8px 14px;
        border: 1px solid #cbd5e1;
        border-radius: 6px;
        outline: none;
      }
      .table-card {
        background: #fff;
        border-radius: 8px;
        border: 1px solid #e2e8f0;
        overflow: hidden;
      }
      .data-table {
        width: 100%;
        border-collapse: collapse;
        text-align: left;
      }
      .data-table th {
        background-color: #f8fafc;
        padding: 12px 16px;
        font-size: 0.75rem;
        font-weight: 700;
        color: #64748b;
        border-bottom: 1px solid #e2e8f0;
      }
      .data-table td {
        padding: 14px 16px;
        border-bottom: 1px solid #f1f5f9;
        font-size: 0.875rem;
        color: #334155;
      }
      .user-cell {
        display: flex;
        align-items: center;
        gap: 10px;
      }
      .avatar {
        width: 32px;
        height: 32px;
        border-radius: 50%;
        background: #e0f2fe;
        color: #0369a1;
        font-weight: 700;
        font-size: 0.75rem;
        display: flex;
        align-items: center;
        justify-content: center;
      }
      .user-name {
        font-weight: 600;
        color: #0f172a;
      }
      .user-id {
        font-size: 0.72rem;
        color: #94a3b8;
      }
      .role-tag {
        background: #f1f5f9;
        color: #475569;
        padding: 2px 8px;
        border-radius: 4px;
        font-size: 0.75rem;
        font-weight: 600;
        margin-right: 4px;
      }
      .role-tag.superadmin {
        background: #fee2e2;
        color: #991b1b;
      }
      .status-badge {
        display: inline-block;
        padding: 2px 8px;
        border-radius: 9999px;
        font-size: 0.75rem;
        font-weight: 600;
        background-color: #f1f5f9;
        color: #64748b;
      }
      .status-badge.active {
        background-color: #dcfce7;
        color: #166534;
      }
      .text-right {
        text-align: right;
      }
      .actions-cell {
        display: flex;
        justify-content: flex-end;
        gap: 4px;
      }
      .btn-action {
        background: none;
        border: none;
        cursor: pointer;
        font-size: 1rem;
        padding: 4px 6px;
        border-radius: 4px;
      }
      .btn-action:hover {
        background-color: #f1f5f9;
      }
      .btn-action.text-danger:hover {
        background-color: #fee2e2;
      }
      .loading-state,
      .empty-state {
        padding: 40px;
        text-align: center;
        color: #64748b;
      }
      .table-footer {
        padding: 12px 16px;
        background-color: #f8fafc;
        font-size: 0.8rem;
        color: #64748b;
        border-top: 1px solid #e2e8f0;
      }
    `,
  ],
})
export class UsersListComponent implements OnInit {
  readonly userService = inject(UserService);
  searchKeyword = '';

  readonly showUserModal = signal<boolean>(false);
  readonly selectedUserForEdit = signal<UserDto | null>(null);

  readonly showPasswordModal = signal<boolean>(false);
  readonly selectedUserForPassword = signal<UserDto | null>(null);

  ngOnInit(): void {
    this.userService.fetchUsers();
  }

  onSearch(): void {
    this.userService.fetchUsers(1, 10, this.searchKeyword);
  }

  openCreateModal(): void {
    this.selectedUserForEdit.set(null);
    this.showUserModal.set(true);
  }

  openEditModal(user: UserDto): void {
    this.selectedUserForEdit.set(user);
    this.showUserModal.set(true);
  }

  openPasswordModal(user: UserDto): void {
    this.selectedUserForPassword.set(user);
    this.showPasswordModal.set(true);
  }

  confirmDelete(user: UserDto): void {
    if (confirm(`Bạn có chắc chắn muốn xóa tài khoản "${user.fullName}" (${user.email})?`)) {
      this.userService.deleteUser(user.id).subscribe({
        next: () => alert('Đã xóa mềm tài khoản thành công.'),
        error: (err) => alert(err.detail || 'Không thể xóa tài khoản.'),
      });
    }
  }

  onModalSaved(): void {
    this.userService.fetchUsers(1, 10, this.searchKeyword);
  }
}

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
import { AppButtonComponent } from '../../../../shared/components/button/button.component';
import { AppIconComponent } from '../../../../shared/components/icon/icon.component';
import { AppBadgeComponent } from '../../../../shared/components/badge/badge.component';

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
    AppButtonComponent,
    AppIconComponent,
    AppBadgeComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-7xl mx-auto w-full px-4 sm:px-6 lg:px-8 py-6 sm:py-8 space-y-6">
      <!-- Page Header -->
      <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 class="text-xl sm:text-2xl font-bold text-black dark:text-white tracking-tight">
            {{ 'USERS.TITLE' | translate }}
          </h2>
          <p class="text-xs sm:text-sm text-body dark:text-bodydark mt-1">
            {{ 'USERS.SUBTITLE' | translate }}
          </p>
        </div>

        <div *appHasRole="['SuperAdmin', 'Admin']">
          <app-button variant="primary" size="md" (click)="openCreateModal()">
            <app-icon name="plus" [size]="18" class="mr-1.5" />
            {{ 'USERS.BTN_ADD' | translate }}
          </app-button>
        </div>
      </div>

      <!-- Unified Data Table Card -->
      <div class="rounded-xl border border-stroke bg-white shadow-xs dark:border-strokedark dark:bg-boxdark overflow-hidden">
        <!-- Integrated Toolbar Header -->
        <div class="p-5 border-b border-stroke dark:border-strokedark flex flex-col sm:flex-row items-center justify-between gap-4">
          <div class="relative flex-1 w-full max-w-md">
            <input
              type="text"
              [(ngModel)]="searchKeyword"
              (keyup.enter)="onSearch()"
              [placeholder]="'USERS.SEARCH_PLACEHOLDER' | translate"
              class="w-full h-10 rounded-lg border border-stroke bg-transparent pl-10 pr-4 text-sm text-black outline-none transition-colors focus:border-primary dark:border-strokedark dark:bg-meta-4 dark:text-white dark:focus:border-primary"
            />
            <span class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3.5 text-body dark:text-bodydark">
              <app-icon name="search" [size]="18" />
            </span>
          </div>

          <div class="flex items-center gap-2.5 w-full sm:w-auto">
            <app-button variant="secondary" size="md" (click)="onSearch()" class="w-full sm:w-auto">
              {{ 'USERS.BTN_SEARCH' | translate }}
            </app-button>
          </div>
        </div>

        @if (userService.isLoading()) {
          <div class="p-8 text-center text-sm text-body dark:text-bodydark flex items-center justify-center gap-2">
            <app-icon name="refresh" [size]="20" class="animate-spin text-primary" />
            <span>Đang tải dữ liệu người dùng...</span>
          </div>
        } @else {
          <div class="max-w-full overflow-x-auto">
            <table class="w-full table-auto">
              <thead>
                <tr class="bg-gray-2 text-left dark:bg-meta-4">
                  <th class="py-3.5 px-6 font-semibold text-xs text-black dark:text-white uppercase tracking-wider">
                    {{ 'USERS.COL_NAME' | translate }}
                  </th>
                  <th class="py-3.5 px-6 font-semibold text-xs text-black dark:text-white uppercase tracking-wider">
                    {{ 'USERS.COL_EMAIL' | translate }}
                  </th>
                  <th class="py-3.5 px-6 font-semibold text-xs text-black dark:text-white uppercase tracking-wider">
                    {{ 'USERS.COL_ROLE' | translate }}
                  </th>
                  <th class="py-3.5 px-6 font-semibold text-xs text-black dark:text-white uppercase tracking-wider">
                    {{ 'USERS.COL_STATUS' | translate }}
                  </th>
                  <th class="py-3.5 px-6 font-semibold text-xs text-black dark:text-white uppercase tracking-wider">
                    {{ 'USERS.COL_CREATED' | translate }}
                  </th>
                  <th class="py-3.5 px-6 font-semibold text-xs text-black dark:text-white uppercase tracking-wider text-right">
                    {{ 'USERS.COL_ACTIONS' | translate }}
                  </th>
                </tr>
              </thead>
              <tbody class="divide-y divide-stroke dark:divide-strokedark">
                @for (user of userService.users(); track user.id) {
                  <tr class="hover:bg-gray-2/50 dark:hover:bg-meta-4/20 transition-colors">
                    <!-- User Info Cell -->
                    <td class="py-4 px-6">
                      <div class="flex items-center gap-3">
                        <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary dark:bg-primary/20 dark:text-white font-semibold text-xs border border-primary/20">
                          {{ user.fullName.slice(0, 2).toUpperCase() }}
                        </div>
                        <div class="min-w-0">
                          <div class="font-medium text-sm text-black dark:text-white truncate">
                            {{ user.fullName }}
                          </div>
                          <div class="text-[11px] text-body dark:text-bodydark font-mono truncate">
                            UUIDv7: {{ user.id.slice(0, 8) }}...
                          </div>
                        </div>
                      </div>
                    </td>

                    <!-- Email Cell -->
                    <td class="py-4 px-6 text-sm text-body dark:text-bodydark font-medium">
                      {{ user.email }}
                    </td>

                    <!-- Roles Cell -->
                    <td class="py-4 px-6">
                      <div class="flex flex-wrap gap-1.5">
                        @for (role of user.roles; track role) {
                          <app-badge [variant]="role === 'SuperAdmin' ? 'danger' : role === 'Admin' ? 'primary' : 'neutral'" size="sm">
                            {{ role }}
                          </app-badge>
                        }
                      </div>
                    </td>

                    <!-- Status Cell -->
                    <td class="py-4 px-6">
                      <app-badge [variant]="user.isActive ? 'success' : 'danger'" size="sm">
                        {{ user.isActive ? ('USERS.STATUS_ACTIVE' | translate) : ('USERS.STATUS_LOCKED' | translate) }}
                      </app-badge>
                    </td>

                    <!-- Created Date Cell -->
                    <td class="py-4 px-6 text-xs text-body dark:text-bodydark">
                      {{ user.createdDate | date: 'dd/MM/yyyy HH:mm' }}
                    </td>

                    <!-- Actions Cell -->
                    <td class="py-4 px-6 text-right">
                      <div class="flex items-center justify-end gap-1.5">
                        <button
                          type="button"
                          class="flex h-8 w-8 items-center justify-center rounded-lg text-body hover:bg-gray-2 hover:text-black dark:text-bodydark dark:hover:bg-meta-4 dark:hover:text-white transition-colors"
                          title="Đổi mật khẩu"
                          (click)="openPasswordModal(user)"
                        >
                          <app-icon name="lock" [size]="16" />
                        </button>
                        <button
                          type="button"
                          class="flex h-8 w-8 items-center justify-center rounded-lg text-body hover:bg-gray-2 hover:text-primary dark:text-bodydark dark:hover:bg-meta-4 dark:hover:text-primary transition-colors"
                          title="Chỉnh sửa"
                          (click)="openEditModal(user)"
                          *appHasRole="['SuperAdmin', 'Admin']"
                        >
                          <app-icon name="edit" [size]="16" />
                        </button>
                        <button
                          type="button"
                          class="flex h-8 w-8 items-center justify-center rounded-lg text-body hover:bg-danger/10 hover:text-danger dark:text-bodydark dark:hover:bg-danger/20 dark:hover:text-danger transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
                          [title]="isSuperAdmin(user) ? 'Không thể xóa tài khoản Quản trị viên tối cao (SuperAdmin)' : 'Xóa mềm'"
                          [disabled]="isSuperAdmin(user)"
                          (click)="confirmDelete(user)"
                          *appHasRole="['SuperAdmin', 'Admin']"
                        >
                          <app-icon name="trash" [size]="16" />
                        </button>
                      </div>
                    </td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="6" class="py-8 text-center text-sm text-body dark:text-bodydark">
                      {{ 'USERS.EMPTY' | translate }}
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }

        <!-- Table Footer -->
        <div class="border-t border-stroke py-4 px-6 dark:border-strokedark flex items-center justify-between text-xs text-body dark:text-bodydark">
          <span>{{ 'USERS.TOTAL' | translate }} <strong class="text-black dark:text-white">{{ userService.totalCount() }}</strong></span>
        </div>
      </div>

      <!-- User Form Modal -->
      @if (showUserModal()) {
        <app-user-form-modal
          [userToEdit]="selectedUserForEdit()"
          (saved)="onModalSaved()"
          (closed)="showUserModal.set(false)"
        />
      }

      <!-- Change Password Modal -->
      @if (showPasswordModal()) {
        <app-change-password-modal
          [targetUser]="selectedUserForPassword()"
          (saved)="showPasswordModal.set(false)"
          (closed)="showPasswordModal.set(false)"
        />
      }
    </div>
  `,
})
export class UsersListComponent implements OnInit {
  readonly userService = inject(UserService);

  searchKeyword = '';

  readonly showUserModal = signal<boolean>(false);
  readonly showPasswordModal = signal<boolean>(false);
  readonly selectedUserForEdit = signal<UserDto | null>(null);
  readonly selectedUserForPassword = signal<UserDto | null>(null);

  ngOnInit(): void {
    this.userService.fetchUsers();
  }

  onSearch(): void {
    this.userService.fetchUsers(1, 10, this.searchKeyword.trim());
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
        next: () => this.userService.fetchUsers(1, 10, this.searchKeyword.trim()),
        error: (err) => alert(err.detail || 'Xóa người dùng thất bại.'),
      });
    }
  }

  isSuperAdmin(user: UserDto): boolean {
    return user.roles.includes('SuperAdmin');
  }

  onModalSaved(): void {
    this.showUserModal.set(false);
    this.userService.fetchUsers(1, 10, this.searchKeyword.trim());
  }
}

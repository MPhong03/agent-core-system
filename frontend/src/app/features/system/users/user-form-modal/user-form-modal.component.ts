/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  input,
  output,
  OnInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { UserService } from '../services/user.service';
import { UserDto, CreateUserRequest, UpdateUserRequest } from '../models/user.model';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { AppModalComponent } from '../../../../shared/components/modal/modal.component';
import { AppButtonComponent } from '../../../../shared/components/button/button.component';
import { AppIconComponent } from '../../../../shared/components/icon/icon.component';

@Component({
  selector: 'app-user-form-modal',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TranslatePipe,
    AppModalComponent,
    AppButtonComponent,
    AppIconComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-modal
      [isOpen]="true"
      [title]="modalTitle()"
      size="md"
      (close)="close()"
    >
      @if (errorMessage()) {
        <div class="mb-4 flex items-center gap-2 rounded-md bg-danger/10 border border-danger/20 p-3 text-xs text-danger dark:bg-danger/20 dark:border-danger/30">
          <app-icon name="close" [size]="16" class="shrink-0" />
          <span>{{ errorMessage() }}</span>
        </div>
      }

      <form [formGroup]="form" (ngSubmit)="onSubmit()" class="space-y-4">
        <!-- Full Name -->
        <div>
          <label class="mb-1.5 block text-xs font-semibold text-black dark:text-white">
            {{ 'MODAL.FULL_NAME' | translate }} <span class="text-danger">*</span>
          </label>
          <input
            type="text"
            formControlName="fullName"
            placeholder="Nguyễn Văn A"
            class="w-full h-10 rounded-lg border border-stroke bg-transparent px-3.5 text-sm text-black outline-none transition-colors focus:border-primary dark:border-strokedark dark:bg-meta-4 dark:text-white dark:focus:border-primary"
          />
        </div>

        <!-- Email -->
        <div>
          <label class="mb-1.5 block text-xs font-semibold text-black dark:text-white">
            {{ 'MODAL.EMAIL' | translate }} <span class="text-danger">*</span>
          </label>
          <input
            type="email"
            formControlName="email"
            placeholder="example@agentcore.vn"
            [readonly]="isEditMode()"
            [class.opacity-60]="isEditMode()"
            class="w-full h-10 rounded-lg border border-stroke bg-transparent px-3.5 text-sm text-black outline-none transition-colors focus:border-primary dark:border-strokedark dark:bg-meta-4 dark:text-white dark:focus:border-primary"
          />
        </div>

        <!-- Password (Create mode only) -->
        @if (!isEditMode()) {
          <div>
            <label class="mb-1.5 block text-xs font-semibold text-black dark:text-white">
              {{ 'MODAL.PASSWORD' | translate }} <span class="text-danger">*</span>
            </label>
            <input
              type="password"
              formControlName="password"
              placeholder="••••••••"
              class="w-full h-10 rounded-lg border border-stroke bg-transparent px-3.5 text-sm text-black outline-none transition-colors focus:border-primary dark:border-strokedark dark:bg-meta-4 dark:text-white dark:focus:border-primary"
            />
          </div>
        }

        <!-- Phone Number -->
        <div>
          <label class="mb-1.5 block text-xs font-semibold text-black dark:text-white">
            {{ 'MODAL.PHONE' | translate }}
          </label>
          <input
            type="text"
            formControlName="phoneNumber"
            placeholder="0912345678"
            class="w-full h-10 rounded-lg border border-stroke bg-transparent px-3.5 text-sm text-black outline-none transition-colors focus:border-primary dark:border-strokedark dark:bg-meta-4 dark:text-white dark:focus:border-primary"
          />
        </div>

        <!-- Roles Selection -->
        <div>
          <label class="mb-1.5 block text-xs font-semibold text-black dark:text-white">
            {{ 'MODAL.ROLES' | translate }} <span class="text-danger">*</span>
          </label>
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-2.5">
            @for (role of availableRoles; track role) {
              <label class="flex items-center gap-2 p-2.5 rounded-lg border border-stroke dark:border-strokedark hover:bg-gray-2 dark:hover:bg-meta-4 cursor-pointer text-xs font-medium text-black dark:text-white transition-colors">
                <input
                  type="checkbox"
                  [checked]="hasRole(role)"
                  (change)="toggleRole(role, $event)"
                  class="rounded text-primary focus:ring-primary h-4 w-4"
                />
                <span>{{ role }}</span>
              </label>
            }
          </div>
        </div>

        <!-- Active Status (Edit mode only) -->
        @if (isEditMode()) {
          <div class="pt-1">
            <label class="flex items-center gap-2 cursor-pointer text-xs font-medium text-black dark:text-white">
              <input
                type="checkbox"
                formControlName="isActive"
                class="rounded text-primary focus:ring-primary h-4 w-4"
              />
              <span>{{ 'MODAL.IS_ACTIVE' | translate }}</span>
            </label>
          </div>
        }
      </form>

      <!-- Modal Footer -->
      <div modal-footer class="flex items-center justify-end gap-2.5 w-full">
        <app-button variant="secondary" size="sm" (click)="close()">
          {{ 'MODAL.BTN_CANCEL' | translate }}
        </app-button>
        <app-button
          variant="primary"
          size="sm"
          [loading]="isSaving()"
          [disabled]="form.invalid || isSaving()"
          (click)="onSubmit()"
        >
          {{ isSaving() ? ('MODAL.BTN_SAVING' | translate) : ('MODAL.BTN_SAVE' | translate) }}
        </app-button>
      </div>
    </app-modal>
  `,
})
export class UserFormModalComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly userService = inject(UserService);

  readonly userToEdit = input<UserDto | null>(null);
  readonly saved = output<void>();
  readonly closed = output<void>();

  readonly availableRoles = ['SuperAdmin', 'Admin', 'Employee'];
  readonly selectedRoles = signal<string[]>([]);
  readonly isSaving = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.group({
    fullName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email]],
    password: [''],
    phoneNumber: [''],
    isActive: [true],
  });

  isEditMode(): boolean {
    return !!this.userToEdit();
  }

  modalTitle(): string {
    return this.isEditMode() ? 'Chỉnh sửa Người dùng' : 'Thêm Người dùng mới';
  }

  ngOnInit(): void {
    const edit = this.userToEdit();
    if (edit) {
      this.form.patchValue({
        fullName: edit.fullName,
        email: edit.email,
        phoneNumber: edit.phoneNumber || '',
        isActive: edit.isActive,
      });
      this.selectedRoles.set([...edit.roles]);
    } else {
      this.form.get('password')?.setValidators([Validators.required, Validators.minLength(6)]);
    }
  }

  hasRole(role: string): boolean {
    return this.selectedRoles().includes(role);
  }

  toggleRole(role: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    const current = [...this.selectedRoles()];
    if (checked) {
      if (!current.includes(role)) current.push(role);
    } else {
      const idx = current.indexOf(role);
      if (idx !== -1) current.splice(idx, 1);
    }
    this.selectedRoles.set(current);
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    if (this.selectedRoles().length === 0) {
      this.errorMessage.set('Vui lòng chọn ít nhất một vai trò.');
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    const val = this.form.getRawValue();

    if (this.isEditMode()) {
      const edit = this.userToEdit()!;
      const updateReq: UpdateUserRequest = {
        id: edit.id,
        fullName: val.fullName!,
        phoneNumber: val.phoneNumber || undefined,
        isActive: val.isActive ?? true,
        roles: this.selectedRoles(),
      };

      this.userService.updateUser(edit.id, updateReq).subscribe({
        next: () => {
          this.isSaving.set(false);
          this.saved.emit();
          this.close();
        },
        error: (err) => {
          this.isSaving.set(false);
          this.errorMessage.set(err.detail || 'Cập nhật tài khoản thất bại.');
        },
      });
    } else {
      const createReq: CreateUserRequest = {
        fullName: val.fullName!,
        email: val.email!,
        password: val.password!,
        phoneNumber: val.phoneNumber || undefined,
        roles: this.selectedRoles(),
      };

      this.userService.createUser(createReq).subscribe({
        next: () => {
          this.isSaving.set(false);
          this.saved.emit();
          this.close();
        },
        error: (err) => {
          this.isSaving.set(false);
          this.errorMessage.set(err.detail || 'Tạo mới tài khoản thất bại.');
        },
      });
    }
  }

  close(): void {
    this.closed.emit();
  }
}

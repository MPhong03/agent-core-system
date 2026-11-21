/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { Component, ChangeDetectionStrategy, inject, signal, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { UserService } from '../services/user.service';
import { UserDto, ChangePasswordRequest } from '../models/user.model';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { AppModalComponent } from '../../../../shared/components/modal/modal.component';
import { AppButtonComponent } from '../../../../shared/components/button/button.component';
import { AppIconComponent } from '../../../../shared/components/icon/icon.component';

@Component({
  selector: 'app-change-password-modal',
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
      [title]="'Đổi mật khẩu: ' + (targetUser()?.fullName || targetUser()?.email)"
      size="md"
      (close)="close()"
    >
      @if (errorMessage()) {
        <div class="mb-4 flex items-center gap-2 rounded-md bg-danger/10 border border-danger/20 p-3 text-xs text-danger dark:bg-danger/20 dark:border-danger/30">
          <app-icon name="close" [size]="16" class="shrink-0" />
          <span>{{ errorMessage() }}</span>
        </div>
      }

      @if (successMessage()) {
        <div class="mb-4 flex items-center gap-2 rounded-md bg-success/10 border border-success/20 p-3 text-xs text-success dark:bg-success/20 dark:border-success/30">
          <app-icon name="check" [size]="16" class="shrink-0" />
          <span>{{ successMessage() }}</span>
        </div>
      }

      <form [formGroup]="form" (ngSubmit)="onSubmit()" class="space-y-4">
        <div>
          <label class="mb-1.5 block text-xs font-semibold text-black dark:text-white">
            {{ 'MODAL.CURRENT_PASSWORD' | translate }} <span class="text-danger">*</span>
          </label>
          <input
            type="password"
            formControlName="currentPassword"
            placeholder="••••••••"
            class="w-full h-10 rounded-lg border border-stroke bg-transparent px-3.5 text-sm text-black outline-none transition-colors focus:border-primary dark:border-strokedark dark:bg-meta-4 dark:text-white dark:focus:border-primary"
          />
        </div>

        <div>
          <label class="mb-1.5 block text-xs font-semibold text-black dark:text-white">
            {{ 'MODAL.NEW_PASSWORD' | translate }} <span class="text-danger">*</span>
          </label>
          <input
            type="password"
            formControlName="newPassword"
            placeholder="Tối thiểu 6 ký tự"
            class="w-full h-10 rounded-lg border border-stroke bg-transparent px-3.5 text-sm text-black outline-none transition-colors focus:border-primary dark:border-strokedark dark:bg-meta-4 dark:text-white dark:focus:border-primary"
          />
        </div>

        <div>
          <label class="mb-1.5 block text-xs font-semibold text-black dark:text-white">
            {{ 'MODAL.CONFIRM_PASSWORD' | translate }} <span class="text-danger">*</span>
          </label>
          <input
            type="password"
            formControlName="confirmPassword"
            placeholder="Nhập lại mật khẩu mới"
            class="w-full h-10 rounded-lg border border-stroke bg-transparent px-3.5 text-sm text-black outline-none transition-colors focus:border-primary dark:border-strokedark dark:bg-meta-4 dark:text-white dark:focus:border-primary"
          />
        </div>
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
          {{ isSaving() ? ('MODAL.BTN_SAVING' | translate) : ('MODAL.BTN_CONFIRM_PWD' | translate) }}
        </app-button>
      </div>
    </app-modal>
  `,
})
export class ChangePasswordModalComponent {
  private readonly fb = inject(FormBuilder);
  private readonly userService = inject(UserService);

  readonly targetUser = input<UserDto | null>(null);
  readonly saved = output<void>();
  readonly closed = output<void>();

  readonly isSaving = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly form = this.fb.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', [Validators.required]],
  });

  onSubmit(): void {
    if (this.form.invalid) return;

    const val = this.form.getRawValue();
    if (val.newPassword !== val.confirmPassword) {
      this.errorMessage.set('Mật khẩu xác nhận không khớp.');
      return;
    }

    const user = this.targetUser();
    if (!user) return;

    this.isSaving.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const req: ChangePasswordRequest = {
      userId: user.id,
      currentPassword: val.currentPassword!,
      newPassword: val.newPassword!,
    };

    this.userService.changePassword(user.id, req).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.successMessage.set('Đổi mật khẩu thành công!');
        setTimeout(() => {
          this.saved.emit();
          this.close();
        }, 1200);
      },
      error: (err) => {
        this.isSaving.set(false);
        this.errorMessage.set(err.detail || 'Đổi mật khẩu thất bại. Vui lòng thử lại.');
      },
    });
  }

  close(): void {
    this.closed.emit();
  }
}

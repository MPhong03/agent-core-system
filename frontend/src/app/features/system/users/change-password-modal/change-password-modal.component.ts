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

@Component({
  selector: 'app-change-password-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="modal-backdrop" (click)="close()">
      <div class="modal-card" (click)="$event.stopPropagation()">
        <div class="modal-header">
          <h3>{{ 'MODAL.CHANGE_PWD_TITLE' | translate }} ({{ targetUser()?.email }})</h3>
          <button class="btn-close" (click)="close()">✕</button>
        </div>

        @if (errorMessage()) {
          <div class="alert alert-danger">{{ errorMessage() }}</div>
        }
        @if (successMessage()) {
          <div class="alert alert-success">{{ successMessage() }}</div>
        }

        <form [formGroup]="form" (ngSubmit)="onSubmit()">
          <div class="form-group">
            <label>{{ 'MODAL.CURRENT_PASSWORD' | translate }} *</label>
            <input type="password" formControlName="currentPassword" placeholder="••••••••" />
          </div>

          <div class="form-group">
            <label>{{ 'MODAL.NEW_PASSWORD' | translate }} *</label>
            <input type="password" formControlName="newPassword" placeholder="Tối thiểu 6 ký tự" />
          </div>

          <div class="form-group">
            <label>{{ 'MODAL.CONFIRM_PASSWORD' | translate }} *</label>
            <input
              type="password"
              formControlName="confirmPassword"
              placeholder="Nhập lại mật khẩu mới"
            />
          </div>

          <div class="modal-actions">
            <button type="button" class="btn-secondary" (click)="close()">
              {{ 'MODAL.BTN_CANCEL' | translate }}
            </button>
            <button type="submit" [disabled]="form.invalid || isSaving()" class="btn-primary">
              @if (isSaving()) {
                {{ 'MODAL.BTN_SAVING' | translate }}
              } @else {
                {{ 'MODAL.BTN_CONFIRM_PWD' | translate }}
              }
            </button>
          </div>
        </form>
      </div>
    </div>
  `,
  styles: [
    `
      .modal-backdrop {
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background: rgba(15, 23, 42, 0.6);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 1000;
        backdrop-filter: blur(2px);
      }
      .modal-card {
        background: #ffffff;
        width: 100%;
        max-width: 440px;
        border-radius: 12px;
        padding: 24px;
        box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.2);
      }
      .modal-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 20px;
        border-bottom: 1px solid #f1f5f9;
        padding-bottom: 12px;
      }
      .modal-header h3 {
        margin: 0;
        font-size: 1.15rem;
        color: #0f172a;
      }
      .btn-close {
        background: none;
        border: none;
        font-size: 1.2rem;
        cursor: pointer;
        color: #64748b;
      }
      .form-group {
        margin-bottom: 16px;
      }
      .form-group label {
        display: block;
        margin-bottom: 6px;
        font-size: 0.85rem;
        font-weight: 600;
        color: #334155;
      }
      .form-group input {
        width: 100%;
        padding: 10px 12px;
        border: 1px solid #cbd5e1;
        border-radius: 6px;
        font-size: 0.95rem;
        outline: none;
        box-sizing: border-box;
      }
      .modal-actions {
        display: flex;
        justify-content: flex-end;
        gap: 10px;
        margin-top: 24px;
        padding-top: 16px;
        border-top: 1px solid #f1f5f9;
      }
      .btn-primary {
        background: #2563eb;
        color: #fff;
        border: none;
        padding: 10px 20px;
        border-radius: 6px;
        font-weight: 600;
        cursor: pointer;
      }
      .btn-primary:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
      .btn-secondary {
        background: #f1f5f9;
        color: #334155;
        border: 1px solid #cbd5e1;
        padding: 10px 16px;
        border-radius: 6px;
        font-weight: 600;
        cursor: pointer;
      }
      .alert {
        padding: 10px 14px;
        border-radius: 6px;
        font-size: 0.85rem;
        margin-bottom: 16px;
      }
      .alert-danger {
        background: #fee2e2;
        color: #991b1b;
      }
      .alert-success {
        background: #dcfce7;
        color: #166534;
      }
    `,
  ],
})
export class ChangePasswordModalComponent {
  private readonly fb = inject(FormBuilder);
  private readonly userService = inject(UserService);

  targetUser = input<UserDto | null>(null);
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

    const { currentPassword, newPassword, confirmPassword } = this.form.getRawValue();

    if (newPassword !== confirmPassword) {
      this.errorMessage.set('Mật khẩu mới và xác nhận mật khẩu không trùng khớp.');
      return;
    }

    if (newPassword === currentPassword) {
      this.errorMessage.set('Mật khẩu mới không được trùng với mật khẩu hiện tại.');
      return;
    }

    const user = this.targetUser();
    if (!user) return;

    this.isSaving.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const req: ChangePasswordRequest = {
      userId: user.id,
      currentPassword: currentPassword!,
      newPassword: newPassword!,
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
        this.errorMessage.set(err.detail || 'Đổi mật khẩu không thành công.');
      },
    });
  }

  close(): void {
    this.closed.emit();
  }
}

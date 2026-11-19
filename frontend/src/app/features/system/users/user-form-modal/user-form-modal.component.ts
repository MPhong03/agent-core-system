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

@Component({
  selector: 'app-user-form-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="modal-backdrop" (click)="close()">
      <div class="modal-card" (click)="$event.stopPropagation()">
        <div class="modal-header">
          <h3>
            {{
              isEditMode()
                ? ('MODAL.USER_EDIT_TITLE' | translate)
                : ('MODAL.USER_CREATE_TITLE' | translate)
            }}
          </h3>
          <button class="btn-close" (click)="close()">✕</button>
        </div>

        @if (errorMessage()) {
          <div class="alert alert-danger">{{ errorMessage() }}</div>
        }

        <form [formGroup]="form" (ngSubmit)="onSubmit()">
          <div class="form-group">
            <label>{{ 'MODAL.FULL_NAME' | translate }} *</label>
            <input type="text" formControlName="fullName" placeholder="Nguyễn Văn A" />
          </div>

          <div class="form-group">
            <label>{{ 'MODAL.EMAIL' | translate }} *</label>
            <input
              type="email"
              formControlName="email"
              placeholder="example@mvp.vn"
              [readonly]="isEditMode()"
            />
          </div>

          @if (!isEditMode()) {
            <div class="form-group">
              <label>{{ 'MODAL.PASSWORD' | translate }} *</label>
              <input type="password" formControlName="password" placeholder="Tối thiểu 6 ký tự" />
            </div>
          }

          <div class="form-group">
            <label>{{ 'MODAL.PHONE_NUMBER' | translate }}</label>
            <input type="text" formControlName="phoneNumber" placeholder="0912345678" />
          </div>

          <div class="form-group">
            <label>{{ 'MODAL.ROLES' | translate }} *</label>
            <div class="roles-selection">
              <label class="checkbox-label">
                <input
                  type="checkbox"
                  [checked]="hasRole('SuperAdmin')"
                  (change)="toggleRole('SuperAdmin', $event)"
                />
                SuperAdmin
              </label>
              <label class="checkbox-label">
                <input
                  type="checkbox"
                  [checked]="hasRole('Admin')"
                  (change)="toggleRole('Admin', $event)"
                />
                Admin
              </label>
              <label class="checkbox-label">
                <input
                  type="checkbox"
                  [checked]="hasRole('Employee')"
                  (change)="toggleRole('Employee', $event)"
                />
                Employee
              </label>
            </div>
          </div>

          @if (isEditMode()) {
            <div class="form-group checkbox-group">
              <label class="checkbox-label">
                <input type="checkbox" formControlName="isActive" />
                {{ 'MODAL.IS_ACTIVE' | translate }}
              </label>
            </div>
          }

          <div class="modal-actions">
            <button type="button" class="btn-secondary" (click)="close()">
              {{ 'MODAL.BTN_CANCEL' | translate }}
            </button>
            <button type="submit" [disabled]="form.invalid || isSaving()" class="btn-primary">
              @if (isSaving()) {
                {{ 'MODAL.BTN_SAVING' | translate }}
              } @else {
                {{ 'MODAL.BTN_SAVE' | translate }}
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
        max-width: 480px;
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
        font-size: 1.2rem;
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
      .form-group input[type='text'],
      .form-group input[type='email'],
      .form-group input[type='password'] {
        width: 100%;
        padding: 10px 12px;
        border: 1px solid #cbd5e1;
        border-radius: 6px;
        font-size: 0.95rem;
        outline: none;
        box-sizing: border-box;
      }
      .form-group input[readonly] {
        background-color: #f8fafc;
        color: #64748b;
        cursor: not-allowed;
      }
      .roles-selection {
        display: flex;
        gap: 16px;
      }
      .checkbox-label {
        display: flex;
        align-items: center;
        gap: 6px;
        font-size: 0.9rem;
        cursor: pointer;
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
    `,
  ],
})
export class UserFormModalComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly userService = inject(UserService);

  userToEdit = input<UserDto | null>(null);
  readonly saved = output<void>();
  readonly closed = output<void>();

  readonly isSaving = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly selectedRoles = signal<string[]>(['Employee']);

  readonly form = this.fb.group({
    fullName: ['', [Validators.required, Validators.maxLength(150)]],
    email: ['', [Validators.required, Validators.email]],
    password: [''],
    phoneNumber: [''],
    isActive: [true],
  });

  isEditMode(): boolean {
    return this.userToEdit() !== null;
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

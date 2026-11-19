/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { I18nService } from '../../../core/i18n/i18n.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="login-wrapper">
      <div class="login-box">
        <div class="lang-bar">
          <button
            [class.active]="i18nService.currentLang() === 'vi'"
            (click)="i18nService.setLanguage('vi')"
          >
            🇻🇳 VI
          </button>
          <button
            [class.active]="i18nService.currentLang() === 'en'"
            (click)="i18nService.setLanguage('en')"
          >
            🇬🇧 EN
          </button>
        </div>

        <div class="header">
          <div class="logo">E</div>
          <h2>{{ 'AUTH.LOGIN_TITLE' | translate }}</h2>
          <p>{{ 'AUTH.LOGIN_SUBTITLE' | translate }}</p>
        </div>

        @if (errorMessage()) {
          <div class="alert alert-danger">
            {{ errorMessage() }}
          </div>
        }

        <form [formGroup]="form" (ngSubmit)="onSubmit()">
          <div class="form-group">
            <label>{{ 'AUTH.EMAIL' | translate }}</label>
            <input type="email" formControlName="email" placeholder="name@example.com" />
          </div>

          <div class="form-group">
            <label>{{ 'AUTH.PASSWORD' | translate }}</label>
            <input type="password" formControlName="password" placeholder="••••••••" />
          </div>

          <button type="submit" [disabled]="form.invalid || isLoading()" class="btn-submit">
            @if (isLoading()) {
              {{ 'AUTH.BTN_LOGGING_IN' | translate }}
            } @else {
              {{ 'AUTH.BTN_LOGIN' | translate }}
            }
          </button>
        </form>
      </div>
    </div>
  `,
  styles: [
    `
      .login-wrapper {
        min-height: 100vh;
        display: flex;
        align-items: center;
        justify-content: center;
        background-color: #f1f5f9;
        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
      }
      .login-box {
        width: 100%;
        max-width: 420px;
        padding: 32px;
        background: #ffffff;
        border-radius: 12px;
        box-shadow:
          0 10px 25px -5px rgba(0, 0, 0, 0.1),
          0 8px 10px -6px rgba(0, 0, 0, 0.1);
        position: relative;
      }
      .lang-bar {
        display: flex;
        justify-content: flex-end;
        gap: 6px;
        margin-bottom: 12px;
      }
      .lang-bar button {
        background: #f1f5f9;
        border: 1px solid #cbd5e1;
        padding: 3px 8px;
        border-radius: 4px;
        font-size: 0.75rem;
        cursor: pointer;
      }
      .lang-bar button.active {
        background: #2563eb;
        color: #fff;
        border-color: #2563eb;
      }
      .header {
        text-align: center;
        margin-bottom: 24px;
      }
      .logo {
        width: 48px;
        height: 48px;
        margin: 0 auto 12px;
        border-radius: 10px;
        background: #2563eb;
        color: #fff;
        font-weight: 700;
        font-size: 1.5rem;
        display: flex;
        align-items: center;
        justify-content: center;
      }
      .header h2 {
        margin: 0 0 4px 0;
        color: #0f172a;
        font-size: 1.4rem;
      }
      .header p {
        margin: 0;
        color: #64748b;
        font-size: 0.85rem;
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
        box-sizing: border-box;
        outline: none;
      }
      .form-group input:focus {
        border-color: #2563eb;
        box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.1);
      }
      .btn-submit {
        width: 100%;
        padding: 12px;
        background-color: #2563eb;
        color: #fff;
        border: none;
        border-radius: 6px;
        font-size: 1rem;
        font-weight: 600;
        cursor: pointer;
        margin-top: 8px;
      }
      .btn-submit:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
      .alert {
        padding: 10px 14px;
        border-radius: 6px;
        font-size: 0.85rem;
        margin-bottom: 16px;
      }
      .alert-danger {
        background-color: #fee2e2;
        color: #991b1b;
      }
    `,
  ],
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  readonly i18nService = inject(I18nService);
  private readonly router = inject(Router);

  readonly isLoading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  onSubmit(): void {
    if (this.form.invalid) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const { email, password } = this.form.getRawValue();
    this.authService.login({ email: email!, password: password! }).subscribe({
      next: (res) => {
        this.isLoading.set(false);
        if (res.success) {
          this.router.navigate(['/dashboard']);
        }
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.detail || this.i18nService.translate('AUTH.INVALID_CREDENTIALS'));
      },
    });
  }
}

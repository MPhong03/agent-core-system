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
import { ThemeService } from '../../../core/services/theme.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { AppButtonComponent } from '../../../shared/components/button/button.component';
import { AppIconComponent } from '../../../shared/components/icon/icon.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TranslatePipe,
    AppButtonComponent,
    AppIconComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="relative min-h-screen flex items-center justify-center bg-whiten px-4 py-8 dark:bg-boxdark-2 font-sans transition-colors duration-200">
      <!-- Top Action Controls (Theme & Language) -->
      <div class="absolute top-5 right-5 flex items-center gap-3">
        <!-- Theme Switcher -->
        <button
          type="button"
          (click)="themeService.toggleTheme()"
          class="flex h-9 w-9 items-center justify-center rounded-full border border-stroke bg-white text-body hover:text-black dark:border-strokedark dark:bg-boxdark dark:text-bodydark dark:hover:text-white shadow-card transition-colors"
          [title]="themeService.isDark() ? 'Chuyển sang giao diện Sáng' : 'Chuyển sang giao diện Tối'"
        >
          <app-icon [name]="themeService.isDark() ? 'sun' : 'moon'" [size]="18" />
        </button>

        <!-- Language Switcher -->
        <div class="flex items-center rounded-full border border-stroke bg-white p-0.5 dark:border-strokedark dark:bg-boxdark text-xs font-semibold shadow-card">
          <button
            type="button"
            [class.bg-primary]="i18nService.currentLang() === 'vi'"
            [class.text-white]="i18nService.currentLang() === 'vi'"
            (click)="i18nService.setLanguage('vi')"
            class="rounded-full px-2.5 py-1 text-body dark:text-bodydark transition-all"
          >
            VI
          </button>
          <button
            type="button"
            [class.bg-primary]="i18nService.currentLang() === 'en'"
            [class.text-white]="i18nService.currentLang() === 'en'"
            (click)="i18nService.setLanguage('en')"
            class="rounded-full px-2.5 py-1 text-body dark:text-bodydark transition-all"
          >
            EN
          </button>
        </div>
      </div>

      <!-- Login Card Container -->
      <div class="w-full max-w-md rounded-2xl border border-stroke bg-white shadow-card dark:border-strokedark dark:bg-boxdark p-7 sm:p-9">
        <!-- Brand Header -->
        <div class="text-center mb-7">
          <div class="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-xl bg-primary text-white font-bold text-2xl shadow-sm">
            A
          </div>
          <h2 class="text-xl sm:text-2xl font-bold text-black dark:text-white tracking-tight">
            {{ 'AUTH.LOGIN_TITLE' | translate }}
          </h2>
          <p class="text-xs sm:text-sm text-body dark:text-bodydark mt-1">
            {{ 'AUTH.LOGIN_SUBTITLE' | translate }}
          </p>
        </div>

        <!-- Error Notification Banner -->
        @if (errorMessage()) {
          <div class="mb-5 flex items-center gap-3 rounded-lg bg-danger/10 border border-danger/20 p-3.5 text-xs text-danger dark:bg-danger/20 dark:border-danger/30">
            <app-icon name="close" [size]="16" class="shrink-0 text-danger" />
            <span>{{ errorMessage() }}</span>
          </div>
        }

        <!-- Login Form -->
        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="space-y-4.5">
          <!-- Email Input -->
          <div>
            <label class="mb-1.5 block text-xs font-semibold text-black dark:text-white">
              {{ 'AUTH.EMAIL' | translate }}
            </label>
            <div class="relative">
              <input
                type="email"
                formControlName="email"
                placeholder="name@example.com"
                class="w-full h-11 rounded-lg border border-stroke bg-transparent pl-4 pr-11 text-sm text-black outline-none transition-colors focus:border-primary focus-visible:shadow-none dark:border-strokedark dark:bg-meta-4 dark:text-white dark:focus:border-primary"
              />
              <span class="pointer-events-none absolute inset-y-0 right-0 flex items-center pr-3.5 text-body dark:text-bodydark">
                <app-icon name="mail" [size]="18" />
              </span>
            </div>
            @if (form.get('email')?.touched && form.get('email')?.invalid) {
              <p class="mt-1 text-[11px] text-danger">Email không hợp lệ hoặc để trống.</p>
            }
          </div>

          <!-- Password Input -->
          <div>
            <label class="mb-1.5 block text-xs font-semibold text-black dark:text-white">
              {{ 'AUTH.PASSWORD' | translate }}
            </label>
            <div class="relative">
              <input
                [type]="showPassword() ? 'text' : 'password'"
                formControlName="password"
                placeholder="••••••••"
                class="w-full h-11 rounded-lg border border-stroke bg-transparent pl-4 pr-11 text-sm text-black outline-none transition-colors focus:border-primary focus-visible:shadow-none dark:border-strokedark dark:bg-meta-4 dark:text-white dark:focus:border-primary"
              />
              <button
                type="button"
                (click)="showPassword.set(!showPassword())"
                class="absolute inset-y-0 right-0 flex items-center pr-3.5 text-body hover:text-black dark:text-bodydark dark:hover:text-white transition-colors"
                tabindex="-1"
              >
                <app-icon [name]="showPassword() ? 'eye-slash' : 'eye'" [size]="18" />
              </button>
            </div>
          </div>

          <!-- Submit Button -->
          <div class="pt-2">
            <app-button
              type="submit"
              variant="primary"
              size="lg"
              [fullWidth]="true"
              [loading]="isLoading()"
              [disabled]="form.invalid || isLoading()"
            >
              @if (isLoading()) {
                {{ 'AUTH.BTN_LOGGING_IN' | translate }}
              } @else {
                {{ 'AUTH.BTN_LOGIN' | translate }}
              }
            </app-button>
          </div>
        </form>
      </div>
    </div>
  `,
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  readonly i18nService = inject(I18nService);
  readonly themeService = inject(ThemeService);
  private readonly router = inject(Router);

  readonly isLoading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly showPassword = signal<boolean>(false);

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

/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { I18nService } from '../i18n/i18n.service';
import { ThemeService } from '../services/theme.service';
import { HasRoleDirective } from '../../shared/directives/has-role.directive';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { AppIconComponent } from '../../shared/components/icon/icon.component';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, HasRoleDirective, TranslatePipe, AppIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex h-screen overflow-hidden bg-whiten text-body dark:bg-boxdark-2 dark:text-bodydark font-sans">
      <!-- Mobile Backdrop -->
      @if (isMobileSidebarOpen()) {
        <div
          class="fixed inset-0 z-40 bg-black/50 backdrop-blur-sm lg:hidden transition-opacity"
          (click)="isMobileSidebarOpen.set(false)"
        ></div>
      }

      <!-- Sidebar -->
      <aside
        class="fixed top-0 bottom-0 left-0 z-50 flex flex-col bg-white dark:bg-boxdark border-r border-stroke dark:border-strokedark transition-all duration-300 ease-in-out lg:static lg:translate-x-0"
        [class.w-64]="!isSidebarCollapsed()"
        [class.w-20]="isSidebarCollapsed()"
        [class.translate-x-0]="isMobileSidebarOpen()"
        [class.-translate-x-full]="!isMobileSidebarOpen()"
      >
        <!-- Brand / Logo Area -->
        <div class="flex h-16 items-center justify-between px-5 border-b border-stroke dark:border-strokedark">
          <a routerLink="/dashboard" class="flex items-center gap-3 overflow-hidden">
            <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary text-white font-bold text-lg shadow-sm">
              A
            </div>
            @if (!isSidebarCollapsed()) {
              <div class="flex flex-col">
                <span class="text-base font-bold text-black dark:text-white tracking-wide leading-none">
                  AgentCore
                </span>
                <span class="text-[11px] font-medium text-body dark:text-bodydark mt-1">
                  System Platform
                </span>
              </div>
            }
          </a>

          <!-- Collapse button for desktop -->
          <button
            type="button"
            (click)="toggleSidebarCollapse()"
            class="hidden lg:flex h-8 w-8 items-center justify-center rounded-md text-body hover:bg-gray-2 dark:hover:bg-meta-4 hover:text-black dark:hover:text-white transition-colors"
            [title]="isSidebarCollapsed() ? 'Mở rộng menu' : 'Thu gọn menu'"
          >
            <app-icon [name]="isSidebarCollapsed() ? 'chevron-right' : 'menu'" [size]="18" />
          </button>
        </div>

        <!-- Navigation Menu -->
        <nav class="flex-1 overflow-y-auto px-3 py-4 space-y-1.5 custom-scrollbar" (click)="isMobileSidebarOpen.set(false)">
          <!-- Overview Section -->
          @if (!isSidebarCollapsed()) {
            <div class="px-3 pb-1 pt-2 text-[11px] font-semibold uppercase tracking-wider text-bodydark2">
              {{ 'NAV.OVERVIEW' | translate }}
            </div>
          }
          <a
            routerLink="/dashboard"
            routerLinkActive="bg-primary/10 text-primary dark:bg-primary/20 dark:text-white font-semibold"
            class="flex items-center gap-3 px-3 py-2.5 rounded-md text-sm font-medium text-body dark:text-bodydark hover:bg-gray-2 dark:hover:bg-meta-4 hover:text-black dark:hover:text-white transition-colors"
            [title]="'NAV.DASHBOARD' | translate"
          >
            <app-icon name="dashboard" [size]="20" />
            @if (!isSidebarCollapsed()) {
              <span class="truncate">{{ 'NAV.DASHBOARD' | translate }}</span>
            }
          </a>

          <!-- Workspace Section -->
          @if (!isSidebarCollapsed()) {
            <div class="px-3 pb-1 pt-4 text-[11px] font-semibold uppercase tracking-wider text-bodydark2">
              {{ 'NAV.WORKSPACE' | translate }}
            </div>
          }
          <a
            routerLink="/organization"
            routerLinkActive="bg-primary/10 text-primary dark:bg-primary/20 dark:text-white font-semibold"
            class="flex items-center gap-3 px-3 py-2.5 rounded-md text-sm font-medium text-body dark:text-bodydark hover:bg-gray-2 dark:hover:bg-meta-4 hover:text-black dark:hover:text-white transition-colors"
            [title]="'NAV.ORGANIZATION' | translate"
          >
            <app-icon name="organization" [size]="20" />
            @if (!isSidebarCollapsed()) {
              <span class="truncate">{{ 'NAV.ORGANIZATION' | translate }}</span>
            }
          </a>

          <!-- System Section -->
          @if (!isSidebarCollapsed()) {
            <div class="px-3 pb-1 pt-4 text-[11px] font-semibold uppercase tracking-wider text-bodydark2">
              {{ 'NAV.SYSTEM_SECTION' | translate }}
            </div>
          }

          <a
            routerLink="/system/users"
            routerLinkActive="bg-primary/10 text-primary dark:bg-primary/20 dark:text-white font-semibold"
            class="flex items-center gap-3 px-3 py-2.5 rounded-md text-sm font-medium text-body dark:text-bodydark hover:bg-gray-2 dark:hover:bg-meta-4 hover:text-black dark:hover:text-white transition-colors"
            [title]="'NAV.USERS' | translate"
          >
            <app-icon name="users" [size]="20" />
            @if (!isSidebarCollapsed()) {
              <span class="truncate">{{ 'NAV.USERS' | translate }}</span>
            }
          </a>

          <a
            routerLink="/system/roles"
            routerLinkActive="bg-primary/10 text-primary dark:bg-primary/20 dark:text-white font-semibold"
            class="flex items-center gap-3 px-3 py-2.5 rounded-md text-sm font-medium text-body dark:text-bodydark hover:bg-gray-2 dark:hover:bg-meta-4 hover:text-black dark:hover:text-white transition-colors"
            *appHasRole="['SuperAdmin', 'Admin']"
            [title]="'NAV.ROLES' | translate"
          >
            <app-icon name="roles" [size]="20" />
            @if (!isSidebarCollapsed()) {
              <span class="truncate">{{ 'NAV.ROLES' | translate }}</span>
            }
          </a>

          <a
            routerLink="/system/audit-logs"
            routerLinkActive="bg-primary/10 text-primary dark:bg-primary/20 dark:text-white font-semibold"
            class="flex items-center gap-3 px-3 py-2.5 rounded-md text-sm font-medium text-body dark:text-bodydark hover:bg-gray-2 dark:hover:bg-meta-4 hover:text-black dark:hover:text-white transition-colors"
            *appHasRole="['SuperAdmin']"
            [title]="'NAV.AUDIT_LOGS' | translate"
          >
            <app-icon name="audit" [size]="20" />
            @if (!isSidebarCollapsed()) {
              <span class="truncate">{{ 'NAV.AUDIT_LOGS' | translate }}</span>
            }
          </a>
        </nav>

        <!-- Sidebar Footer / User Profile Card -->
        <div class="border-t border-stroke dark:border-strokedark p-3.5 bg-gray-2/50 dark:bg-meta-4/20">
          <div class="flex items-center justify-between gap-2 overflow-hidden">
            <div class="flex items-center gap-3 overflow-hidden min-w-0">
              <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-primary/20 text-primary dark:bg-primary/30 dark:text-white font-semibold text-xs border border-primary/30">
                {{ userInitials() }}
              </div>
              @if (!isSidebarCollapsed()) {
                <div class="truncate">
                  <div class="truncate text-xs font-semibold text-black dark:text-white">
                    {{ authService.currentUser()?.fullName || 'Người dùng' }}
                  </div>
                  <div class="truncate text-[11px] text-body dark:text-bodydark">
                    {{ activeRole() }}
                  </div>
                </div>
              }
            </div>

            <button
              type="button"
              (click)="authService.logout()"
              class="flex h-8 w-8 shrink-0 items-center justify-center rounded-md text-body hover:bg-danger/10 hover:text-danger dark:text-bodydark dark:hover:bg-danger/20 dark:hover:text-danger transition-colors"
              [title]="'NAV.LOGOUT' | translate"
            >
              <app-icon name="logout" [size]="18" />
            </button>
          </div>
        </div>
      </aside>

      <!-- Main Content Area -->
      <div class="relative flex flex-1 flex-col overflow-y-auto overflow-x-hidden">
        <!-- Topbar Header -->
        <header class="sticky top-0 z-30 flex h-16 w-full items-center justify-between border-b border-stroke bg-white px-4 md:px-6 shadow-card dark:border-strokedark dark:bg-boxdark">
          <div class="flex items-center gap-3">
            <!-- Mobile Menu Toggle -->
            <button
              type="button"
              (click)="isMobileSidebarOpen.set(!isMobileSidebarOpen())"
              class="flex h-9 w-9 items-center justify-center rounded-md border border-stroke text-body hover:bg-gray-2 dark:border-strokedark dark:text-bodydark dark:hover:bg-meta-4 lg:hidden"
            >
              <app-icon name="menu" [size]="20" />
            </button>

            <!-- App Title / Breadcrumb -->
            <div class="font-semibold text-black dark:text-white text-base md:text-lg tracking-tight">
              {{ 'APP.TITLE' | translate }}
            </div>
          </div>

          <!-- Topbar Actions -->
          <div class="flex items-center gap-2 md:gap-3">
            <!-- Dark / Light Mode Toggle -->
            <button
              type="button"
              (click)="themeService.toggleTheme()"
              class="flex h-9 w-9 items-center justify-center rounded-full border border-stroke bg-gray-2 text-body hover:text-black dark:border-strokedark dark:bg-meta-4 dark:text-bodydark dark:hover:text-white transition-colors"
              [title]="themeService.isDark() ? 'Chuyển sang giao diện Sáng' : 'Chuyển sang giao diện Tối'"
            >
              <app-icon [name]="themeService.isDark() ? 'sun' : 'moon'" [size]="18" />
            </button>

            <!-- Language Switcher Pill -->
            <div class="flex items-center rounded-full border border-stroke bg-gray-2 p-0.5 dark:border-strokedark dark:bg-meta-4 text-xs font-semibold">
              <button
                type="button"
                [class.bg-white]="i18nService.currentLang() === 'vi'"
                [class.text-primary]="i18nService.currentLang() === 'vi'"
                [class.shadow-sm]="i18nService.currentLang() === 'vi'"
                [class.dark:bg-boxdark]="i18nService.currentLang() === 'vi'"
                [class.dark:text-white]="i18nService.currentLang() === 'vi'"
                (click)="i18nService.setLanguage('vi')"
                class="rounded-full px-2.5 py-1 text-body dark:text-bodydark transition-all"
                title="Tiếng Việt"
              >
                VI
              </button>
              <button
                type="button"
                [class.bg-white]="i18nService.currentLang() === 'en'"
                [class.text-primary]="i18nService.currentLang() === 'en'"
                [class.shadow-sm]="i18nService.currentLang() === 'en'"
                [class.dark:bg-boxdark]="i18nService.currentLang() === 'en'"
                [class.dark:text-white]="i18nService.currentLang() === 'en'"
                (click)="i18nService.setLanguage('en')"
                class="rounded-full px-2.5 py-1 text-body dark:text-bodydark transition-all"
                title="English"
              >
                EN
              </button>
            </div>

            <!-- Role Indicator Pill -->
            <span class="hidden sm:inline-flex items-center rounded-full bg-primary/10 px-3 py-1 text-xs font-semibold text-primary dark:bg-primary/20 dark:text-secondary">
              {{ activeRole() }}
            </span>
          </div>
        </header>

        <!-- Page Content Viewport -->
        <main class="flex-1 p-4 md:p-6 lg:p-8">
          <router-outlet></router-outlet>
        </main>
      </div>
    </div>
  `,
  styles: [
    `
      .custom-scrollbar::-webkit-scrollbar {
        width: 4px;
      }
      .custom-scrollbar::-webkit-scrollbar-thumb {
        background-color: rgba(156, 163, 175, 0.4);
        border-radius: 9999px;
      }
    `,
  ],
})
export class MainLayoutComponent {
  readonly authService = inject(AuthService);
  readonly i18nService = inject(I18nService);
  readonly themeService = inject(ThemeService);

  readonly isSidebarCollapsed = signal<boolean>(false);
  readonly isMobileSidebarOpen = signal<boolean>(false);

  toggleSidebarCollapse(): void {
    this.isSidebarCollapsed.update((v) => !v);
  }

  userInitials(): string {
    const name = this.authService.currentUser()?.fullName || 'User';
    return name.slice(0, 2).toUpperCase();
  }

  activeRole(): string {
    const roles = this.authService.roles();
    return roles.length > 0 ? roles[0] : 'Employee';
  }
}

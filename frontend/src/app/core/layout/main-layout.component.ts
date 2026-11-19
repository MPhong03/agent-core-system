/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { I18nService } from '../i18n/i18n.service';
import { HasRoleDirective } from '../../shared/directives/has-role.directive';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, HasRoleDirective, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="layout-container">
      <!-- Sidebar -->
      <aside class="sidebar">
        <div class="brand">
          <div class="logo-icon">A</div>
          <span class="logo-text">AgentCore</span>
        </div>

        <nav class="nav-menu">
          <a routerLink="/dashboard" routerLinkActive="active" class="nav-item">
            <span class="nav-icon">📊</span>
            <span>{{ 'NAV.DASHBOARD' | translate }}</span>
          </a>

          <div class="nav-header">{{ 'NAV.SYSTEM_SECTION' | translate }}</div>

          <a routerLink="/system/users" routerLinkActive="active" class="nav-item">
            <span class="nav-icon">👥</span>
            <span>{{ 'NAV.USERS' | translate }}</span>
          </a>

          <a
            routerLink="/system/roles"
            routerLinkActive="active"
            class="nav-item"
            *appHasRole="['SuperAdmin', 'Admin']"
          >
            <span class="nav-icon">🛡️</span>
            <span>{{ 'NAV.ROLES' | translate }}</span>
          </a>

          <a
            routerLink="/system/audit-logs"
            routerLinkActive="active"
            class="nav-item"
            *appHasRole="['SuperAdmin']"
          >
            <span class="nav-icon">📜</span>
            <span>{{ 'NAV.AUDIT_LOGS' | translate }}</span>
          </a>
        </nav>

        <div class="sidebar-footer">
          <div class="user-brief">
            <div class="avatar">{{ userInitials() }}</div>
            <div class="user-meta">
              <div class="name">{{ authService.currentUser()?.fullName || 'Người dùng' }}</div>
              <div class="role">{{ activeRole() }}</div>
            </div>
          </div>
          <button
            (click)="authService.logout()"
            class="btn-logout"
            [title]="'NAV.LOGOUT' | translate"
          >
            🚪
          </button>
        </div>
      </aside>

      <!-- Main Content Area -->
      <div class="content-wrapper">
        <header class="topbar">
          <div class="topbar-title">{{ 'APP.TITLE' | translate }}</div>

          <div class="topbar-actions">
            <!-- Language Switcher Pill -->
            <div class="lang-switcher">
              <button
                [class.active]="i18nService.currentLang() === 'vi'"
                (click)="i18nService.setLanguage('vi')"
                title="Tiếng Việt"
              >
                🇻🇳 VI
              </button>
              <button
                [class.active]="i18nService.currentLang() === 'en'"
                (click)="i18nService.setLanguage('en')"
                title="English"
              >
                🇬🇧 EN
              </button>
            </div>

            <span class="badge version-badge">API v1.0</span>
            <span class="badge role-badge">{{ activeRole() }}</span>
          </div>
        </header>

        <main class="page-content">
          <router-outlet></router-outlet>
        </main>
      </div>
    </div>
  `,
  styles: [
    `
      .layout-container {
        display: flex;
        height: 100vh;
        width: 100vw;
        overflow: hidden;
        font-family:
          -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
        background-color: #f8fafc;
        color: #1e293b;
      }

      .sidebar {
        width: 260px;
        background-color: #0f172a;
        color: #f8fafc;
        display: flex;
        flex-direction: column;
        flex-shrink: 0;
        border-right: 1px solid #1e293b;
      }

      .brand {
        height: 64px;
        display: flex;
        align-items: center;
        padding: 0 20px;
        gap: 12px;
        border-bottom: 1px solid #1e293b;
      }

      .logo-icon {
        width: 36px;
        height: 36px;
        border-radius: 8px;
        background: linear-gradient(135deg, #3b82f6, #1d4ed8);
        display: flex;
        align-items: center;
        justify-content: center;
        font-weight: 700;
        font-size: 1.25rem;
        color: #fff;
      }

      .logo-text {
        font-size: 1.15rem;
        font-weight: 700;
        letter-spacing: 0.5px;
      }

      .nav-menu {
        flex: 1;
        padding: 16px 12px;
        overflow-y: auto;
        display: flex;
        flex-direction: column;
        gap: 4px;
      }

      .nav-header {
        font-size: 0.72rem;
        font-weight: 700;
        color: #64748b;
        padding: 16px 12px 6px;
        letter-spacing: 0.05em;
      }

      .nav-item {
        display: flex;
        align-items: center;
        gap: 12px;
        padding: 10px 12px;
        border-radius: 6px;
        color: #94a3b8;
        text-decoration: none;
        font-size: 0.9rem;
        font-weight: 500;
        transition: all 0.2s ease;
      }

      .nav-item:hover {
        background-color: #1e293b;
        color: #f8fafc;
      }

      .nav-item.active {
        background-color: #2563eb;
        color: #ffffff;
        font-weight: 600;
      }

      .sidebar-footer {
        padding: 16px;
        border-top: 1px solid #1e293b;
        display: flex;
        align-items: center;
        justify-content: space-between;
        background-color: #0b1120;
      }

      .user-brief {
        display: flex;
        align-items: center;
        gap: 10px;
        overflow: hidden;
      }

      .avatar {
        width: 36px;
        height: 36px;
        border-radius: 50%;
        background-color: #3b82f6;
        display: flex;
        align-items: center;
        justify-content: center;
        font-weight: 600;
        color: #fff;
        font-size: 0.85rem;
      }

      .user-meta .name {
        font-size: 0.85rem;
        font-weight: 600;
        white-space: nowrap;
        text-overflow: ellipsis;
        overflow: hidden;
      }

      .user-meta .role {
        font-size: 0.75rem;
        color: #94a3b8;
      }

      .btn-logout {
        background: none;
        border: none;
        font-size: 1.2rem;
        cursor: pointer;
        padding: 6px;
        border-radius: 6px;
        transition: background 0.2s;
      }

      .btn-logout:hover {
        background-color: #1e293b;
      }

      .content-wrapper {
        flex: 1;
        display: flex;
        flex-direction: column;
        overflow: hidden;
      }

      .topbar {
        height: 64px;
        background-color: #ffffff;
        border-bottom: 1px solid #e2e8f0;
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 0 24px;
      }

      .topbar-title {
        font-size: 1rem;
        font-weight: 600;
        color: #334155;
      }

      .topbar-actions {
        display: flex;
        align-items: center;
        gap: 12px;
      }

      .lang-switcher {
        display: flex;
        background-color: #f1f5f9;
        border: 1px solid #cbd5e1;
        border-radius: 6px;
        overflow: hidden;
      }

      .lang-switcher button {
        background: none;
        border: none;
        padding: 4px 10px;
        font-size: 0.8rem;
        font-weight: 600;
        color: #475569;
        cursor: pointer;
        transition: all 0.2s;
      }

      .lang-switcher button.active {
        background-color: #2563eb;
        color: #ffffff;
      }

      .badge {
        padding: 4px 10px;
        border-radius: 9999px;
        font-size: 0.75rem;
        font-weight: 600;
      }

      .version-badge {
        background-color: #e0f2fe;
        color: #0369a1;
      }

      .role-badge {
        background-color: #f1f5f9;
        color: #475569;
      }

      .page-content {
        flex: 1;
        padding: 24px;
        overflow-y: auto;
      }
    `,
  ],
})
export class MainLayoutComponent {
  readonly authService = inject(AuthService);
  readonly i18nService = inject(I18nService);

  userInitials(): string {
    const name = this.authService.currentUser()?.fullName || 'User';
    return name.slice(0, 2).toUpperCase();
  }

  activeRole(): string {
    const roles = this.authService.roles();
    return roles.length > 0 ? roles[0] : 'Employee';
  }
}

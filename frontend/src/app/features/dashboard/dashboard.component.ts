/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { Component, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="dashboard-page">
      <div class="welcome-card">
        <h2>{{ 'DASHBOARD.WELCOME_TITLE' | translate }}</h2>
        <p>{{ 'DASHBOARD.WELCOME_SUB' | translate }}</p>
      </div>

      <div class="stats-grid">
        <div class="stat-card">
          <div class="stat-icon blue">⚡</div>
          <div class="stat-info">
            <span class="stat-label">{{ 'DASHBOARD.CARD_BACKEND' | translate }}</span>
            <span class="stat-value">Modular Monolith</span>
            <span class="stat-desc">Schema [system] on SQL Server</span>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon green">🍃</div>
          <div class="stat-info">
            <span class="stat-label">{{ 'DASHBOARD.CARD_JOBS' | translate }}</span>
            <span class="stat-value">Hangfire + MongoDB</span>
            <span class="stat-desc">Fail-fast connection strings</span>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon purple">🚀</div>
          <div class="stat-info">
            <span class="stat-label">{{ 'DASHBOARD.CARD_API' | translate }}</span>
            <span class="stat-value">v1.0 RESTful</span>
            <span class="stat-desc">RFC 7807 &amp; i18n vi-VN/en-US</span>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon orange">🅰️</div>
          <div class="stat-info">
            <span class="stat-label">{{ 'DASHBOARD.CARD_FRONTEND' | translate }}</span>
            <span class="stat-value">Angular Standalone</span>
            <span class="stat-desc">Signals + OnPush + i18n</span>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .welcome-card {
        background: linear-gradient(135deg, #1e293b, #0f172a);
        color: #fff;
        padding: 24px;
        border-radius: 12px;
        margin-bottom: 24px;
      }
      .welcome-card h2 {
        margin: 0 0 8px 0;
        font-size: 1.5rem;
      }
      .welcome-card p {
        margin: 0;
        color: #94a3b8;
      }
      .stats-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
        gap: 16px;
      }
      .stat-card {
        background: #fff;
        padding: 20px;
        border-radius: 10px;
        border: 1px solid #e2e8f0;
        display: flex;
        align-items: center;
        gap: 16px;
      }
      .stat-icon {
        width: 48px;
        height: 48px;
        border-radius: 10px;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 1.5rem;
      }
      .stat-icon.blue {
        background-color: #dbeafe;
      }
      .stat-icon.green {
        background-color: #dcfce7;
      }
      .stat-icon.purple {
        background-color: #f3e8ff;
      }
      .stat-icon.orange {
        background-color: #ffedd5;
      }
      .stat-info {
        display: flex;
        flex-direction: column;
      }
      .stat-label {
        font-size: 0.75rem;
        color: #64748b;
        font-weight: 600;
        text-transform: uppercase;
      }
      .stat-value {
        font-size: 1.15rem;
        color: #0f172a;
        font-weight: 700;
        margin: 2px 0;
      }
      .stat-desc {
        font-size: 0.75rem;
        color: #94a3b8;
      }
    `,
  ],
})
export class DashboardComponent {}

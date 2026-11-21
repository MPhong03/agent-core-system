/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';

export type BadgeVariant = 'success' | 'warning' | 'danger' | 'info' | 'neutral' | 'primary';
export type BadgeSize = 'sm' | 'md';

@Component({
  selector: 'app-badge',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span [class]="badgeClasses()">
      <ng-content />
    </span>
  `,
})
export class AppBadgeComponent {
  readonly variant = input<BadgeVariant>('primary');
  readonly size = input<BadgeSize>('md');

  readonly badgeClasses = computed(() => {
    const base = 'inline-flex items-center justify-center font-medium rounded-full';

    const sizeMap: Record<BadgeSize, string> = {
      sm: 'text-[11px] px-2 py-0.5',
      md: 'text-xs px-2.5 py-1',
    };

    const variantMap: Record<BadgeVariant, string> = {
      primary: 'bg-primary/10 text-primary dark:bg-primary/20 dark:text-secondary',
      success: 'bg-success/10 text-success dark:bg-success/20 dark:text-meta-3',
      warning: 'bg-warning/10 text-warning dark:bg-warning/20 dark:text-meta-6',
      danger: 'bg-danger/10 text-danger dark:bg-danger/20 dark:text-meta-1',
      info: 'bg-info/10 text-info dark:bg-info/20 dark:text-meta-5',
      neutral: 'bg-gray-2 text-body dark:bg-meta-4 dark:text-bodydark',
    };

    return `${base} ${sizeMap[this.size()]} ${variantMap[this.variant()]}`;
  });
}

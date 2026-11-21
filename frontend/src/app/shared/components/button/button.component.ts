/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';

export type ButtonVariant = 'primary' | 'secondary' | 'danger' | 'outline' | 'ghost';
export type ButtonSize = 'sm' | 'md' | 'lg';

@Component({
  selector: 'app-button',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      [type]="type()"
      [disabled]="isDisabled()"
      [class]="buttonClasses()"
    >
      @if (loading()) {
        <svg
          class="animate-spin -ml-1 mr-2 h-4 w-4 text-current"
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
        >
          <circle
            class="opacity-25"
            cx="12"
            cy="12"
            r="10"
            stroke="currentColor"
            stroke-width="4"
          ></circle>
          <path
            class="opacity-75"
            fill="currentColor"
            d="M4 12a8 8 0 018-8v8H4z"
          ></path>
        </svg>
      }
      <ng-content />
    </button>
  `,
})
export class AppButtonComponent {
  readonly variant = input<ButtonVariant>('primary');
  readonly size = input<ButtonSize>('md');
  readonly loading = input<boolean>(false);
  readonly disabled = input<boolean>(false);
  readonly type = input<'button' | 'submit' | 'reset'>('button');
  readonly fullWidth = input<boolean>(false);

  readonly isDisabled = computed(() => this.disabled() || this.loading());

  readonly buttonClasses = computed(() => {
    const base =
      'inline-flex items-center justify-center font-medium rounded-md transition-all duration-150 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2';
    
    // Sizing
    const sizeMap: Record<ButtonSize, string> = {
      sm: 'px-3 py-1.5 text-xs',
      md: 'px-5 py-2 text-sm',
      lg: 'px-6 py-2.5 text-base',
    };

    // Variants (8-state responsive TailAdmin styling)
    const variantMap: Record<ButtonVariant, string> = {
      primary:
        'bg-primary text-white border border-primary hover:bg-primary-hover active:scale-[0.98] focus-visible:ring-primary disabled:opacity-50 disabled:pointer-events-none shadow-sm',
      secondary:
        'bg-gray-2 text-body dark:bg-meta-4 dark:text-white border border-stroke dark:border-strokedark hover:bg-stroke dark:hover:bg-opacity-80 active:scale-[0.98] focus-visible:ring-stroke disabled:opacity-50 disabled:pointer-events-none',
      danger:
        'bg-danger text-white border border-danger hover:bg-opacity-90 active:scale-[0.98] focus-visible:ring-danger disabled:opacity-50 disabled:pointer-events-none shadow-sm',
      outline:
        'bg-transparent text-primary dark:text-white border border-primary hover:bg-primary hover:text-white active:scale-[0.98] focus-visible:ring-primary disabled:opacity-50 disabled:pointer-events-none',
      ghost:
        'bg-transparent text-body dark:text-bodydark hover:bg-gray-2 dark:hover:bg-meta-4 hover:text-black dark:hover:text-white active:scale-[0.98] focus-visible:ring-stroke disabled:opacity-50 disabled:pointer-events-none',
    };

    const widthClass = this.fullWidth() ? 'w-full' : '';

    return `${base} ${sizeMap[this.size()]} ${variantMap[this.variant()]} ${widthClass}`;
  });
}

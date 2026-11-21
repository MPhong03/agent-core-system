/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-card',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="rounded-sm border border-stroke bg-white shadow-default dark:border-strokedark dark:bg-boxdark transition-colors duration-200"
      [class]="customClass()"
    >
      @if (title() || hasHeaderContent) {
        <div
          class="border-b border-stroke py-4 px-6.5 dark:border-strokedark flex items-center justify-between"
        >
          <div>
            @if (title()) {
              <h3 class="font-medium text-black dark:text-white text-base">
                {{ title() }}
              </h3>
            }
            @if (subtitle()) {
              <p class="text-xs text-body dark:text-bodydark mt-0.5">
                {{ subtitle() }}
              </p>
            }
          </div>
          <div class="flex items-center gap-2">
            <ng-content select="[card-actions]" />
          </div>
        </div>
      }

      <div [class.p-6.5]="!noPadding()" [class.p-0]="noPadding()">
        <ng-content />
      </div>

      <ng-content select="[card-footer]" />
    </div>
  `,
})
export class AppCardComponent {
  readonly title = input<string>();
  readonly subtitle = input<string>();
  readonly noPadding = input<boolean>(false);
  readonly customClass = input<string>('');

  get hasHeaderContent(): boolean {
    return !!this.title() || !!this.subtitle();
  }
}

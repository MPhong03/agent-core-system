/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  HostListener,
  computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppIconComponent } from '../icon/icon.component';

export type ModalSize = 'sm' | 'md' | 'lg' | 'xl';

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [CommonModule, AppIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (isOpen()) {
      <div
        class="fixed inset-0 z-99999 flex items-center justify-center overflow-y-auto overflow-x-hidden bg-black/60 p-4 md:p-6 backdrop-blur-sm transition-opacity duration-200"
        (click)="onBackdropClick($event)"
      >
        <div
          class="relative w-full rounded-sm border border-stroke bg-white shadow-default dark:border-strokedark dark:bg-boxdark animate-in fade-in zoom-in-95 duration-150"
          [class]="modalWidthClass()"
          (click)="$event.stopPropagation()"
        >
          <!-- Header -->
          <div
            class="flex items-center justify-between border-b border-stroke py-4 px-6 dark:border-strokedark"
          >
            <h3 class="font-medium text-black dark:text-white text-base">
              {{ title() }}
            </h3>
            <button
              type="button"
              (click)="close.emit()"
              class="flex h-8 w-8 items-center justify-center rounded-sm text-body hover:bg-gray-2 hover:text-black dark:text-bodydark dark:hover:bg-meta-4 dark:hover:text-white transition-colors"
              aria-label="Close modal"
            >
              <app-icon name="close" [size]="18" />
            </button>
          </div>

          <!-- Body -->
          <div class="p-6">
            <ng-content />
          </div>

          <!-- Footer -->
          <div
            class="border-t border-stroke py-3.5 px-6 dark:border-strokedark flex items-center justify-end gap-3"
          >
            <ng-content select="[modal-footer]" />
          </div>
        </div>
      </div>
    }
  `,
})
export class AppModalComponent {
  readonly isOpen = input<boolean>(false);
  readonly title = input<string>('');
  readonly size = input<ModalSize>('md');

  readonly close = output<void>();

  @HostListener('document:keydown.escape')
  handleEscape(): void {
    if (this.isOpen()) {
      this.close.emit();
    }
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.close.emit();
    }
  }

  readonly modalWidthClass = computed(() => {
    const sizeMap: Record<ModalSize, string> = {
      sm: 'max-w-md',
      md: 'max-w-lg',
      lg: 'max-w-2xl',
      xl: 'max-w-4xl',
    };
    return sizeMap[this.size()];
  });
}

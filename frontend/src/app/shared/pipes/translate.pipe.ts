/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { Pipe, PipeTransform, inject } from '@angular/core';
import { I18nService } from '../../core/i18n/i18n.service';

/**
 * Pipe dịch đa ngôn ngữ theo thời gian thực (Reactive Signal i18n).
 *
 * Ví dụ sử dụng trong template:
 * <h1>{{ 'NAV.USERS' | translate }}</h1>
 */
@Pipe({
  name: 'translate',
  standalone: true,
  pure: false, // Cho phép cập nhật tức thì khi signal trong I18nService thay đổi
})
export class TranslatePipe implements PipeTransform {
  private readonly i18nService = inject(I18nService);

  transform(key: string): string {
    return this.i18nService.translate(key);
  }
}

/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { Directive, Input, TemplateRef, ViewContainerRef, inject, effect } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';

/**
 * Structural directive kiểm tra quyền hạn Role (SuperAdmin, Admin, Employee) để hiển thị/ẩn DOM elements.
 *
 * Ví dụ sử dụng:
 * <button *appHasRole="['SuperAdmin', 'Admin']">Xóa tài khoản</button>
 */
@Directive({
  selector: '[appHasRole]',
  standalone: true,
})
export class HasRoleDirective {
  private readonly templateRef = inject(TemplateRef<any>);
  private readonly viewContainer = inject(ViewContainerRef);
  private readonly authService = inject(AuthService);

  private requiredRoles: string[] = [];

  @Input()
  set appHasRole(roles: string[] | string) {
    this.requiredRoles = Array.isArray(roles) ? roles : [roles];
    this.updateView();
  }

  constructor() {
    // Tự động re-evaluate khi tín hiệu roles của user thay đổi
    effect(() => {
      this.updateView();
    });
  }

  private updateView(): void {
    if (this.requiredRoles.length === 0 || this.authService.hasAnyRole(this.requiredRoles)) {
      if (this.viewContainer.length === 0) {
        this.viewContainer.createEmbeddedView(this.templateRef);
      }
    } else {
      this.viewContainer.clear();
    }
  }
}

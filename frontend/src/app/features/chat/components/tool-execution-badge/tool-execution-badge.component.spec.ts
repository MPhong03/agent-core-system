/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file tool-execution-badge.component.spec.ts — Unit tests cho ToolExecutionBadgeComponent (Sprint 3 - Task 10)
 */

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach } from 'vitest';
import { ToolExecutionBadgeComponent } from './tool-execution-badge.component';

describe('ToolExecutionBadgeComponent', () => {
  let component: ToolExecutionBadgeComponent;
  let fixture: ComponentFixture<ToolExecutionBadgeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ToolExecutionBadgeComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ToolExecutionBadgeComponent);
    component = fixture.componentInstance;
  });

  it('khởi tạo thành công', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('hiển thị thông báo đang thực thi công cụ kèm tên tool', () => {
    fixture.componentRef.setInput('toolName', 'get_weather_forecast');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Đang thực thi công cụ');
    expect(compiled.textContent).toContain('get_weather_forecast');
  });
});

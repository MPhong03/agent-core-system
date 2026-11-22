/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file tool-approval-card.component.spec.ts — Unit tests cho ToolApprovalCardComponent (Sprint 3 - Task 10)
 *
 * ## Mục Tiêu Kiểm Thử
 * Đảm bảo Action Card phê duyệt công cụ hiển thị tên tool, tham số JSON định dạng rõ ràng,
 * bắt sự kiện Phê duyệt / Từ chối, vô hiệu hóa nút bấm khi đang gửi yêu cầu và hiển thị đúng badge trạng thái.
 */

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { ToolApprovalCardComponent } from './tool-approval-card.component';
import { ToolApprovalRequest } from '../../models/chat.models';

describe('ToolApprovalCardComponent', () => {
  let component: ToolApprovalCardComponent;
  let fixture: ComponentFixture<ToolApprovalCardComponent>;

  const sampleApprovalRequest: ToolApprovalRequest = {
    pendingToolCallId: '018f2345-6789-7def-8123-456789abcdef',
    channelId: 'chan-001',
    chatMessageId: 'msg-001',
    toolCallId: 'call_abc123',
    toolName: 'execute_sql_query',
    argumentsJson: '{"query":"SELECT * FROM Users WHERE IsActive = 1","limit":50}',
    expiresAt: '2026-11-21T15:00:00Z',
    status: 'pending',
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ToolApprovalCardComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ToolApprovalCardComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('approvalRequest', sampleApprovalRequest);
  });

  it('khởi tạo thành công component', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('hiển thị tên công cụ và xem trước tham số JSON đã được format', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    // Kiểm tra tên công cụ
    expect(compiled.textContent).toContain('execute_sql_query');

    // Kiểm tra JSON arguments đã được format
    const preElement = compiled.querySelector('pre');
    expect(preElement).toBeTruthy();
    expect(preElement?.textContent).toContain('SELECT * FROM Users WHERE IsActive = 1');
    expect(preElement?.textContent).toContain('"limit": 50');
  });

  it('phát ra sự kiện confirmed kèm pendingToolCallId khi bấm nút Phê duyệt', () => {
    fixture.detectChanges();
    const confirmedSpy = vi.fn();
    component.confirmed.subscribe(confirmedSpy);

    const confirmBtn = fixture.nativeElement.querySelector(
      'button[data-testid="confirm-tool-btn"]',
    ) as HTMLButtonElement;
    expect(confirmBtn).toBeTruthy();

    confirmBtn.click();
    fixture.detectChanges();

    expect(confirmedSpy).toHaveBeenCalledWith('018f2345-6789-7def-8123-456789abcdef');
  });

  it('phát ra sự kiện rejected kèm callId khi bấm nút Từ chối', () => {
    fixture.detectChanges();
    const rejectedSpy = vi.fn();
    component.rejected.subscribe(rejectedSpy);

    const rejectBtn = fixture.nativeElement.querySelector(
      'button[data-testid="reject-tool-btn"]',
    ) as HTMLButtonElement;
    expect(rejectBtn).toBeTruthy();

    rejectBtn.click();
    fixture.detectChanges();

    expect(rejectedSpy).toHaveBeenCalledWith({
      callId: '018f2345-6789-7def-8123-456789abcdef',
    });
  });

  it('vô hiệu hóa các nút hành động khi đang submit (isSubmitting = true)', () => {
    fixture.componentRef.setInput('isSubmitting', true);
    fixture.detectChanges();

    const confirmBtn = fixture.nativeElement.querySelector(
      'button[data-testid="confirm-tool-btn"]',
    ) as HTMLButtonElement;
    const rejectBtn = fixture.nativeElement.querySelector(
      'button[data-testid="reject-tool-btn"]',
    ) as HTMLButtonElement;

    expect(confirmBtn.disabled).toBe(true);
    expect(rejectBtn.disabled).toBe(true);
  });

  it('hiển thị badge trạng thái đã phê duyệt khi status = confirmed', () => {
    fixture.componentRef.setInput('approvalRequest', {
      ...sampleApprovalRequest,
      status: 'confirmed',
    });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Đã phê duyệt');
    expect(compiled.querySelector('button[data-testid="confirm-tool-btn"]')).toBeNull();
  });

  it('hiển thị badge trạng thái đã từ chối khi status = rejected', () => {
    fixture.componentRef.setInput('approvalRequest', {
      ...sampleApprovalRequest,
      status: 'rejected',
    });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Đã từ chối');
    expect(compiled.querySelector('button[data-testid="reject-tool-btn"]')).toBeNull();
  });

  it('nhận diện công cụ execute_python_code và hiển thị khối mã Python chuyên dụng', () => {
    const pythonApprovalRequest: ToolApprovalRequest = {
      pendingToolCallId: '018f2345-6789-7def-8123-python0001',
      channelId: 'chan-001',
      chatMessageId: 'msg-002',
      toolCallId: 'call_py123',
      toolName: 'execute_python_code',
      argumentsJson: JSON.stringify({
        code: 'import math\nprint(math.sqrt(144))',
        script_name: 'calculate_sqrt.py',
        timeout_seconds: 30,
      }),
      expiresAt: '2026-11-21T15:00:00Z',
      status: 'pending',
    };

    fixture.componentRef.setInput('approvalRequest', pythonApprovalRequest);
    fixture.detectChanges();

    expect(component.isPythonExecution()).toBe(true);
    expect(component.pythonCode()).toBe('import math\nprint(math.sqrt(144))');
    expect(component.scriptName()).toBe('calculate_sqrt.py');

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Python Sandbox');
    expect(compiled.textContent).toContain('calculate_sqrt.py');
    expect(compiled.textContent).toContain('import math');
  });
});

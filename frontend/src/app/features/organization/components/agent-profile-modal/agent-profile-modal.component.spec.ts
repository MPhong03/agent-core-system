/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file agent-profile-modal.component.spec.ts — Unit tests cho AgentProfileModalComponent (Sprint 3 - PBI-TOOL-02)
 *
 * ## Mục Tiêu Kiểm Thử
 * - Khởi tạo modal với ToolExecutionMode mặc định (SemiAuto).
 * - Hiển thị 3 lựa chọn chế độ thực thi công cụ (Auto, Semi-Auto, Need Approve).
 * - Tương tác bật/tắt checkbox phân quyền công cụ (Assigned Tools) cho Agent.
 * - Submit tạo Agent Profile mới kèm ToolExecutionMode và gọi gán công cụ.
 * - Hỗ trợ chế độ cấu hình lại công cụ cho Agent đã tồn tại (agentProfileId).
 */

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { AgentProfileModalComponent } from './agent-profile-modal.component';
import { OrganizationStore } from '../../store/organization.store';
import { ToolsService } from '../../../agents/tools/services/tools.service';
import { ToolExecutionMode, ModelProviderType } from '../../models/organization.models';
import { ToolDefinitionDto } from '../../../agents/tools/models/tool.models';
import { ApiResponse } from '../../../../core/models/api-response.model';

describe('AgentProfileModalComponent', () => {
  let component: AgentProfileModalComponent;
  let fixture: ComponentFixture<AgentProfileModalComponent>;

  let mockOrgStore: {
    createAgentProfile: ReturnType<typeof vi.fn>;
  };

  let mockToolsService: {
    tools: ReturnType<typeof signal<ToolDefinitionDto[]>>;
    isLoading: ReturnType<typeof signal<boolean>>;
    loadTools: ReturnType<typeof vi.fn>;
    getAgentTools: ReturnType<typeof vi.fn>;
    assignAgentTools: ReturnType<typeof vi.fn>;
  };

  const sampleTools: ToolDefinitionDto[] = [
    {
      id: 'tool-001',
      name: 'get_weather',
      displayName: 'Tra cứu thời tiết',
      description: 'API tra cứu thời tiết thời gian thực',
      type: 'RestApi',
      endpointUrl: 'https://api.weather.com/v1',
      httpMethod: 'GET',
      headersConfig: '{}',
      parametersSchema: '{"type":"object"}',
      requiresConfirmation: false,
      timeoutSeconds: 30,
      isActive: true,
      createdDate: '2026-11-21T00:00:00Z',
      updatedDate: null,
    },
    {
      id: 'tool-002',
      name: 'send_email',
      displayName: 'Gửi Email Khách Hàng',
      description: 'API gửi email thông báo bảo mật',
      type: 'RestApi',
      endpointUrl: 'https://api.mail.com/send',
      httpMethod: 'POST',
      headersConfig: '{}',
      parametersSchema: '{"type":"object"}',
      requiresConfirmation: true,
      timeoutSeconds: 45,
      isActive: true,
      createdDate: '2026-11-21T00:00:00Z',
      updatedDate: null,
    },
  ];

  beforeEach(async () => {
    mockOrgStore = {
      createAgentProfile: vi.fn().mockReturnValue(
        of({
          success: true,
          data: '018dc3f9-7d00-7b23-a7bb-69a31a980099',
          message: 'Tạo hồ sơ Agent thành công.',
        } as ApiResponse<string>),
      ),
    };

    mockToolsService = {
      tools: signal<ToolDefinitionDto[]>(sampleTools),
      isLoading: signal<boolean>(false),
      loadTools: vi.fn().mockReturnValue(
        of({
          success: true,
          data: sampleTools,
          message: 'OK',
        } as ApiResponse<ToolDefinitionDto[]>),
      ),
      getAgentTools: vi.fn().mockReturnValue(
        of({
          success: true,
          data: [sampleTools[0]],
          message: 'OK',
        } as ApiResponse<ToolDefinitionDto[]>),
      ),
      assignAgentTools: vi.fn().mockReturnValue(
        of({
          success: true,
          data: 'Phân quyền công cụ thành công.',
          message: 'OK',
        } as ApiResponse<string>),
      ),
    };

    await TestBed.configureTestingModule({
      imports: [AgentProfileModalComponent],
      providers: [
        { provide: OrganizationStore, useValue: mockOrgStore },
        { provide: ToolsService, useValue: mockToolsService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AgentProfileModalComponent);
    component = fixture.componentInstance;
  });

  it('khởi tạo thành công với giá trị form mặc định bao gồm ToolExecutionMode.SemiAuto', () => {
    fixture.componentRef.setInput('isOpen', true);
    fixture.detectChanges();

    expect(component).toBeTruthy();
    expect(component.form.get('toolExecutionMode')?.value).toBe(ToolExecutionMode.SemiAuto);
    expect(component.form.get('selectedToolIds')?.value).toEqual([]);
    expect(mockToolsService.loadTools).toHaveBeenCalled();
  });

  it('hiển thị 3 tùy chọn ToolExecutionMode và mô tả gợi ý', () => {
    fixture.componentRef.setInput('isOpen', true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const modeCards = compiled.querySelectorAll('.execution-mode-card');
    expect(modeCards.length).toBe(3);

    expect(compiled.textContent).toContain('Tự trị');
    expect(compiled.textContent).toContain('Bán tự động');
    expect(compiled.textContent).toContain('Nghiêm ngặt');
  });

  it('thay đổi toolExecutionMode khi chọn chế độ thực thi', () => {
    fixture.componentRef.setInput('isOpen', true);
    fixture.detectChanges();

    component.onModeChange(ToolExecutionMode.Auto);
    expect(component.form.get('toolExecutionMode')?.value).toBe(ToolExecutionMode.Auto);

    component.onModeChange(ToolExecutionMode.NeedApprove);
    expect(component.form.get('toolExecutionMode')?.value).toBe(ToolExecutionMode.NeedApprove);
  });

  it('hiển thị danh sách công cụ và cho phép bật/tắt checkbox phân quyền công cụ', () => {
    fixture.componentRef.setInput('isOpen', true);
    fixture.detectChanges();

    expect(component.isToolSelected('tool-001')).toBe(false);

    // Bật tool-001
    component.toggleTool('tool-001');
    expect(component.isToolSelected('tool-001')).toBe(true);
    expect(component.form.get('selectedToolIds')?.value).toEqual(['tool-001']);

    // Bật tiếp tool-002
    component.toggleTool('tool-002');
    expect(component.form.get('selectedToolIds')?.value).toEqual(['tool-001', 'tool-002']);

    // Tắt tool-001
    component.toggleTool('tool-001');
    expect(component.isToolSelected('tool-001')).toBe(false);
    expect(component.form.get('selectedToolIds')?.value).toEqual(['tool-002']);
  });

  it('submit form tạo Agent Profile mới kèm ToolExecutionMode và gọi assignAgentTools', () => {
    fixture.componentRef.setInput('isOpen', true);
    fixture.detectChanges();

    const createdSpy = vi.spyOn(component.created, 'emit');
    const closeSpy = vi.spyOn(component, 'close');

    component.form.patchValue({
      name: 'Agent Test',
      provider: ModelProviderType.Gemini,
      modelName: 'gemini-2.5-flash',
      systemPrompt: 'Bạn là chuyên viên kiểm thử.',
      temperature: 0.7,
      toolExecutionMode: ToolExecutionMode.SemiAuto,
      selectedToolIds: ['tool-001', 'tool-002'],
    });

    component.onSubmit();

    expect(mockOrgStore.createAgentProfile).toHaveBeenCalledWith(
      expect.objectContaining({
        name: 'Agent Test',
        toolExecutionMode: ToolExecutionMode.SemiAuto,
      }),
    );

    expect(mockToolsService.assignAgentTools).toHaveBeenCalledWith(
      '018dc3f9-7d00-7b23-a7bb-69a31a980099',
      ['tool-001', 'tool-002'],
    );

    expect(createdSpy).toHaveBeenCalled();
    expect(closeSpy).toHaveBeenCalled();
  });

  it('hỗ trợ cấu hình lại công cụ cho Agent đã tồn tại khi có agentProfileId', () => {
    fixture.componentRef.setInput('isOpen', true);
    fixture.componentRef.setInput('agentProfileId', 'agent-existing-123');
    fixture.detectChanges();

    // Kiểm tra đã nạp công cụ hiện tại của Agent
    expect(mockToolsService.getAgentTools).toHaveBeenCalledWith('agent-existing-123');

    // Ở chế độ edit, không cho phép đổi ToolExecutionMode
    expect(component.isEditMode).toBe(true);
    component.onModeChange(ToolExecutionMode.Auto);
    expect(component.form.get('toolExecutionMode')?.value).toBe(ToolExecutionMode.SemiAuto);

    component.toggleTool('tool-002');
    component.onSubmit();

    expect(mockToolsService.assignAgentTools).toHaveBeenCalledWith(
      'agent-existing-123',
      expect.arrayContaining(['tool-002']),
    );
  });
});

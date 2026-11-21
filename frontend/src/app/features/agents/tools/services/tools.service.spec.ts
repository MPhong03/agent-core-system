/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file tools.service.spec.ts — Unit tests cho ToolsService (Sprint 3 - PBI-TOOL-01)
 *
 * ## Mục Tiêu Kiểm Thử
 * Kiểm thử việc tương tác REST API quản lý công cụ AI (No-Code Tool Builder):
 * - Tải danh sách công cụ (loadTools) và cập nhật Angular Signals
 * - Truy vấn chi tiết theo ID (getToolById)
 * - Tạo mới (createTool), cập nhật (updateTool), xóa (deleteTool)
 * - Chạy thử nghiệm công cụ đã lưu (testSavedTool) và công cụ ad-hoc (testAdHocTool)
 */

import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ToolsService } from './tools.service';
import {
  ToolDefinitionDto,
  CreateToolRequest,
  UpdateToolRequest,
  TestToolRequest,
  TestAdHocToolRequest,
  ToolTestResultDto,
} from '../models/tool.models';
import { ApiResponse } from '../../../../core/models/api-response.model';
import { environment } from '../../../../../environments/environment';

describe('ToolsService', () => {
  let service: ToolsService;
  let httpTestingController: HttpTestingController;
  const baseUrl = `${environment.apiUrl}/agents/tools`;

  const mockTool: ToolDefinitionDto = {
    id: '018dc3f9-7d00-7b23-a7bb-69a31a980001',
    name: 'get_weather_forecast',
    displayName: 'Dự báo thời tiết',
    description: 'Truy vấn dự báo thời tiết theo địa điểm và ngày',
    type: 'RestApi',
    endpointUrl: 'https://api.weather.com/v1/forecast',
    httpMethod: 'GET',
    headersConfig: '{"X-Api-Key":"••••••••"}',
    parametersSchema: JSON.stringify({
      type: 'object',
      properties: {
        city: { type: 'string', description: 'Tên thành phố' },
      },
      required: ['city'],
    }),
    requiresConfirmation: false,
    timeoutSeconds: 30,
    isActive: true,
    createdDate: '2026-11-21T10:00:00Z',
    updatedDate: null,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ToolsService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(ToolsService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('should be created and have initial signal values', () => {
    expect(service).toBeTruthy();
    expect(service.tools()).toEqual([]);
    expect(service.isLoading()).toBe(false);
    expect(service.selectedTool()).toBeNull();
  });

  it('should load tools and update tools and isLoading signals', () => {
    const mockApiResponse: ApiResponse<ToolDefinitionDto[]> = {
      success: true,
      message: 'Lấy danh sách công cụ thành công.',
      data: [mockTool],
      timestamp: '2026-11-21T10:00:00Z',
    };

    service.loadTools().subscribe((res) => {
      expect(res.success).toBe(true);
      expect(res.data.length).toBe(1);
      expect(res.data[0].name).toBe('get_weather_forecast');
    });

    expect(service.isLoading()).toBe(true);

    const req = httpTestingController.expectOne(baseUrl);
    expect(req.request.method).toBe('GET');
    req.flush(mockApiResponse);

    expect(service.isLoading()).toBe(false);
    expect(service.tools().length).toBe(1);
    expect(service.tools()[0]).toEqual(mockTool);
  });

  it('should handle error when loading tools and reset isLoading', () => {
    service.loadTools().subscribe({
      error: (err) => {
        expect(err).toBeTruthy();
      },
    });

    expect(service.isLoading()).toBe(true);

    const req = httpTestingController.expectOne(baseUrl);
    req.error(new ProgressEvent('error'), { status: 500, statusText: 'Server Error' });

    expect(service.isLoading()).toBe(false);
    expect(service.tools()).toEqual([]);
  });

  it('should get tool by id', () => {
    const mockApiResponse: ApiResponse<ToolDefinitionDto> = {
      success: true,
      message: 'Lấy chi tiết công cụ thành công.',
      data: mockTool,
      timestamp: '2026-11-21T10:00:00Z',
    };

    service.getToolById(mockTool.id).subscribe((res) => {
      expect(res.success).toBe(true);
      expect(res.data.id).toBe(mockTool.id);
    });

    const req = httpTestingController.expectOne(`${baseUrl}/${mockTool.id}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockApiResponse);
  });

  it('should create tool and reload tools list', () => {
    const createReq: CreateToolRequest = {
      name: 'send_slack_message',
      displayName: 'Gửi tin nhắn Slack',
      description: 'Gửi thông báo vào kênh Slack chỉ định',
      type: 'RestApi',
      endpointUrl: 'https://slack.com/api/chat.postMessage',
      httpMethod: 'POST',
      headersConfig: '{"Authorization":"Bearer xoxb-..."}',
      parametersSchema: '{"type":"object"}',
      requiresConfirmation: true,
      timeoutSeconds: 45,
    };

    const createdTool: ToolDefinitionDto = {
      ...mockTool,
      id: '018dc3f9-7d00-7b23-a7bb-69a31a980002',
      name: createReq.name,
      displayName: createReq.displayName,
      description: createReq.description,
      endpointUrl: createReq.endpointUrl,
      httpMethod: createReq.httpMethod,
      requiresConfirmation: createReq.requiresConfirmation,
      timeoutSeconds: createReq.timeoutSeconds,
    };

    const createResponse: ApiResponse<ToolDefinitionDto> = {
      success: true,
      message: 'Tạo định nghĩa công cụ thành công.',
      data: createdTool,
      timestamp: '2026-11-21T10:00:00Z',
    };

    service.createTool(createReq).subscribe((res) => {
      expect(res.success).toBe(true);
      expect(res.data.name).toBe('send_slack_message');
    });

    const req = httpTestingController.expectOne(baseUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(createReq);
    req.flush(createResponse);
  });

  it('should update tool with UpdateToolRequest', () => {
    const updateReq: UpdateToolRequest = {
      displayName: 'Dự báo thời tiết cập nhật',
      description: 'Mô tả mới',
      endpointUrl: 'https://api.weather.com/v2/forecast',
      httpMethod: 'POST',
      headersConfig: null,
      parametersSchema: null,
      requiresConfirmation: true,
      timeoutSeconds: 60,
    };

    const updatedTool: ToolDefinitionDto = {
      ...mockTool,
      displayName: updateReq.displayName,
      description: updateReq.description,
      endpointUrl: updateReq.endpointUrl,
      httpMethod: updateReq.httpMethod,
      requiresConfirmation: updateReq.requiresConfirmation,
      timeoutSeconds: updateReq.timeoutSeconds,
    };

    const updateResponse: ApiResponse<ToolDefinitionDto> = {
      success: true,
      message: 'Cập nhật định nghĩa công cụ thành công.',
      data: updatedTool,
      timestamp: '2026-11-21T10:00:00Z',
    };

    service.updateTool(mockTool.id, updateReq).subscribe((res) => {
      expect(res.success).toBe(true);
      expect(res.data.displayName).toBe('Dự báo thời tiết cập nhật');
    });

    const req = httpTestingController.expectOne(`${baseUrl}/${mockTool.id}`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(updateReq);
    req.flush(updateResponse);
  });

  it('should delete tool and remove it from tools list', () => {
    // Pre-populate tools signal
    service.tools.set([mockTool]);

    const deleteResponse: ApiResponse<string> = {
      success: true,
      message: 'Xóa định nghĩa công cụ thành công.',
      data: 'Xóa định nghĩa công cụ thành công.',
      timestamp: '2026-11-21T10:00:00Z',
    };

    service.deleteTool(mockTool.id).subscribe((res) => {
      expect(res.success).toBe(true);
    });

    const req = httpTestingController.expectOne(`${baseUrl}/${mockTool.id}`);
    expect(req.request.method).toBe('DELETE');
    req.flush(deleteResponse);

    expect(service.tools().some((t) => t.id === mockTool.id)).toBe(false);
  });

  it('should test saved tool with TestToolRequest', () => {
    const testReq: TestToolRequest = {
      arguments: { city: 'Hanoi' },
      argumentsJson: '{"city":"Hanoi"}',
      timeoutSeconds: 15,
    };

    const mockResult: ToolTestResultDto = {
      isSuccess: true,
      statusCode: 200,
      responsePayload: '{"temp": 28, "condition": "Sunny"}',
      durationMs: 142,
      errorMessage: null,
    };

    const testResponse: ApiResponse<ToolTestResultDto> = {
      success: true,
      message: 'Kiểm thử công cụ thành công.',
      data: mockResult,
      timestamp: '2026-11-21T10:00:00Z',
    };

    service.testSavedTool(mockTool.id, testReq).subscribe((res) => {
      expect(res.success).toBe(true);
      expect(res.data.isSuccess).toBe(true);
      expect(res.data.statusCode).toBe(200);
      expect(res.data.durationMs).toBe(142);
    });

    const req = httpTestingController.expectOne(`${baseUrl}/${mockTool.id}/test`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(testReq);
    req.flush(testResponse);
  });

  it('should test ad-hoc tool with TestAdHocToolRequest', () => {
    const adHocReq: TestAdHocToolRequest = {
      endpointUrl: 'https://httpbin.org/get',
      httpMethod: 'GET',
      argumentsJson: '{}',
      timeoutSeconds: 10,
    };

    const mockResult: ToolTestResultDto = {
      isSuccess: true,
      statusCode: 200,
      responsePayload: '{"origin": "1.2.3.4"}',
      durationMs: 95,
      errorMessage: null,
    };

    const testResponse: ApiResponse<ToolTestResultDto> = {
      success: true,
      message: 'Kiểm thử công cụ ad-hoc thành công.',
      data: mockResult,
      timestamp: '2026-11-21T10:00:00Z',
    };

    service.testAdHocTool(adHocReq).subscribe((res) => {
      expect(res.success).toBe(true);
      expect(res.data.statusCode).toBe(200);
    });

    const req = httpTestingController.expectOne(`${baseUrl}/test`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(adHocReq);
    req.flush(testResponse);
  });

  it('should update selectedTool signal', () => {
    service.setSelectedTool(mockTool);
    expect(service.selectedTool()).toEqual(mockTool);

    service.setSelectedTool(null);
    expect(service.selectedTool()).toBeNull();
  });

  it('should get agent tools', () => {
    const agentId = '018dc3f9-7d00-7b23-a7bb-69a31a980099';
    const mockResponse: ApiResponse<ToolDefinitionDto[]> = {
      success: true,
      message: 'OK',
      data: [mockTool],
      timestamp: '2026-11-21T10:00:00Z',
    };

    service.getAgentTools(agentId).subscribe((res) => {
      expect(res.success).toBe(true);
      expect(res.data.length).toBe(1);
    });

    const req = httpTestingController.expectOne(`${environment.apiUrl}/agents/profiles/${agentId}/tools`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('should assign agent tools', () => {
    const agentId = '018dc3f9-7d00-7b23-a7bb-69a31a980099';
    const toolIds = [mockTool.id];
    const mockResponse: ApiResponse<string> = {
      success: true,
      message: 'OK',
      data: 'Success',
      timestamp: '2026-11-21T10:00:00Z',
    };

    service.assignAgentTools(agentId, toolIds).subscribe((res) => {
      expect(res.success).toBe(true);
    });

    const req = httpTestingController.expectOne(`${environment.apiUrl}/agents/profiles/${agentId}/tools`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ toolDefinitionIds: toolIds });
    req.flush(mockResponse);
  });
});

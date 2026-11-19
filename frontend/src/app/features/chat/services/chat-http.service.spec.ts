/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ChatHttpService } from './chat-http.service';
import { ChatMessage } from '../models/chat.models';
import { ApiResponse } from '../../../core/models/api-response.model';

describe('ChatHttpService', () => {
  let service: ChatHttpService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ChatHttpService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(ChatHttpService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should call getMessages with default limit and no before parameter', () => {
    const channelId = 'channel-123';
    const mockMessages: ChatMessage[] = [
      {
        id: 'msg-1',
        channelId,
        senderMemberId: 'member-1',
        senderDisplayName: 'Phong Minh',
        isAgent: false,
        content: 'Hello World',
        callDepth: 0,
        parentMessageId: null,
        mentions: [],
        createdDate: '2026-11-19T10:00:00Z',
      },
    ];

    const mockResponse: ApiResponse<ChatMessage[]> = {
      success: true,
      message: 'OK',
      data: mockMessages,
      timestamp: '2026-11-19T10:00:00Z',
    };

    service.getMessages(channelId).subscribe((res) => {
      expect(res.success).toBe(true);
      expect(res.data.length).toBe(1);
      expect(res.data[0].content).toBe('Hello World');
    });

    const req = httpTestingController.expectOne(
      (request) =>
        request.url === `/api/v1/channels/${channelId}/messages` &&
        request.params.get('limit') === '50' &&
        !request.params.has('before'),
    );

    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('should call getMessages with custom limit and before parameter', () => {
    const channelId = 'channel-456';
    const beforeId = 'msg-marker-999';

    service.getMessages(channelId, 25, beforeId).subscribe();

    const req = httpTestingController.expectOne(
      (request) =>
        request.url === `/api/v1/channels/${channelId}/messages` &&
        request.params.get('limit') === '25' &&
        request.params.get('before') === beforeId,
    );

    expect(req.request.method).toBe('GET');
    req.flush({
      success: true,
      message: 'OK',
      data: [],
      timestamp: '2026-11-19T10:00:00Z',
    });
  });
});

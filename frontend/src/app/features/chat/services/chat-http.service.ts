/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file chat-http.service.ts — Dịch vụ HTTP truy vấn lịch sử tin nhắn kênh chat (Sprint 1 - PBI-CHAT-01)
 *
 * ## Vai trò Kiến trúc
 * Cung cấp các thao tác truy vấn lịch sử tin nhắn trong kênh làm việc theo phân trang và mốc thời gian qua REST API.
 *
 * ## Ràng buộc Kỹ thuật & Anti-AI-Slop (RULE-01, RULE-02, RULE-06)
 * 🔴 Phân trang lùi (Cursor-based Pagination): Tham số `before` nhận ID tin nhắn mốc để lấy 50 tin nhắn cũ hơn.
 * 🔴 RULE-02: Sử dụng `environment.apiUrl`, không hardcode tiền tố domain hoặc port.
 * ⛔ RULE-06: Tuyệt đối không dùng `catchError` nuốt lỗi trả về danh sách rỗng giả mạo.
 * ⇒ Luồng Observable trả về `ApiResponse<ChatMessage[]>` bọc danh sách tin nhắn từ server.
 */

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';
import { ChatMessage } from '../models/chat.models';

@Injectable({
  providedIn: 'root',
})
export class ChatHttpService {
  // ── Injections & Cấu hình ──
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  // ── Truy Vấn Lịch Sử Tin Nhắn ──

  /**
   * Truy vấn danh sách tin nhắn trong một kênh làm việc với phân trang theo mốc tin nhắn.
   *
   * @param channelId Mã định danh kênh làm việc.
   * @param limit Số lượng tin nhắn tối đa cần lấy (mặc định 50).
   * @param before Mã định danh tin nhắn mốc để phân trang lùi (tùy chọn).
   * @returns Luồng Observable chứa danh sách tin nhắn dạng ApiResponse<ChatMessage[]>.
   */
  getMessages(
    channelId: string,
    limit: number = 50,
    before?: string,
  ): Observable<ApiResponse<ChatMessage[]>> {
    let params = new HttpParams().set('limit', limit.toString());

    if (before && before.trim().length > 0) {
      params = params.set('before', before.trim());
    }

    return this.http.get<ApiResponse<ChatMessage[]>>(`${this.apiUrl}/channels/${channelId}/messages`, {
      params,
    });
  }
}

/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponse } from '../../../core/models/api-response.model';
import { ChatMessage } from '../models/chat.models';

/**
 * Dịch vụ HTTP tương tác với REST API phân hệ Chat.
 *
 * <remarks>
 * <para><strong>What:</strong> Cung cấp các thao tác truy vấn lịch sử tin nhắn trong kênh làm việc theo phân trang và mốc thời gian.</para>
 * <para><strong>When:</strong> Khách hàng (Angular Client) cần nạp dữ liệu lịch sử hội thoại khi người dùng truy cập hoặc cuộn ngược trang trong kênh làm việc.</para>
 * <para><strong>Why:</strong> Đảm bảo phân trang tối ưu hiệu năng, giảm thiểu lượng dữ liệu truyền tải và hỗ trợ nạp tin nhắn theo mốc thời gian.</para>
 * </remarks>
 */
@Injectable({
  providedIn: 'root',
})
export class ChatHttpService {
  private readonly http = inject(HttpClient);

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

    return this.http.get<ApiResponse<ChatMessage[]>>(`/api/v1/channels/${channelId}/messages`, {
      params,
    });
  }
}

/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file organization.store.ts — Quản lý trạng thái phân hệ Organization bằng Angular Signals (Sprint 0 - PBI-ORG-01)
 *
 * ## Quản Trị Trạng Thái Bằng Angular Signals
 * Lưu trữ tập trung trạng thái cây tổ chức (Phòng ban -> Kênh nhóm -> Thành viên) và danh mục Hồ sơ Agent AI.
 * Đóng vai trò Single Source of Truth (SSOT) cho toàn bộ các UI components trong module Organization.
 *
 * ## Ràng buộc Kỹ thuật & Luồng Dữ liệu
 * 🔴 Bất Biến State: Cập nhật state bất biến qua `.set()` hoặc `.update()`, cấm mutate trực tiếp mảng/đối tượng bên trong Signal.
 * ⛔ RULE-06: Tuyệt đối không nuốt lỗi; mọi thất bại mạng/API đều được đẩy vào Signal `error` và tắt cờ `loading`.
 * ⇒ Chọn phòng ban (`selectDepartment`) ⇒ Tự động reset kênh được chọn, dọn dẹp danh sách thành viên và tải kênh mới.
 * ⇒ Tạo kênh mới thành công ⇒ Tải lại danh sách kênh kèm làm mới phòng ban để cập nhật số lượng `channelCount`.
 * ⇒ Gán thành viên mới thành công ⇒ Làm mới danh sách thành viên kênh và cập nhật `memberCount` của kênh.
 */

import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { ApiResponse } from '../../../core/models/api-response.model';
import {
  Department,
  TeamChannel,
  Member,
  AgentProfile,
  MemberType,
  CreateDepartmentRequest,
  CreateChannelRequest,
  CreateAgentProfileRequest,
  AssignMemberRequest,
} from '../models/organization.models';
import { OrganizationService } from '../services/organization.service';

@Injectable({
  providedIn: 'root',
})
export class OrganizationStore {
  // ── Injections ──
  private readonly orgService = inject(OrganizationService);

  // ── State Signals ──
  readonly departments = signal<Department[]>([]);
  readonly selectedDepartment = signal<Department | null>(null);
  readonly channels = signal<TeamChannel[]>([]);
  readonly selectedChannel = signal<TeamChannel | null>(null);
  readonly channelMembers = signal<Member[]>([]);
  readonly agentProfiles = signal<AgentProfile[]>([]);
  readonly loading = signal<boolean>(false);
  readonly error = signal<string | null>(null);

  // ── Computed Selectors ──
  readonly humanMembers = computed(() =>
    this.channelMembers().filter((m) => m.type === MemberType.Human),
  );

  readonly agentMembers = computed(() =>
    this.channelMembers().filter((m) => m.type === MemberType.Agent),
  );

  readonly totalDepartmentCount = computed(() => this.departments().length);

  readonly totalChannelCount = computed(() => this.channels().length);

  // ── Actions (Tải dữ liệu & Đồng bộ State) ──

  /**
   * Nạp danh sách tất cả phòng ban từ API.
   * AI-NOTE: Giữ nguyên phòng ban đang được chọn nếu vẫn tồn tại trong danh sách mới.
   */
  loadDepartments(): void {
    this.loading.set(true);
    this.error.set(null);

    this.orgService.getDepartments().subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.success && res.data) {
          this.departments.set(res.data);
          const current = this.selectedDepartment();
          if (current) {
            const updated = res.data.find((d) => d.id === current.id);
            if (updated) {
              this.selectedDepartment.set(updated);
            }
          }
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.detail || err?.message || 'Không thể tải danh sách phòng ban');
      },
    });
  }

  /**
   * Chọn một phòng ban làm tiêu điểm làm việc.
   * ⇒ Kéo theo reset kênh được chọn và nạp danh sách kênh thuộc phòng ban này.
   */
  selectDepartment(dept: Department): void {
    this.selectedDepartment.set(dept);
    this.selectedChannel.set(null);
    this.channelMembers.set([]);
    this.loadChannels(dept.id);
  }

  /**
   * Nạp danh sách kênh làm việc của một phòng ban.
   */
  loadChannels(deptId: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.orgService.getChannels(deptId).subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.success && res.data) {
          this.channels.set(res.data);
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.detail || err?.message || 'Không thể tải danh sách kênh nhóm');
      },
    });
  }

  /**
   * Chọn kênh làm việc hiện tại và nạp danh sách thành viên trong kênh.
   */
  selectChannel(channel: TeamChannel): void {
    this.selectedChannel.set(channel);
    this.loadChannelMembers(channel.id);
  }

  /**
   * Nạp danh sách thành viên (con người và Agent AI) của một kênh.
   */
  loadChannelMembers(channelId: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.orgService.getChannelMembers(channelId).subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.success && res.data) {
          this.channelMembers.set(res.data);
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.detail || err?.message || 'Không thể tải danh sách thành viên');
      },
    });
  }

  /**
   * Nạp danh mục toàn bộ hồ sơ cấu hình Agent AI.
   */
  loadAgentProfiles(): void {
    this.loading.set(true);
    this.error.set(null);

    this.orgService.getAgentProfiles().subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.success && res.data) {
          this.agentProfiles.set(res.data);
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.detail || err?.message || 'Không thể tải hồ sơ Agent');
      },
    });
  }

  // ── Actions (Thao tác Thêm / Gán Dữ liệu) ──

  /**
   * Tạo phòng ban mới và tự động nạp lại danh sách phòng ban.
   */
  createDepartment(req: CreateDepartmentRequest): Observable<ApiResponse<string>> {
    this.loading.set(true);
    return this.orgService.createDepartment(req).pipe(
      tap({
        next: () => {
          this.loading.set(false);
          this.loadDepartments();
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(err?.detail || err?.message || 'Không thể tạo phòng ban');
        },
      }),
    );
  }

  /**
   * Tạo kênh làm việc mới thuộc phòng ban và làm mới danh sách kênh & số lượng kênh phòng ban.
   */
  createChannel(deptId: string, req: CreateChannelRequest): Observable<ApiResponse<string>> {
    this.loading.set(true);
    return this.orgService.createChannel(deptId, req).pipe(
      tap({
        next: () => {
          this.loading.set(false);
          this.loadChannels(deptId);
          this.loadDepartments(); // Refresh channelCount phòng ban
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(err?.detail || err?.message || 'Không thể tạo kênh nhóm');
        },
      }),
    );
  }

  /**
   * Tạo hồ sơ cấu hình Agent AI mới và làm mới danh mục Agent profiles.
   */
  createAgentProfile(req: CreateAgentProfileRequest): Observable<ApiResponse<string>> {
    this.loading.set(true);
    return this.orgService.createAgentProfile(req).pipe(
      tap({
        next: () => {
          this.loading.set(false);
          this.loadAgentProfiles();
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(err?.detail || err?.message || 'Không thể tạo hồ sơ Agent');
        },
      }),
    );
  }

  /**
   * Gán thành viên mới hoặc Agent vào kênh làm việc và đồng bộ lại danh sách thành viên kênh.
   */
  assignMember(channelId: string, req: AssignMemberRequest): Observable<ApiResponse<string>> {
    this.loading.set(true);
    return this.orgService.assignMember(channelId, req).pipe(
      tap({
        next: () => {
          this.loading.set(false);
          this.loadChannelMembers(channelId);
          const dept = this.selectedDepartment();
          if (dept) {
            this.loadChannels(dept.id); // Refresh memberCount kênh
          }
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(err?.detail || err?.message || 'Không thể gán thành viên vào kênh');
        },
      }),
    );
  }
}

/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file organization.service.ts — Dịch vụ HTTP gọi REST API quản lý phòng ban, kênh và Agent profile (Sprint 0 - PBI-ORG-01)
 *
 * ## Vai trò Kiến trúc
 * Cung cấp tầng giao tiếp HTTP giữa Angular Client và Web API ASP.NET Core cho phân hệ Organization.
 *
 * ## Ràng buộc Kỹ thuật & Anti-AI-Slop (RULE-01, RULE-02, RULE-04, RULE-06)
 * 🔴 RULE-04: Toàn bộ dữ liệu trả về đều từ REST API thực tế, không dùng mock data hoặc json tĩnh.
 * 🔴 RULE-02: Base URL được nạp động từ `environment.apiUrl`, tuyệt đối không hardcode IP hay domain.
 * ⛔ RULE-06: Không dùng `catchError(() => of([]))` nuốt lỗi tại Service; để Store và UI xử lý ngoại lệ.
 * ⇒ Luôn bọc kiểu trả về dưới dạng `Observable<ApiResponse<T>>` theo đúng envelope chuẩn BE.
 */

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';
import {
  Department,
  TeamChannel,
  Member,
  AgentProfile,
  CreateDepartmentRequest,
  CreateChannelRequest,
  CreateAgentProfileRequest,
  AssignMemberRequest,
} from '../models/organization.models';

@Injectable({
  providedIn: 'root',
})
export class OrganizationService {
  // ── Injections & Cấu hình ──
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  // ── API Phòng Ban (Departments) ──

  /**
   * Truy vấn danh sách tất cả phòng ban trong tổ chức.
   *
   * @returns Luồng Observable chứa danh sách Department.
   */
  getDepartments(): Observable<ApiResponse<Department[]>> {
    return this.http.get<ApiResponse<Department[]>>(`${this.apiUrl}/departments`);
  }

  /**
   * Khởi tạo phòng ban mới.
   *
   * @param req Dữ liệu thông tin phòng ban cần tạo.
   * @returns Luồng Observable chứa ID của phòng ban mới tạo (UUIDv7 string).
   */
  createDepartment(req: CreateDepartmentRequest): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.apiUrl}/departments`, req);
  }

  // ── API Kênh Nhóm (Channels) ──

  /**
   * Lấy danh sách các kênh làm việc trực thuộc một phòng ban cụ thể.
   *
   * @param departmentId Mã định danh phòng ban.
   * @returns Luồng Observable chứa danh sách kênh nhóm TeamChannel.
   */
  getChannels(departmentId: string): Observable<ApiResponse<TeamChannel[]>> {
    return this.http.get<ApiResponse<TeamChannel[]>>(
      `${this.apiUrl}/departments/${departmentId}/channels`,
    );
  }

  /**
   * Khởi tạo kênh làm việc mới thuộc một phòng ban.
   *
   * @param departmentId Mã định danh phòng ban cha.
   * @param req Thông tin kênh cần tạo.
   * @returns Luồng Observable chứa ID kênh mới tạo.
   */
  createChannel(
    departmentId: string,
    req: CreateChannelRequest,
  ): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(
      `${this.apiUrl}/departments/${departmentId}/channels`,
      req,
    );
  }

  // ── API Hồ Sơ Agent (Agent Profiles) ──

  /**
   * Lấy danh mục tất cả hồ sơ cấu hình Agent AI có trong hệ thống.
   *
   * @returns Luồng Observable chứa danh sách AgentProfile.
   */
  getAgentProfiles(): Observable<ApiResponse<AgentProfile[]>> {
    return this.http.get<ApiResponse<AgentProfile[]>>(`${this.apiUrl}/agent-profiles`);
  }

  /**
   * Tạo mới một hồ sơ cấu hình Agent AI.
   *
   * @param req Cấu hình Agent (Tên, Provider, Model, System Prompt, Temperature).
   * @returns Luồng Observable chứa ID hồ sơ Agent mới tạo.
   */
  createAgentProfile(req: CreateAgentProfileRequest): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.apiUrl}/agent-profiles`, req);
  }

  // ── API Thành Viên Kênh (Members) ──

  /**
   * Lấy danh sách thành viên (con người & AI) trong một kênh làm việc.
   *
   * @param channelId Mã định danh kênh.
   * @returns Luồng Observable chứa danh sách thành viên Member.
   */
  getChannelMembers(channelId: string): Observable<ApiResponse<Member[]>> {
    return this.http.get<ApiResponse<Member[]>>(`${this.apiUrl}/channels/${channelId}/members`);
  }

  /**
   * Gán thành viên mới (hoặc Agent AI) vào kênh làm việc.
   *
   * @param channelId Mã định danh kênh đích.
   * @param req Dữ liệu thành viên cần gán.
   * @returns Luồng Observable chứa ID bản ghi thành viên Member.
   */
  assignMember(
    channelId: string,
    req: AssignMemberRequest,
  ): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(
      `${this.apiUrl}/channels/${channelId}/members`,
      req,
    );
  }
}

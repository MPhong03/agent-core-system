/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
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
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  getDepartments(): Observable<ApiResponse<Department[]>> {
    return this.http.get<ApiResponse<Department[]>>(`${this.apiUrl}/departments`);
  }

  createDepartment(req: CreateDepartmentRequest): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.apiUrl}/departments`, req);
  }

  getChannels(departmentId: string): Observable<ApiResponse<TeamChannel[]>> {
    return this.http.get<ApiResponse<TeamChannel[]>>(
      `${this.apiUrl}/departments/${departmentId}/channels`,
    );
  }

  createChannel(
    departmentId: string,
    req: CreateChannelRequest,
  ): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(
      `${this.apiUrl}/departments/${departmentId}/channels`,
      req,
    );
  }

  getAgentProfiles(): Observable<ApiResponse<AgentProfile[]>> {
    return this.http.get<ApiResponse<AgentProfile[]>>(`${this.apiUrl}/agent-profiles`);
  }

  createAgentProfile(req: CreateAgentProfileRequest): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.apiUrl}/agent-profiles`, req);
  }

  getChannelMembers(channelId: string): Observable<ApiResponse<Member[]>> {
    return this.http.get<ApiResponse<Member[]>>(`${this.apiUrl}/channels/${channelId}/members`);
  }

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

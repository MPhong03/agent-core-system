/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
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
  private readonly orgService = inject(OrganizationService);

  // State Signals
  readonly departments = signal<Department[]>([]);
  readonly selectedDepartment = signal<Department | null>(null);
  readonly channels = signal<TeamChannel[]>([]);
  readonly selectedChannel = signal<TeamChannel | null>(null);
  readonly channelMembers = signal<Member[]>([]);
  readonly agentProfiles = signal<AgentProfile[]>([]);
  readonly loading = signal<boolean>(false);
  readonly error = signal<string | null>(null);

  // Computed Selectors
  readonly humanMembers = computed(() =>
    this.channelMembers().filter((m) => m.type === MemberType.Human),
  );

  readonly agentMembers = computed(() =>
    this.channelMembers().filter((m) => m.type === MemberType.Agent),
  );

  readonly totalDepartmentCount = computed(() => this.departments().length);

  readonly totalChannelCount = computed(() => this.channels().length);

  // Actions
  loadDepartments(): void {
    this.loading.set(true);
    this.error.set(null);

    this.orgService.getDepartments().subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.success && res.data) {
          this.departments.set(res.data);
          // If previous selectedDepartment exists, keep or refresh it
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

  selectDepartment(dept: Department): void {
    this.selectedDepartment.set(dept);
    this.selectedChannel.set(null);
    this.channelMembers.set([]);
    this.loadChannels(dept.id);
  }

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

  selectChannel(channel: TeamChannel): void {
    this.selectedChannel.set(channel);
    this.loadChannelMembers(channel.id);
  }

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

  createChannel(deptId: string, req: CreateChannelRequest): Observable<ApiResponse<string>> {
    this.loading.set(true);
    return this.orgService.createChannel(deptId, req).pipe(
      tap({
        next: () => {
          this.loading.set(false);
          this.loadChannels(deptId);
          this.loadDepartments(); // To refresh channelCount
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(err?.detail || err?.message || 'Không thể tạo kênh nhóm');
        },
      }),
    );
  }

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

  assignMember(channelId: string, req: AssignMemberRequest): Observable<ApiResponse<string>> {
    this.loading.set(true);
    return this.orgService.assignMember(channelId, req).pipe(
      tap({
        next: () => {
          this.loading.set(false);
          this.loadChannelMembers(channelId);
          const dept = this.selectedDepartment();
          if (dept) {
            this.loadChannels(dept.id); // Refresh memberCount
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

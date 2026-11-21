/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file organization-shell.component.ts — Shell hiển thị tổng quan cấu trúc phòng ban, kênh nhóm và thành viên (Sprint 0 - PBI-ORG-01)
 *
 * ## Kiến Trúc Giao Diện 2 Cột (Master-Detail Shell)
 * Cột trái (Sidebar Panel): Cây phân cấp Phòng ban (Department) và Kênh nhóm (TeamChannel).
 * Cột phải (Main Panel): Chi tiết kênh được chọn, tích hợp 2 tab chuyển đổi mượt mà:
 * - Tab 'chat': Phòng chat thời gian thực (ChatRoomComponent) giao tiếp với AI Agent qua SignalR.
 * - Tab 'members': Thống kê và danh sách phân bổ nhân sự (Con người & Agent AI).
 *
 * ## Ràng buộc Kỹ thuật & Anti-AI-Slop (RULE-01, RULE-05, RULE-06)
 * 🔴 OnPush: Áp dụng ChangeDetectionStrategy.OnPush, phối hợp hoàn hảo với Signal Store.
 * ⛔ Không thao tác trực tiếp với DOM; mọi trạng thái modal, tab, toast đều quản lý qua Signals.
 * ⇒ Chọn kênh mới (`onSelectChannel`) ⇒ Chuyển tab về `'chat'` và kích hoạt nạp tin nhắn/thành viên kênh.
 * ⇒ Đổi loại thành viên (Human ↔ Agent) ⇒ Tự động điền thông tin Agent đầu tiên nếu có để tăng tốc độ thao tác.
 * AI-NOTE: Toast notification tự hủy sau 3.5 giây thông qua setTimeout.
 */

import {
  Component,
  OnInit,
  ChangeDetectionStrategy,
  inject,
  signal,
  computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import {
  Department,
  TeamChannel,
  Member,
  MemberType,
  AgentProfile,
  CreateDepartmentRequest,
  CreateChannelRequest,
  AssignMemberRequest,
} from '../../models/organization.models';
import { OrganizationStore } from '../../store/organization.store';
import { AgentProfileModalComponent } from '../../components/agent-profile-modal/agent-profile-modal.component';
import { ChatRoomComponent } from '../../../chat/components/chat-room/chat-room.component';
import { KanbanBoardComponent } from '../../../tasks/components/kanban-board/kanban-board.component';

@Component({
  selector: 'app-organization-shell',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    AgentProfileModalComponent,
    ChatRoomComponent,
    KanbanBoardComponent,
  ],
  templateUrl: './organization-shell.component.html',
  styleUrls: ['./organization-shell.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrganizationShellComponent implements OnInit {
  // ── Injections ──
  readonly store = inject(OrganizationStore);
  private readonly fb = inject(FormBuilder);

  readonly MemberType = MemberType;

  // ── UI State Signals & Tabs ──
  /**
   * Tab điều hướng trong giao diện kênh: 'chat' (Phòng chat realtime), 'kanban' (Bảng công việc), 'members' (Danh sách thành viên).
   */
  readonly activeChannelTab = signal<'chat' | 'kanban' | 'members'>('chat');

  readonly channelMembersForTasks = computed(() =>
    this.store.channelMembers().map((m) => ({
      id: m.id,
      displayName: m.displayName,
      isAgent: m.type === MemberType.Agent,
    })),
  );

  // Trạng thái bật/tắt các modal
  readonly isAgentModalOpen = signal<boolean>(false);
  readonly selectedAgentForConfig = signal<AgentProfile | null>(null);
  readonly selectedAgentIdForConfig = signal<string | undefined>(undefined);
  readonly isCreateDeptModalOpen = signal<boolean>(false);
  readonly isCreateChannelModalOpen = signal<boolean>(false);
  readonly isAssignMemberModalOpen = signal<boolean>(false);

  // Thông điệp Toast thành công
  readonly successMessage = signal<string | null>(null);

  // ── Forms Định Nghĩa ──
  readonly deptForm = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', [Validators.maxLength(500)]],
  });

  readonly channelForm = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', [Validators.maxLength(500)]],
  });

  readonly memberForm = this.fb.group({
    type: [MemberType.Agent, [Validators.required]],
    displayName: ['', [Validators.required, Validators.maxLength(100)]],
    userId: [''],
    agentProfileId: [''],
  });

  // ── Vòng Đời Lifecycle (ngOnInit) ──
  ngOnInit(): void {
    this.store.loadDepartments();
    this.store.loadAgentProfiles();
  }

  // ── Điều Hướng Chọn Phòng Ban & Kênh ──

  /**
   * Chọn phòng ban và kích hoạt tải danh sách kênh thuộc phòng ban đó.
   */
  onSelectDepartment(dept: Department): void {
    this.store.selectDepartment(dept);
  }

  /**
   * Chọn kênh làm việc, tự động chuyển về tab chat và tải thành viên kênh.
   * ⇒ Chọn kênh ⇒ reset activeChannelTab về 'chat'.
   */
  onSelectChannel(channel: TeamChannel): void {
    this.activeChannelTab.set('chat');
    this.store.selectChannel(channel);
  }

  // ── Modal Tạo Phòng Ban (Department) ──

  openCreateDeptModal(): void {
    this.deptForm.reset({ name: '', description: '' });
    this.isCreateDeptModalOpen.set(true);
  }

  closeCreateDeptModal(): void {
    this.isCreateDeptModalOpen.set(false);
  }

  submitCreateDept(): void {
    if (this.deptForm.invalid) {
      this.deptForm.markAllAsTouched();
      return;
    }

    const val = this.deptForm.value;
    const req: CreateDepartmentRequest = {
      name: val.name!.trim(),
      description: val.description?.trim() || undefined,
    };

    this.store.createDepartment(req).subscribe({
      next: () => {
        this.closeCreateDeptModal();
        this.showToast('Phòng ban đã được tạo thành công!');
      },
    });
  }

  // ── Modal Tạo Kênh Nhóm (TeamChannel) ──

  openCreateChannelModal(): void {
    this.channelForm.reset({ name: '', description: '' });
    this.isCreateChannelModalOpen.set(true);
  }

  closeCreateChannelModal(): void {
    this.isCreateChannelModalOpen.set(false);
  }

  submitCreateChannel(): void {
    const currentDept = this.store.selectedDepartment();
    if (!currentDept) return;

    if (this.channelForm.invalid) {
      this.channelForm.markAllAsTouched();
      return;
    }

    const val = this.channelForm.value;
    const req: CreateChannelRequest = {
      name: val.name!.trim(),
      description: val.description?.trim() || undefined,
    };

    this.store.createChannel(currentDept.id, req).subscribe({
      next: () => {
        this.closeCreateChannelModal();
        this.showToast('Kênh nhóm mới đã được tạo thành công!');
      },
    });
  }

  // ── Modal Gán Thành Viên (Member Assignment) ──

  openAssignMemberModal(): void {
    const agents = this.store.agentProfiles();
    const firstAgent = agents.length > 0 ? agents[0] : null;

    this.memberForm.reset({
      type: MemberType.Agent,
      agentProfileId: firstAgent?.id || '',
      displayName: firstAgent?.name || '',
      userId: '',
    });

    this.isAssignMemberModalOpen.set(true);
  }

  closeAssignMemberModal(): void {
    this.isAssignMemberModalOpen.set(false);
  }

  /**
   * Thay đổi loại thành viên (Con người vs Agent AI) và khởi tạo giá trị mặc định tương ứng.
   * ⇒ Nếu chọn Agent AI ⇒ tự động điền hồ sơ Agent đầu tiên trong danh mục.
   */
  onMemberTypeChange(type: MemberType): void {
    this.memberForm.patchValue({ type });
    if (type === MemberType.Agent) {
      const agents = this.store.agentProfiles();
      if (agents.length > 0) {
        this.memberForm.patchValue({
          agentProfileId: agents[0].id,
          displayName: agents[0].name,
          userId: '',
        });
      }
    } else {
      this.memberForm.patchValue({
        agentProfileId: '',
        displayName: '',
        userId: '',
      });
    }
  }

  /**
   * Khi chọn một Agent profile cụ thể, tự động cập nhật displayName tương ứng.
   */
  onAgentProfileSelect(profileId: string): void {
    const profile = this.store.agentProfiles().find((p) => p.id === profileId);
    if (profile) {
      this.memberForm.patchValue({
        agentProfileId: profile.id,
        displayName: profile.name,
      });
    }
  }

  submitAssignMember(): void {
    const currentChannel = this.store.selectedChannel();
    if (!currentChannel) return;

    if (this.memberForm.invalid) {
      this.memberForm.markAllAsTouched();
      return;
    }

    const val = this.memberForm.value;
    const req: AssignMemberRequest = {
      type: Number(val.type) as MemberType,
      displayName: val.displayName!.trim(),
      userId: val.type === MemberType.Human ? val.userId?.trim() || undefined : undefined,
      agentProfileId:
        val.type === MemberType.Agent ? val.agentProfileId?.trim() || undefined : undefined,
    };

    this.store.assignMember(currentChannel.id, req).subscribe({
      next: () => {
        this.closeAssignMemberModal();
        this.showToast('Thành viên mới đã được gán vào kênh!');
      },
    });
  }

  // ── Modal Cấu Hình Agent AI ──

  openAgentModal(): void {
    this.selectedAgentForConfig.set(null);
    this.selectedAgentIdForConfig.set(undefined);
    this.isAgentModalOpen.set(true);
  }

  configureAgentTools(member: Member): void {
    const profileId = member.agentProfileId;
    const profile = this.store.agentProfiles().find((p) => p.id === profileId) ?? null;
    this.selectedAgentForConfig.set(profile);
    this.selectedAgentIdForConfig.set(profileId);
    this.isAgentModalOpen.set(true);
  }

  closeAgentModal(): void {
    this.isAgentModalOpen.set(false);
    this.selectedAgentForConfig.set(null);
    this.selectedAgentIdForConfig.set(undefined);
  }

  onAgentCreated(): void {
    const isEdit = !!this.selectedAgentIdForConfig();
    this.showToast(
      isEdit
        ? 'Cấu hình công cụ cho Agent đã được cập nhật thành công!'
        : 'Hồ sơ Agent AI đã được khởi tạo thành công!',
    );
  }

  // ── Tiện Ích Thông Báo (Toast) ──

  private showToast(msg: string): void {
    this.successMessage.set(msg);
    setTimeout(() => {
      this.successMessage.set(null);
    }, 3500);
  }
}

/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import {
  Component,
  OnInit,
  ChangeDetectionStrategy,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import {
  Department,
  TeamChannel,
  MemberType,
  CreateDepartmentRequest,
  CreateChannelRequest,
  AssignMemberRequest,
} from '../../models/organization.models';
import { OrganizationStore } from '../../store/organization.store';
import { AgentProfileModalComponent } from '../../components/agent-profile-modal/agent-profile-modal.component';
import { ChatRoomComponent } from '../../../chat/components/chat-room/chat-room.component';

@Component({
  selector: 'app-organization-shell',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    AgentProfileModalComponent,
    ChatRoomComponent,
  ],
  templateUrl: './organization-shell.component.html',
  styleUrls: ['./organization-shell.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrganizationShellComponent implements OnInit {
  readonly store = inject(OrganizationStore);
  private readonly fb = inject(FormBuilder);

  readonly MemberType = MemberType;

  // Tab navigation for active channel view ('chat' | 'members')
  readonly activeChannelTab = signal<'chat' | 'members'>('chat');

  // Modal UI states
  readonly isAgentModalOpen = signal<boolean>(false);
  readonly isCreateDeptModalOpen = signal<boolean>(false);
  readonly isCreateChannelModalOpen = signal<boolean>(false);
  readonly isAssignMemberModalOpen = signal<boolean>(false);

  // Status message
  readonly successMessage = signal<string | null>(null);

  // Forms
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

  ngOnInit(): void {
    this.store.loadDepartments();
    this.store.loadAgentProfiles();
  }

  onSelectDepartment(dept: Department): void {
    this.store.selectDepartment(dept);
  }

  onSelectChannel(channel: TeamChannel): void {
    this.activeChannelTab.set('chat');
    this.store.selectChannel(channel);
  }

  // Department Modal Handlers
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

  // Channel Modal Handlers
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

  // Member Modal Handlers
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

  // Agent Profile Modal Handlers
  openAgentModal(): void {
    this.isAgentModalOpen.set(true);
  }

  closeAgentModal(): void {
    this.isAgentModalOpen.set(false);
  }

  onAgentCreated(): void {
    this.showToast('Hồ sơ Agent AI đã được khởi tạo thành công!');
  }

  private showToast(msg: string): void {
    this.successMessage.set(msg);
    setTimeout(() => {
      this.successMessage.set(null);
    }, 3500);
  }
}

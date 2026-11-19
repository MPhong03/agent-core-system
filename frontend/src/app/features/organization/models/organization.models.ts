/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

export enum MemberType {
  Human = 1,
  Agent = 2,
}

export enum ModelProviderType {
  Gemini = 1,
  OpenAI = 2,
  Claude = 3,
  Ollama = 4,
}

export interface Department {
  id: string;
  name: string;
  description?: string;
  createdDate: string;
  channelCount: number;
}

export interface TeamChannel {
  id: string;
  departmentId: string;
  name: string;
  description?: string;
  createdDate: string;
  memberCount: number;
}

export interface Member {
  id: string;
  channelId: string;
  type: MemberType;
  userId?: string;
  agentProfileId?: string;
  displayName: string;
  createdDate: string;
}

export interface AgentProfile {
  id: string;
  name: string;
  avatarUrl?: string;
  provider: ModelProviderType;
  modelName: string;
  systemPrompt: string;
  temperature: number;
  privateMemoryNotes?: string;
  createdDate: string;
}

export interface CreateDepartmentRequest {
  name: string;
  description?: string;
}

export interface CreateChannelRequest {
  name: string;
  description?: string;
}

export interface CreateAgentProfileRequest {
  name: string;
  avatarUrl?: string;
  provider: ModelProviderType;
  modelName: string;
  systemPrompt: string;
  temperature: number;
  privateMemoryNotes?: string;
}

export interface AssignMemberRequest {
  type: MemberType;
  userId?: string;
  agentProfileId?: string;
  displayName: string;
}

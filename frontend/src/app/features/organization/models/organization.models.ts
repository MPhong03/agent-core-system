/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file organization.models.ts — Định nghĩa kiểu dữ liệu & interfaces cho phân hệ Organization (Sprint 0 - PBI-ORG-01)
 *
 * ## Cấu trúc Dữ liệu Phân Hệ Organization
 * Phản ánh mô hình cơ sở dữ liệu trên schema `organization` của SQL Server và các hợp đồng API (DTOs).
 *
 * ## Ràng buộc Kỹ thuật & Khớp Hợp đồng FE ↔ BE (RULE-01)
 * 🔴 UUIDv7: Tất cả khóa chính `id`, `departmentId`, `channelId`, `agentProfileId`, `userId` đều là chuỗi `string` (UUIDv7 từ .NET Guid).
 * 🔴 Casing: Luôn dùng `camelCase` cho các thuộc tính JSON payload.
 * ⛔ Cấm tự tạo mock models hoặc mở rộng trường không có trong hợp đồng BE.
 * ⇒ MemberType phân định rõ Người thật (Human = 1) và Trợ lý AI (Agent = 2).
 */

// ── Enums Phân Loại ──

/**
 * Phân loại thành viên tham gia kênh: Con người hoặc Agent AI.
 */
export enum MemberType {
  Human = 1,
  Agent = 2,
}

/**
 * Các nhà cung cấp mô hình ngôn ngữ lớn (LLM Provider) được hệ thống hỗ trợ.
 */
export enum ModelProviderType {
  Gemini = 1,
  OpenAI = 2,
  Claude = 3,
  Ollama = 4,
  NineRouter = 5,
}

/**
 * Chế độ thực thi công cụ (Tool Execution Mode) dành cho Agent Profile.
 */
export enum ToolExecutionMode {
  Auto = 0,
  SemiAuto = 1,
  NeedApprove = 2,
}

// ── Entity Models (Khớp BE Schema 'organization') ──

/**
 * Phòng ban / Khối ban chức năng trong doanh nghiệp.
 */
export interface Department {
  id: string;
  name: string;
  description?: string;
  createdDate: string;
  channelCount: number;
}

/**
 * Kênh làm việc chuyên trách thuộc một phòng ban.
 */
export interface TeamChannel {
  id: string;
  departmentId: string;
  name: string;
  description?: string;
  createdDate: string;
  memberCount: number;
}

/**
 * Thành viên (Người dùng hoặc Agent AI) được gán vào kênh làm việc.
 */
export interface Member {
  id: string;
  channelId: string;
  type: MemberType;
  userId?: string;
  agentProfileId?: string;
  displayName: string;
  createdDate: string;
}

/**
 * Hồ sơ cấu hình Agent AI tự chủ trong hệ thống.
 */
export interface AgentProfile {
  id: string;
  name: string;
  avatarUrl?: string;
  provider: ModelProviderType;
  modelName: string;
  systemPrompt: string;
  temperature: number;
  privateMemoryNotes?: string;
  toolExecutionMode?: ToolExecutionMode;
  createdDate: string;
}

// ── DTO Request Models (Khớp Request Payloads BE) ──

/**
 * DTO yêu cầu khởi tạo phòng ban mới.
 */
export interface CreateDepartmentRequest {
  name: string;
  description?: string;
}

/**
 * DTO yêu cầu khởi tạo kênh nhóm mới thuộc phòng ban.
 */
export interface CreateChannelRequest {
  name: string;
  description?: string;
}

/**
 * DTO yêu cầu khởi tạo hồ sơ Agent AI mới.
 */
export interface CreateAgentProfileRequest {
  name: string;
  avatarUrl?: string;
  provider: ModelProviderType;
  modelName: string;
  systemPrompt: string;
  temperature: number;
  privateMemoryNotes?: string;
  toolExecutionMode?: ToolExecutionMode;
}

/**
 * DTO yêu cầu gán thành viên vào kênh làm việc.
 */
export interface AssignMemberRequest {
  type: MemberType;
  userId?: string;
  agentProfileId?: string;
  displayName: string;
}

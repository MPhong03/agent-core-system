/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file tool.models.ts — DTOs và Interfaces cho Phân hệ Công cụ AI (No-Code Tool Builder)
 *
 * ## Vai trò Kiến trúc
 * Định nghĩa hợp đồng dữ liệu giữa Frontend và Phân hệ Agents Backend cho tính năng
 * Function Calling và đăng ký công cụ REST API động.
 */

export type ToolType = 'RestApi';

export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH';

/**
 * DTO chi tiết của một công cụ được lưu trữ trong cơ sở dữ liệu.
 */
export interface ToolDefinitionDto {
  id: string;
  name: string;
  displayName: string;
  description: string;
  type: ToolType | string;
  endpointUrl: string;
  httpMethod: string;
  headersConfig: string;
  parametersSchema: string;
  requiresConfirmation: boolean;
  timeoutSeconds: number;
  isActive: boolean;
  createdDate: string;
  updatedDate: string | null;
}

/**
 * Dữ liệu yêu cầu tạo mới định nghĩa công cụ.
 */
export interface CreateToolRequest {
  name: string;
  displayName: string;
  description: string;
  type?: string;
  endpointUrl: string;
  httpMethod: string;
  headersConfig?: string | null;
  parametersSchema?: string | null;
  requiresConfirmation: boolean;
  timeoutSeconds: number;
}

/**
 * Dữ liệu yêu cầu cập nhật định nghĩa công cụ hiện có.
 */
export interface UpdateToolRequest {
  displayName: string;
  description: string;
  endpointUrl: string;
  httpMethod: string;
  headersConfig?: string | null;
  parametersSchema?: string | null;
  requiresConfirmation: boolean;
  timeoutSeconds: number;
}

/**
 * Yêu cầu kiểm thử công cụ đã lưu trong cơ sở dữ liệu.
 */
export interface TestToolRequest {
  headersConfig?: string | null;
  arguments?: Record<string, any> | null;
  argumentsJson?: string | null;
  timeoutSeconds?: number | null;
}

/**
 * Yêu cầu kiểm thử công cụ ad-hoc (trước khi lưu vào cơ sở dữ liệu).
 */
export interface TestAdHocToolRequest {
  endpointUrl: string;
  httpMethod: string;
  headersConfig?: string | null;
  arguments?: Record<string, any> | null;
  argumentsJson?: string | null;
  timeoutSeconds?: number | null;
}

/**
 * Kết quả thực thi thử nghiệm công cụ REST API.
 */
export interface ToolTestResultDto {
  isSuccess: boolean;
  statusCode: number;
  responsePayload: string;
  durationMs: number;
  errorMessage?: string | null;
}

/**
 * Mục header cấu hình dạng Key - Value hiển thị và chỉnh sửa trên UI.
 */
export interface HeaderConfigItem {
  key: string;
  value: string;
}

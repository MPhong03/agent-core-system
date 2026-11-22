/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

export interface AgentWorkspaceDetail {
  id: string;
  agentProfileId: string;
  rootPath: string;
  pythonVersion?: string | null;
  maxTimeoutSeconds: number;
  isVenvReady: boolean;
  isActive: boolean;
  storageUsedBytes: number;
  totalFilesCount: number;
  scriptsCount: number;
  artifactsCount: number;
  lastActiveAt?: string | null;
}

export interface WorkspaceFile {
  name: string;
  relativePath: string;
  category: 'scripts' | 'artifacts' | 'logs' | 'other' | string;
  sizeBytes: number;
  lastModifiedUtc: string;
  extension: string;
}

export interface ArtifactFile {
  fileName: string;
  sizeBytes: number;
  relativePath: string;
}

export interface PythonExecutionResult {
  isSuccess: boolean;
  exitCode: number;
  stdout: string;
  stderr: string;
  durationMs: number;
  isTimeout: boolean;
  generatedArtifacts: ArtifactFile[];
  errorMessage?: string | null;
}

export interface ScriptExecutionHistory {
  id: string;
  agentProfileId: string;
  callerType: string;
  executionSourceId: string;
  scriptName: string;
  scriptContent: string;
  exitCode: number;
  stdout?: string | null;
  stderr?: string | null;
  durationMs: number;
  isTimeout: boolean;
  artifactsJson?: string | null;
  createdDate: string;
}

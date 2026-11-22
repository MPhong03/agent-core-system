/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file agent-workspace.component.spec.ts — Unit tests cho AgentWorkspaceComponent (Sprint 4)
 */

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { AgentWorkspaceComponent } from './agent-workspace.component';
import { AgentWorkspaceService } from '../../services/agent-workspace.service';
import {
  AgentWorkspaceDetail,
  WorkspaceFile,
  PythonExecutionResult,
  ScriptExecutionHistory,
} from '../../models/workspace.models';

describe('AgentWorkspaceComponent', () => {
  let component: AgentWorkspaceComponent;
  let fixture: ComponentFixture<AgentWorkspaceComponent>;

  let mockWorkspaceService: {
    workspace: ReturnType<typeof signal<AgentWorkspaceDetail | null>>;
    files: ReturnType<typeof signal<WorkspaceFile[]>>;
    packages: ReturnType<typeof signal<string[]>>;
    history: ReturnType<typeof signal<ScriptExecutionHistory[]>>;
    isLoading: ReturnType<typeof signal<boolean>>;
    isExecuting: ReturnType<typeof signal<boolean>>;
    isInitializingVenv: ReturnType<typeof signal<boolean>>;
    lastExecutionResult: ReturnType<typeof signal<PythonExecutionResult | null>>;
    getWorkspace: ReturnType<typeof vi.fn>;
    initVenv: ReturnType<typeof vi.fn>;
    getPackages: ReturnType<typeof vi.fn>;
    installPackages: ReturnType<typeof vi.fn>;
    listFiles: ReturnType<typeof vi.fn>;
    executeScript: ReturnType<typeof vi.fn>;
    getHistory: ReturnType<typeof vi.fn>;
    getDownloadUrl: ReturnType<typeof vi.fn>;
  };

  const sampleWorkspace: AgentWorkspaceDetail = {
    id: 'ws-001',
    agentProfileId: 'agent-123',
    rootPath: 'data/workspaces/agent-123',
    pythonVersion: 'Python 3.14.0',
    maxTimeoutSeconds: 60,
    isVenvReady: true,
    isActive: true,
    storageUsedBytes: 1048576,
    totalFilesCount: 5,
    scriptsCount: 2,
    artifactsCount: 3,
    lastActiveAt: '2026-11-22T08:00:00Z',
  };

  const sampleFiles: WorkspaceFile[] = [
    {
      name: 'report.csv',
      relativePath: 'artifacts/report.csv',
      category: 'artifacts',
      sizeBytes: 1024,
      lastModifiedUtc: '2026-11-22T08:00:00Z',
      extension: '.csv',
    },
    {
      name: 'script.py',
      relativePath: 'scripts/script.py',
      category: 'scripts',
      sizeBytes: 512,
      lastModifiedUtc: '2026-11-22T08:00:00Z',
      extension: '.py',
    },
  ];

  beforeEach(async () => {
    mockWorkspaceService = {
      workspace: signal<AgentWorkspaceDetail | null>(sampleWorkspace),
      files: signal<WorkspaceFile[]>(sampleFiles),
      packages: signal<string[]>(['pandas', 'numpy']),
      history: signal<ScriptExecutionHistory[]>([]),
      isLoading: signal<boolean>(false),
      isExecuting: signal<boolean>(false),
      isInitializingVenv: signal<boolean>(false),
      lastExecutionResult: signal<PythonExecutionResult | null>(null),
      getWorkspace: vi.fn().mockReturnValue(of({ success: true, data: sampleWorkspace })),
      initVenv: vi.fn().mockReturnValue(of({ success: true, data: 'OK' })),
      getPackages: vi.fn().mockReturnValue(of({ success: true, data: ['pandas', 'numpy'] })),
      installPackages: vi.fn().mockReturnValue(of({ success: true, data: 'Installed' })),
      listFiles: vi.fn().mockReturnValue(of({ success: true, data: sampleFiles })),
      executeScript: vi.fn().mockReturnValue(
        of({
          success: true,
          data: {
            isSuccess: true,
            exitCode: 0,
            stdout: 'Hello World',
            stderr: '',
            durationMs: 120,
            isTimeout: false,
            generatedArtifacts: [],
          },
        })
      ),
      getHistory: vi.fn().mockReturnValue(of({ success: true, data: [] })),
      getDownloadUrl: vi.fn().mockImplementation((agentId, path) => `/api/v1/agents/${agentId}/workspace/files/download?path=${path}`),
    };

    await TestBed.configureTestingModule({
      imports: [AgentWorkspaceComponent],
      providers: [{ provide: AgentWorkspaceService, useValue: mockWorkspaceService }],
    }).compileComponents();

    fixture = TestBed.createComponent(AgentWorkspaceComponent);
    component = fixture.componentInstance;
    component.agentProfileId = 'agent-123';
    fixture.detectChanges();
  });

  it('should create and load workspace data on init', () => {
    expect(component).toBeTruthy();
    expect(mockWorkspaceService.getWorkspace).toHaveBeenCalledWith('agent-123');
    expect(mockWorkspaceService.listFiles).toHaveBeenCalledWith('agent-123');
    expect(mockWorkspaceService.getPackages).toHaveBeenCalledWith('agent-123');
  });

  it('should filter files by category', () => {
    expect(component.filteredFiles().length).toBe(2);

    component.setCategory('artifacts');
    expect(component.filteredFiles().length).toBe(1);
    expect(component.filteredFiles()[0].name).toBe('report.csv');

    component.setCategory('scripts');
    expect(component.filteredFiles().length).toBe(1);
    expect(component.filteredFiles()[0].name).toBe('script.py');
  });

  it('should trigger initVenv when onInitVenv is called', () => {
    component.onInitVenv();
    expect(mockWorkspaceService.initVenv).toHaveBeenCalledWith('agent-123');
  });

  it('should trigger installPackages when package name is entered', () => {
    component.newPackageInput.set('openpyxl matplotlib');
    component.onInstallPackage();
    expect(mockWorkspaceService.installPackages).toHaveBeenCalledWith('agent-123', ['openpyxl', 'matplotlib']);
    expect(component.newPackageInput()).toBe('');
  });

  it('should open and close preview modal', () => {
    const file = sampleFiles[0];
    component.openPreview(file);
    expect(component.isPreviewOpen()).toBe(true);
    expect(component.previewFile()).toEqual(file);

    component.closePreview();
    expect(component.isPreviewOpen()).toBe(false);
    expect(component.previewFile()).toBeNull();
  });

  it('should execute script when onRunScript is called', () => {
    component.testScriptCode.set('print("Test Script")');
    component.onRunScript();
    expect(mockWorkspaceService.executeScript).toHaveBeenCalledWith('agent-123', 'print("Test Script")', 'interactive_test.py');
  });
});

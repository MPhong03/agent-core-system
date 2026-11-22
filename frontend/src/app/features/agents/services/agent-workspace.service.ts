/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap, catchError, throwError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';
import {
  AgentWorkspaceDetail,
  WorkspaceFile,
  PythonExecutionResult,
  ScriptExecutionHistory,
} from '../models/workspace.models';

@Injectable({
  providedIn: 'root',
})
export class AgentWorkspaceService {
  private readonly http = inject(HttpClient);

  // ── Reactive Signals State ──
  readonly workspace = signal<AgentWorkspaceDetail | null>(null);
  readonly files = signal<WorkspaceFile[]>([]);
  readonly packages = signal<string[]>([]);
  readonly history = signal<ScriptExecutionHistory[]>([]);
  readonly isLoading = signal<boolean>(false);
  readonly isExecuting = signal<boolean>(false);
  readonly isInitializingVenv = signal<boolean>(false);
  readonly lastExecutionResult = signal<PythonExecutionResult | null>(null);

  private getBaseUrl(agentId: string): string {
    return `${environment.apiUrl}/agents/${agentId}/workspace`;
  }

  getWorkspace(agentId: string): Observable<ApiResponse<AgentWorkspaceDetail>> {
    this.isLoading.set(true);
    return this.http.get<ApiResponse<AgentWorkspaceDetail>>(this.getBaseUrl(agentId)).pipe(
      tap((res) => {
        if (res.success && res.data) {
          this.workspace.set(res.data);
        }
        this.isLoading.set(false);
      }),
      catchError((err) => {
        this.isLoading.set(false);
        return throwError(() => err);
      })
    );
  }

  initVenv(agentId: string): Observable<ApiResponse<string>> {
    this.isInitializingVenv.set(true);
    return this.http.post<ApiResponse<string>>(`${this.getBaseUrl(agentId)}/init-venv`, {}).pipe(
      tap((res) => {
        this.isInitializingVenv.set(false);
        // Refresh workspace state
        this.getWorkspace(agentId).subscribe();
      }),
      catchError((err) => {
        this.isInitializingVenv.set(false);
        return throwError(() => err);
      })
    );
  }

  getPackages(agentId: string): Observable<ApiResponse<string[]>> {
    return this.http.get<ApiResponse<string[]>>(`${this.getBaseUrl(agentId)}/packages`).pipe(
      tap((res) => {
        if (res.success && res.data) {
          this.packages.set(res.data);
        }
      })
    );
  }

  installPackages(agentId: string, packagesList: string[]): Observable<ApiResponse<string>> {
    this.isLoading.set(true);
    return this.http
      .post<ApiResponse<string>>(`${this.getBaseUrl(agentId)}/install-packages`, { packages: packagesList })
      .pipe(
        tap(() => {
          this.isLoading.set(false);
          this.getPackages(agentId).subscribe();
        }),
        catchError((err) => {
          this.isLoading.set(false);
          return throwError(() => err);
        })
      );
  }

  listFiles(agentId: string, category?: string): Observable<ApiResponse<WorkspaceFile[]>> {
    let params = new HttpParams();
    if (category && category !== 'all') {
      params = params.set('category', category);
    }

    return this.http
      .get<ApiResponse<WorkspaceFile[]>>(`${this.getBaseUrl(agentId)}/files`, { params })
      .pipe(
        tap((res) => {
          if (res.success && res.data) {
            this.files.set(res.data);
          }
        })
      );
  }

  executeScript(
    agentId: string,
    scriptContent: string,
    scriptName?: string,
    timeoutSeconds?: number
  ): Observable<ApiResponse<PythonExecutionResult>> {
    this.isExecuting.set(true);
    const payload = { scriptContent, scriptName, timeoutSeconds };

    return this.http
      .post<ApiResponse<PythonExecutionResult>>(`${this.getBaseUrl(agentId)}/execute`, payload)
      .pipe(
        tap((res) => {
          this.isExecuting.set(false);
          if (res.success && res.data) {
            this.lastExecutionResult.set(res.data);
            // Refresh files and history
            this.listFiles(agentId).subscribe();
            this.getHistory(agentId).subscribe();
          }
        }),
        catchError((err) => {
          this.isExecuting.set(false);
          return throwError(() => err);
        })
      );
  }

  getHistory(agentId: string, limit = 50): Observable<ApiResponse<ScriptExecutionHistory[]>> {
    const params = new HttpParams().set('limit', limit.toString());
    return this.http
      .get<ApiResponse<ScriptExecutionHistory[]>>(`${this.getBaseUrl(agentId)}/history`, { params })
      .pipe(
        tap((res) => {
          if (res.success && res.data) {
            this.history.set(res.data);
          }
        })
      );
  }

  getDownloadUrl(agentId: string, relativePath: string): string {
    return `${this.getBaseUrl(agentId)}/files/download?path=${encodeURIComponent(relativePath)}`;
  }
}

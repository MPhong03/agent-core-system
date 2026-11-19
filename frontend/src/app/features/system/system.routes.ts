/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { Routes } from '@angular/router';

export const SYSTEM_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'users',
    pathMatch: 'full',
  },
  {
    path: 'users',
    loadComponent: () =>
      import('./users/users-list/users-list.component').then((m) => m.UsersListComponent),
  },
  {
    path: 'roles',
    loadComponent: () =>
      import('./roles/roles-list/roles-list.component').then((m) => m.RolesListComponent),
  },
  {
    path: 'audit-logs',
    loadComponent: () =>
      import('./audit-logs/audit-logs-list/audit-logs-list.component').then(
        (m) => m.AuditLogsListComponent,
      ),
  },
];

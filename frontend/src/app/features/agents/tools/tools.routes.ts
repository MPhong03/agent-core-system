/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file tools.routes.ts — Định tuyến cho Phân hệ Quản lý Công cụ AI (Tools Management)
 */

import { Routes } from '@angular/router';
import { ToolListComponent } from './components/tool-list/tool-list.component';

export const TOOLS_ROUTES: Routes = [
  {
    path: '',
    component: ToolListComponent,
  },
];

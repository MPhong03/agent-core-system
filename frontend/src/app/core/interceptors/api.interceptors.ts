/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ProblemDetails } from '../models/api-response.model';
import { I18nService } from '../i18n/i18n.service';

/**
 * Interceptor tự động bổ sung Base URL, API Version và Header Accept-Language cho các request tương đối.
 */
export const apiVersionInterceptor: HttpInterceptorFn = (req, next) => {
  // Bỏ qua các URL tuyệt đối hoặc file assets tĩnh (như từ điển i18n JSON)
  if (
    req.url.startsWith('http://') ||
    req.url.startsWith('https://') ||
    req.url.startsWith('/assets') ||
    req.url.startsWith('./assets') ||
    req.url.startsWith('assets')
  ) {
    return next(req);
  }

  const i18nService = inject(I18nService);
  const lang = i18nService.currentLang() === 'en' ? 'en-US' : 'vi-VN';

  const normalizedUrl = req.url.startsWith('/') ? req.url : `/${req.url}`;
  const fullUrl = `${environment.apiUrl}${normalizedUrl}`;

  const cloned = req.clone({
    url: fullUrl,
    setHeaders: {
      'Accept-Language': lang,
    },
  });

  return next(cloned);
};

/**
 * Interceptor đính kèm JWT Bearer Token vào Authorization Header.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = localStorage.getItem('access_token');
  if (token && !req.url.includes('/assets/')) {
    const authReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`,
      },
    });
    return next(authReq);
  }

  return next(req);
};

/**
 * Interceptor bắt lỗi HTTP tập trung, giải mã cấu trúc RFC 7807 Problem Details.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let problemDetails: ProblemDetails | null = null;

      if (error.error && typeof error.error === 'object') {
        problemDetails = error.error as ProblemDetails;
        console.error(
          `[API Error ${error.status}]: ${problemDetails.title} - ${problemDetails.detail}`,
          problemDetails,
        );
      } else {
        console.error(`[Network / HTTP Error ${error.status}]:`, error.message);
      }

      return throwError(() => problemDetails ?? error);
    }),
  );
};

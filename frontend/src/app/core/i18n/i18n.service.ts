/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export type LanguageCode = 'vi' | 'en';

@Injectable({
  providedIn: 'root',
})
export class I18nService {
  private readonly http = inject(HttpClient);

  // Active language signal (defaults to Vietnamese)
  readonly currentLang = signal<LanguageCode>(this.getInitialLanguage());

  // Translation dictionary signal
  readonly dictionary = signal<Record<string, any>>({});

  constructor() {
    this.loadLanguage(this.currentLang());
  }

  setLanguage(lang: LanguageCode): void {
    if (this.currentLang() === lang && Object.keys(this.dictionary()).length > 0) return;
    this.currentLang.set(lang);
    localStorage.setItem('app_language', lang);
    this.loadLanguage(lang);
  }

  translate(key: string): string {
    const dict = this.dictionary();
    if (!key || !dict) return key;

    const parts = key.split('.');
    let current: any = dict;

    for (const part of parts) {
      if (current && typeof current === 'object' && part in current) {
        current = current[part];
      } else {
        return key; // Fallback to raw key if not found
      }
    }

    return typeof current === 'string' ? current : key;
  }

  private loadLanguage(lang: LanguageCode): void {
    this.http.get<Record<string, any>>(`/assets/i18n/${lang}.json`).subscribe({
      next: (dict) => this.dictionary.set(dict),
      error: (err) => console.error(`[i18n] Failed to load dictionary for ${lang}:`, err),
    });
  }

  private getInitialLanguage(): LanguageCode {
    const stored = localStorage.getItem('app_language');
    if (stored === 'vi' || stored === 'en') {
      return stored;
    }
    return 'vi';
  }
}

import { Injectable, signal, effect } from '@angular/core';

export interface AppTheme {
  id: string;
  name: string;
  description: string;
  accentColor: string;
  canvasColor: string;
  isDark: boolean;
}

export const APP_THEMES: AppTheme[] = [
  {
    id: 'slate-dark',
    name: 'Slate & Indigo',
    description: 'Modern developer dark theme with neon indigo accents',
    accentColor: '#6366f1',
    canvasColor: '#090d16',
    isDark: true
  },
  {
    id: 'obsidian-purple',
    name: 'Obsidian Violet',
    description: 'Deep cyberpunk obsidian with neon purple glow',
    accentColor: '#a855f7',
    canvasColor: '#0a0612',
    isDark: true
  },
  {
    id: 'midnight-emerald',
    name: 'Midnight Emerald',
    description: 'High-contrast matrix emerald and deep forest dark',
    accentColor: '#10b981',
    canvasColor: '#04120b',
    isDark: true
  },
  {
    id: 'nordic-cyan',
    name: 'Nordic Cyan',
    description: 'Cool Arctic night with vibrant sky cyan highlights',
    accentColor: '#38bdf8',
    canvasColor: '#07111a',
    isDark: true
  },
  {
    id: 'clean-light',
    name: 'Clean Slate Light',
    description: 'Minimalist high-contrast light theme',
    accentColor: '#4f46e5',
    canvasColor: '#f8fafc',
    isDark: false
  }
];

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private static readonly STORAGE_KEY = 'redis-gui-active-theme';

  readonly themes = APP_THEMES;
  readonly activeThemeId = signal<string>(this.getInitialTheme());

  constructor() {
    effect(() => {
      const themeId = this.activeThemeId();
      this.applyTheme(themeId);
    });
  }

  setTheme(themeId: string): void {
    const found = this.themes.find(t => t.id === themeId);
    if (found) {
      this.activeThemeId.set(themeId);
      try {
        localStorage.setItem(ThemeService.STORAGE_KEY, themeId);
      } catch (e) {
        console.warn('Could not save theme preference:', e);
      }
    }
  }

  private getInitialTheme(): string {
    try {
      const stored = localStorage.getItem(ThemeService.STORAGE_KEY);
      if (stored && this.themes.some(t => t.id === stored)) {
        return stored;
      }
    } catch {
      // Fallback
    }
    return 'slate-dark';
  }

  private applyTheme(themeId: string): void {
    if (typeof document === 'undefined') return;

    const root = document.documentElement;
    root.setAttribute('data-theme', themeId);
    document.body.setAttribute('data-theme', themeId);

    const theme = this.themes.find(t => t.id === themeId);
    if (theme?.isDark) {
      root.classList.add('dark');
      root.classList.remove('light');
      document.body.classList.add('dark', 'dark-theme');
      document.body.classList.remove('light', 'light-theme');
    } else {
      root.classList.add('light');
      root.classList.remove('dark');
      document.body.classList.add('light', 'light-theme');
      document.body.classList.remove('dark', 'dark-theme');
    }
  }
}

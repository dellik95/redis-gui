import { ConnectionBrand } from './connection-brand';
import { Component, inject, signal, effect, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NgTemplateOutlet } from '@angular/common';

import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';

import { LoadingComponent } from './shared/components/loading/loading.component';
import { ThemePicker } from './shared/ui/theme-picker/theme-picker';

import { AuthStore } from './core/stores/auth.store';
import { SideNavStore } from './core/stores/side-nav.store';
import { ConnectionsStore } from './core/stores/connections.store';
import { AuthService } from './core/services/auth/auth.service';
import { useMediaQuery } from './core/utils/signal-utilities';

type DensityType = 'default' | 'comfortable' | 'compact';

/**
 * Root Application Component
 * Main application shell with navigation, sidebar, and layout management
 *
 * Features:
 * - Responsive layout (mobile/desktop)
 * - Side navigation
 * - User authentication UI
 * - Theme management
 * - Density settings (default/comfortable/compact)
 * - Breadcrumb navigation
 * - Section actions portal
 * - Zoneless change detection compatible
 * - Modern signal hooks for media queries
 */
@Component({
  selector: 'app-root',
  host: { '(window:keydown)': 'onWindowKeyDown($event)' },
  imports: [
    ConnectionBrand,
    RouterOutlet,
    LoadingComponent,
    MatToolbarModule,
    MatButtonModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatMenuModule,
    MatDividerModule,
    MatTooltipModule,
    RouterLink,
    RouterLinkActive,
    NgTemplateOutlet,
    ThemePicker,
    MatDialogModule
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppComponent {
  // 1. Constants
  /** Application title */
  readonly title = 'Wunderwaffe';

  // 2. Injected dependencies
  readonly sideNavStore = inject(SideNavStore);
  readonly authStore = inject(AuthStore);
  readonly authService = inject(AuthService);
  readonly connectionsStore = inject(ConnectionsStore);
  private readonly dialog = inject(MatDialog);

  async openCommandPalette(): Promise<void> {
    const { CommandPaletteComponent } =
      await import('./shared/components/command-palette/command-palette.component');
    if (this.dialog.getDialogById('command-palette')) return;
    this.dialog.open(CommandPaletteComponent, {
      id: 'command-palette',
      width: '680px',
      maxWidth: '94vw',
      panelClass: 'command-palette-dialog-panel',
      enterAnimationDuration: '0ms',
      exitAnimationDuration: '0ms'
    });
  }

  onWindowKeyDown(e: KeyboardEvent): void {
    if (!e.defaultPrevented && (e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
      e.preventDefault();
      this.openCommandPalette();
    }
  }

  // 3. State signals
  /** Whether the app is in mobile view (using custom hook) */
  readonly isMobile = useMediaQuery('(max-width: 600px)');

  /** UI density setting (default/comfortable/compact) with localStorage persistence */
  readonly density = signal<DensityType>(this.getStoredDensity());

  constructor() {
    effect(() => {
      const density = this.density();
      this.applyDensityClass(density);
      this.saveDensity(density);
    });
  }

  /**
   * Gets stored density from localStorage with fallback
   */
  private getStoredDensity(): DensityType {
    try {
      const stored = localStorage.getItem('appDensity');
      if (stored === 'comfortable' || stored === 'compact') {
        return stored;
      }
    } catch {
      // Ignore localStorage errors
    }
    return 'default';
  }

  /**
   * Saves density to localStorage
   */
  private saveDensity(density: DensityType): void {
    try {
      localStorage.setItem('appDensity', density);
    } catch {
      // Ignore localStorage errors
    }
  }

  /**
   * Applies density CSS class to document root
   */
  private applyDensityClass(density: DensityType): void {
    const html = document.documentElement;
    html.classList.remove('density-default', 'density-comfortable', 'density-compact');
    html.classList.add(`density-${density}`);
  }

  /**
   * Sets the UI density
   *
   * @param d - Density setting: 'default', 'comfortable', or 'compact'
   */
  setDensity(d: DensityType): void {
    this.density.set(d);
  }

  /**
   * Logs out the current user
   * Clears authentication state and redirects to login
   */
  logout(): void {
    this.authService.logout();
  }
}

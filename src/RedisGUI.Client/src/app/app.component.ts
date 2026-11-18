import { ConnectionBrand } from './connection-brand';
import {
  Component,
  inject,
  signal,
  effect,
  untracked,
  ChangeDetectionStrategy
} from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NgTemplateOutlet } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map } from 'rxjs';

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
import { JobNotificationCenterComponent } from './shared/components/job-notification-center/job-notification-center.component';

import { AuthStore } from './core/stores/auth.store';
import { SideNavStore } from './core/stores/side-nav.store';
import { ConnectionsStore } from './core/stores/connections.store';
import { BackgroundJobsStore } from './core/stores/background-jobs.store';
import { AuthService } from './core/services/auth/auth.service';
import { useMediaQuery } from './core/utils/signal-utilities';

type DensityType = 'default' | 'comfortable' | 'compact';

function extractConnectionIdFromUrl(url: string): string | null {
  const match = /^\/(?:storage|messaging|info)\/([0-9a-fA-F-]{8,})(?:[/?#]|$)/.exec(url);
  return match?.[1] ?? null;
}

@Component({
  selector: 'app-root',
  host: { '(window:keydown)': 'onWindowKeyDown($event)' },
  imports: [
    ConnectionBrand,
    JobNotificationCenterComponent,
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
  readonly title = 'Wunderwaffe';

  readonly sideNavStore = inject(SideNavStore);
  readonly authStore = inject(AuthStore);
  readonly authService = inject(AuthService);
  readonly connectionsStore = inject(ConnectionsStore);
  private readonly backgroundJobsStore = inject(BackgroundJobsStore);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);

  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map(event => event.urlAfterRedirects || event.url)
    ),
    { initialValue: this.router.url }
  );

  private syncedUserId: string | null = null;
  private readonly requestedConnectionIds = new Set<string>();

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

  readonly isMobile = useMediaQuery('(max-width: 600px)');
  readonly density = signal<DensityType>(this.getStoredDensity());

  constructor() {
    effect(() => {
      const density = this.density();
      this.applyDensityClass(density);
      this.saveDensity(density);
    });

    // Synchronize user-scoped background jobs and connections catalog on login/boot
    effect(() => {
      const user = this.authStore.user();
      untracked(() => {
        this.sideNavStore.setUserScope(user?.id ?? null);
        if (!user) {
          this.syncedUserId = null;
          this.requestedConnectionIds.clear();
          return;
        }
        if (this.syncedUserId !== user.id) {
          this.syncedUserId = user.id;
          this.backgroundJobsStore.syncForUser(user.id);
          if (this.connectionsStore.needsRefresh()) {
            this.connectionsStore.loadConnections();
          }
        }
      });
    });

    // Restore and synchronize active connection + sidebar routes across page reloads
    effect(() => {
      const user = this.authStore.user();
      if (!user) return;

      const url = this.currentUrl();
      const connections = this.connectionsStore.connections();
      const selected = this.connectionsStore.selectedConnection();
      const lastFetched = this.connectionsStore.lastFetched();
      const isLoading = this.connectionsStore.isLoading();

      untracked(() => {
        const routeConnectionId = extractConnectionIdFromUrl(url);
        const persisted = this.sideNavStore.getPersistedConnection(user.id);
        const targetId = routeConnectionId ?? selected?.id ?? persisted?.connectionId ?? null;

        if (!targetId) {
          if (this.sideNavStore.hasActiveConnection()) {
            this.sideNavStore.clearConnectionRoutes();
          }
          return;
        }

        const matched = connections.find(c => c.id === targetId);
        if (matched) {
          const selectionStale =
            selected?.id !== matched.id ||
            selected.serverMode !== matched.serverMode ||
            selected.name !== matched.name ||
            selected.isReadOnly !== matched.isReadOnly;

          if (selectionStale) {
            this.connectionsStore.selectConnection(matched);
          }

          const navStale =
            this.sideNavStore.currentConnectionId() !== matched.id ||
            this.sideNavStore.currentConnectionName() !== matched.name ||
            this.sideNavStore.currentConnectionServerMode() !== (matched.serverMode ?? null);

          if (navStale) {
            this.sideNavStore.addConnectionRoutes(matched.id, matched.name, matched.serverMode);
          }
          return;
        }

        if (routeConnectionId) {
          if (
            persisted &&
            persisted.connectionId === routeConnectionId &&
            this.sideNavStore.currentConnectionId() !== routeConnectionId
          ) {
            this.sideNavStore.addConnectionRoutes(
              persisted.connectionId,
              persisted.connectionName,
              persisted.serverMode
            );
          }
          if (!this.requestedConnectionIds.has(routeConnectionId)) {
            this.requestedConnectionIds.add(routeConnectionId);
            this.connectionsStore.loadConnectionById(routeConnectionId);
          }
          return;
        }

        if (!isLoading && lastFetched !== null && persisted && targetId === persisted.connectionId) {
          this.connectionsStore.selectConnection(null);
          this.sideNavStore.clearConnectionRoutes();
        }
      });
    });
  }

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

  private saveDensity(density: DensityType): void {
    try {
      localStorage.setItem('appDensity', density);
    } catch {
      // Ignore localStorage errors
    }
  }

  private applyDensityClass(density: DensityType): void {
    const html = document.documentElement;
    html.classList.remove('density-default', 'density-comfortable', 'density-compact');
    html.classList.add(`density-${density}`);
  }

  setDensity(d: DensityType): void {
    this.density.set(d);
  }

  logout(): void {
    this.authService.logout();
  }
}

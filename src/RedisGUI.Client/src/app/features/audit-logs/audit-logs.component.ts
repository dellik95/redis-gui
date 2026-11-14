import {
  Component,
  inject,
  signal,
  computed,
  ChangeDetectionStrategy,
  effect,
  untracked
} from '@angular/core';
import { DatePipe, SlicePipe, DecimalPipe } from '@angular/common';
import { form, FormField, pattern, submit } from '@angular/forms/signals';

import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatExpansionModule } from '@angular/material/expansion';
import { UiSelectComponent, UiSelectOption, UiInputComponent } from '../../shared/ui';
import {
  PageHeaderComponent,
  StatCountCardComponent,
  EmptyStateComponent
} from '../../shared/components';
import { MatSnackBar } from '@angular/material/snack-bar';

import { AuditStore } from '../../core/stores/audit.store';
import { AuthStore } from '../../core/stores/auth.store';
import type { AuditLogItem, GetAuditLogsRequest } from '../../models/audit.types';

interface AuditFilterFormModel {
  entityType: string;
  entityId: string;
  userId: string;
  fromDate: string;
  toDate: string;
}

const INITIAL_AUDIT_FILTERS: AuditFilterFormModel = {
  entityType: '',
  entityId: '',
  userId: '',
  fromDate: '',
  toDate: ''
};

/**
 * Audit Logs Component
 *
 * Displays audit log history with filtering and pagination.
 * Admin-only access required.
 *
 * Features:
 * - Paginated audit log display
 * - Filtering by entity type, user, and date range
 * - Expandable details for each log entry
 * - JSON diff display for value changes
 * - Modern signal-based state management
 * - Zoneless compatible
 */
@Component({
  selector: 'app-audit-logs',
  standalone: true,
  imports: [
    DatePipe,
    SlicePipe,
    DecimalPipe,
    FormField,
    MatCardModule,
    MatTableModule,
    MatProgressBarModule,
    MatIconModule,
    MatButtonModule,
    MatChipsModule,
    MatTooltipModule,
    MatExpansionModule,
    UiSelectComponent,
    UiInputComponent,
    PageHeaderComponent,
    StatCountCardComponent,
    EmptyStateComponent
  ],
  templateUrl: './audit-logs.component.html',
  styleUrl: './audit-logs.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AuditLogsComponent {
  // 1. Injected dependencies
  readonly auditStore = inject(AuditStore);
  private readonly authStore = inject(AuthStore);
  private readonly snackBar = inject(MatSnackBar);

  // 2. Store signals
  readonly items = this.auditStore.items;
  readonly totalCount = this.auditStore.totalCount;
  readonly isLoading = this.auditStore.isLoading;
  readonly error = this.auditStore.error;
  readonly currentPage = this.auditStore.currentPage;
  readonly totalPages = this.auditStore.totalPages;
  readonly hasNextPage = this.auditStore.hasNextPage;
  readonly hasPreviousPage = this.auditStore.hasPreviousPage;
  readonly isAdmin = this.authStore.isAdmin;

  // 3. State signals
  readonly filterModel = signal<AuditFilterFormModel>(INITIAL_AUDIT_FILTERS);
  readonly filterForm = form(this.filterModel, schema => {
    const guid = /^(?:[0-9a-f]{32}|[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12})$/i;
    const config = { message: 'Enter a valid GUID.' };
    pattern(schema.entityId, guid, config);
    pattern(schema.userId, guid, config);
  });
  readonly pageSize = signal(50);
  readonly expandedRowId = signal<string | null>(null);

  // 4. Configuration
  readonly displayedColumns = [
    'timestamp',
    'action',
    'entityType',
    'entityId',
    'userId',
    'ipAddress',
    'details'
  ] as const;
  readonly entityTypeOptions: UiSelectOption[] = [
    { label: 'All Types', value: '', icon: 'list' },
    { label: 'Connection', value: 'Connection', icon: 'dns' },
    { label: 'User', value: 'User', icon: 'person' },
    { label: 'Redis', value: 'Redis', icon: 'storage' }
  ];

  readonly pageSizeOptions: UiSelectOption[] = [
    { label: '25 / page', value: 25 },
    { label: '50 / page', value: 50 },
    { label: '100 / page', value: 100 },
    { label: '200 / page', value: 200 }
  ];

  readonly pageStart = computed(() => {
    const count = this.totalCount();
    if (count === 0) return 0;
    return (this.currentPage() - 1) * this.pageSize() + 1;
  });

  readonly pageEnd = computed(() => {
    return Math.min(this.currentPage() * this.pageSize(), this.totalCount());
  });

  onPageSizeChange(newSize: number): void {
    this.pageSize.set(newSize);
    this.goToPage(1);
  }

  goToPage(page: number): void {
    if (page < 1 || (this.totalPages() > 0 && page > this.totalPages())) return;
    const newSkip = (page - 1) * this.pageSize();
    this.auditStore.loadAuditLogs({
      ...this.auditStore.filters(),
      skip: newSkip,
      take: this.pageSize()
    });
  }

  constructor() {
    // Check admin access and load data
    effect(() => {
      if (this.isAdmin()) {
        untracked(() => this.loadAuditLogs());
      } else {
        this.snackBar.open('Access denied. Admin privileges required.', 'Close', {
          duration: 5000
        });
      }
    });
  }

  // 5. Public methods
  /**
   * Get action icon based on audit action type
   */
  getActionIcon(action: string): string {
    switch (action.toLowerCase()) {
      case 'create':
        return 'add_circle';
      case 'update':
        return 'edit';
      case 'delete':
        return 'delete';
      case 'login':
        return 'login';
      case 'logout':
        return 'logout';
      default:
        return 'info';
    }
  }

  /**
   * Get action chip color class
   */
  getActionColor(action: string): string {
    switch (action.toLowerCase()) {
      case 'create':
        return 'accent';
      case 'delete':
        return 'warn';
      default:
        return 'primary';
    }
  }

  /**
   * Load audit logs with current filters
   */
  loadAuditLogs(): void {
    const request: GetAuditLogsRequest = {
      ...this.auditStore.filters(),
      take: this.pageSize()
    };
    this.auditStore.loadAuditLogs(request);
  }

  /**
   * Apply filter changes and reload data
   * Resets pagination when filters change
   */
  applyFilters(event?: Event): void {
    event?.preventDefault();
    submit(this.filterForm, async () => {
      this.auditStore.resetFilters();
      this.auditStore.loadAuditLogs({
        ...this.toAuditRequest(),
        skip: 0,
        take: this.pageSize()
      });
    });
  }

  /**
   * Reset all filters
   */
  resetFilters(): void {
    this.filterModel.set(INITIAL_AUDIT_FILTERS);
    this.auditStore.resetFilters();
    this.loadAuditLogs();
  }

  /**
   * Toggle expanded row for details view
   */
  toggleExpanded(row: AuditLogItem): void {
    this.expandedRowId.update(id => (id === row.id ? null : row.id));
  }

  /**
   * Format JSON for display
   */
  formatJson(json: string | null): string {
    if (!json) return '';
    try {
      return JSON.stringify(JSON.parse(json), null, 2);
    } catch {
      return json;
    }
  }

  private toAuditRequest(): GetAuditLogsRequest {
    const filters = this.filterModel();
    return {
      ...(filters.entityType ? { entityType: filters.entityType } : {}),
      ...(filters.entityId ? { entityId: filters.entityId } : {}),
      ...(filters.userId ? { userId: filters.userId } : {}),
      ...(filters.fromDate ? { fromDate: `${filters.fromDate}T00:00:00Z` } : {}),
      ...(filters.toDate ? { toDate: `${filters.toDate}T23:59:59Z` } : {})
    };
  }
}

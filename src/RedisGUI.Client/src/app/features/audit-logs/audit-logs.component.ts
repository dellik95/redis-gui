import { Component, inject, signal, computed, ChangeDetectionStrategy, effect } from '@angular/core';
import { DatePipe, SlicePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatSnackBar } from '@angular/material/snack-bar';

import { AuditStore } from '../../core/stores/audit.store';
import { AuthStore } from '../../core/stores/auth.store';
import type { AuditLogItem, GetAuditLogsRequest } from '../../models/audit.types';

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
		FormsModule,
		MatCardModule,
		MatTableModule,
		MatPaginatorModule,
		MatProgressSpinnerModule,
		MatIconModule,
		MatButtonModule,
		MatFormFieldModule,
		MatInputModule,
		MatSelectModule,
		MatDatepickerModule,
		MatNativeDateModule,
		MatChipsModule,
		MatTooltipModule,
		MatExpansionModule
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
	readonly filters = signal<GetAuditLogsRequest>({});
	readonly pageSize = signal(50);
	readonly expandedRowId = signal<string | null>(null);

	// 4. Configuration
	readonly displayedColumns = ['timestamp', 'action', 'entityType', 'entityId', 'userId', 'ipAddress', 'details'] as const;
	readonly entityTypes = ['Connection', 'User', 'Redis'] as const;

	constructor() {
		// Check admin access and load data
		effect(() => {
			if (this.isAdmin()) {
				this.loadAuditLogs();
			} else {
				this.snackBar.open('Access denied. Admin privileges required.', 'Close', { duration: 5000 });
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
			...this.filters(),
			take: this.pageSize()
		};
		this.auditStore.loadAuditLogs(request);
	}

	/**
	 * Apply filter changes and reload data
	 * Resets pagination when filters change
	 */
	applyFilters(): void {
		this.auditStore.resetFilters();
		this.auditStore.loadAuditLogs({
			...this.filters(),
			skip: 0,
			take: this.pageSize()
		});
	}

	/**
	 * Reset all filters
	 */
	resetFilters(): void {
		this.filters.set({});
		this.auditStore.resetFilters();
		this.loadAuditLogs();
	}

	/**
	 * Handle page change event
	 */
	onPageChange(event: PageEvent): void {
		this.pageSize.set(event.pageSize);
		const newSkip = event.pageIndex * event.pageSize;

		this.auditStore.loadAuditLogs({
			...this.filters(),
			skip: newSkip,
			take: event.pageSize
		});
	}

	/**
	 * Toggle expanded row for details view
	 */
	toggleExpanded(row: AuditLogItem): void {
		this.expandedRowId.update(id => id === row.id ? null : row.id);
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

	/**
	 * Update filter value
	 */
	updateFilter(key: keyof GetAuditLogsRequest, value: string | number | Date | null | undefined): void {
		this.filters.update(f => ({
			...f,
			[key]: value || undefined
		}));
	}
}

import { Component, signal, viewChild, input, inject, effect, computed, ChangeDetectionStrategy, DestroyRef, output } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormsModule } from "@angular/forms";

import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatTable, MatTableModule } from "@angular/material/table";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatTooltipModule } from "@angular/material/tooltip";
import { MatDialog } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatPaginatorModule, PageEvent } from "@angular/material/paginator";
import { MatCheckboxModule } from "@angular/material/checkbox";
import { MatMenuModule } from "@angular/material/menu";
import { MatDividerModule } from "@angular/material/divider";
import { MatButtonToggleModule } from "@angular/material/button-toggle";
import { MatTreeModule } from "@angular/material/tree";
import { MatSelectModule } from "@angular/material/select";
import { MatOptionModule } from "@angular/material/core";

import { ScrollingModule } from "@angular/cdk/scrolling";
import { SelectionModel } from "@angular/cdk/collections";

import { TogglerComponent, TogglerConfig } from "../../../../shared/components/toggler/toggler.component";
import { BulkActionsToolbarComponent } from "../bulk-actions-toolbar/bulk-actions-toolbar.component";
import { TreeViewListComponent } from "../../../../shared/components/tree-view-list/tree-view-list.component";
import { ValueEditorOutput } from "../storage-value-editor/storage-value-editor.component";
import { AddKeyComponent } from "../add-key/add-key.component";
import { ImportDialogComponent } from "../import/import-dialog.component";
import { ExportDialogComponent } from "../export/export-dialog.component";
import { ConfirmDialogComponent, PromptDialogComponent } from "../../../../shared/components";
import { filter } from "rxjs/operators";

import { StorageValuesDataSource, StorageValue, StorageValueType } from "../../services/storage-values.datasource";
import { StorageManagementStore } from "../../stores/storage-management.store";
import { RedisKeysStore } from "../../../../core/stores/redis-keys.store";
import { RedisOperationsStore } from "../../../../core/stores/redis-operations.store";
import { RedisDataType, RedisValue } from "../../../../models/redis-keys.types";

/**
 * Storage Values List Component
 * Main component for displaying and managing Redis keys
 *
 * Features:
 * - Table and tree view modes
 * - Key search and filtering
 * - Bulk operations (delete, export, import)
 * - Key selection and editing
 * - Pagination
 * - Real-time updates
 *
 * @example
 * ```html
 * <app-storage-values-list
 *   [connectionId]="connectionId"
 *   (keySelected)="onKeySelected($event)"
 * />
 * ```
 */
@Component({
	selector: "app-storage-values-list",
	standalone: true,
	templateUrl: "./storage-values-list.component.html",
	styleUrl: "./storage-values-list.component.scss",
	changeDetection: ChangeDetectionStrategy.OnPush,
	imports: [
		MatTableModule,
		ScrollingModule,
		MatTreeModule,
		MatIconModule,
		MatButtonModule,
		TogglerComponent,
		TreeViewListComponent,
		BulkActionsToolbarComponent,
		MatFormFieldModule,
		MatInputModule,
		MatProgressSpinnerModule,
		MatTooltipModule,
		MatPaginatorModule,
		MatCheckboxModule,
		MatMenuModule,
		MatDividerModule,
		MatButtonToggleModule,
		FormsModule,
		MatSelectModule,
		MatOptionModule
	],
})
export class StorageValuesListComponent {
	// 1. Injected dependencies
	private readonly dialog = inject(MatDialog);
	private readonly snackBar = inject(MatSnackBar);
	private readonly storageStore = inject(StorageManagementStore);
	private readonly redisKeysStore = inject(RedisKeysStore);
	private readonly redisOperationsStore = inject(RedisOperationsStore);
	private readonly destroyRef = inject(DestroyRef);

	// 2. Inputs
	readonly connectionId = input.required<string>();

	// 3. Outputs
	readonly keySelected = output<{ key: string, type: RedisDataType, value: RedisValue, ttl: number }>();

	// 4. View queries
	readonly table = viewChild(MatTable);

	// 5. State signals
	readonly viewMode = signal<"table" | "tree">("table");
	readonly keyDelimiter = signal<string>(":");
	readonly searchPattern = signal<string>("*");
	readonly isLoading = signal<boolean>(false);
	readonly chunkLoading = signal<boolean>(false);
	readonly totalCount = signal<number>(0);
	readonly pageSize = signal<number>(50);
	readonly pageNumber = signal<number>(1);

	// 6. Data source and selection
	readonly dataSource = new StorageValuesDataSource();
	readonly selection = new SelectionModel<string>(true, []);

	// 7. Configuration
	readonly displayedColumns: readonly string[] = ["select", "type", "key", "ttl", "size", "actions"] as const;
	readonly modesTogglerConfig: TogglerConfig = {
		defaultValue: "table",
		items: [
			{ value: "table", icon: "format_list_bulleted" },
			{ value: "tree", icon: "account_tree" }
		]
	};

	// 8. Filter/sort state
	readonly typeFilter = signal<string>("");
	readonly sortField = signal<string>("key");
	readonly sortDir = signal<"asc" | "desc">("asc");

	// 9. Private state
	private readonly previousConnectionId = signal<string | null>(null);

	// 10. TrackBy functions for performance
	readonly trackByKey = (index: number, item: StorageValue) => item.key;

	// 11. Constructor with effects
	// 8. Constructor with effects
	constructor() {
		// Watch for loading state changes from data source
		effect(() => {
			this.isLoading.set(this.dataSource.loading$());
		});

		// Load keys when connectionId becomes available or changes
		effect(() => {
			const connId = this.connectionId();
			const prevId = this.previousConnectionId();

			// Only load if connectionId changed to avoid unnecessary reloads
			if (connId && connId !== prevId) {
				this.previousConnectionId.set(connId);
				this.loadKeys();
			}
		});

		// Apply filters when data changes in table mode
		effect(() => {
			const data = this.dataSource.data$();
			const mode = this.viewMode();

			// Only apply filters in table mode and if we have data
			if (mode === 'table' && data.length > 0 && this.table()) {
				// Use requestAnimationFrame to ensure table is ready before applying filters
				requestAnimationFrame(() => {
					this.applyFilters();
				});
			}
		});

		// Cleanup on destroy
		this.destroyRef.onDestroy(() => {
			this.dataSource.disconnect();
		});
	}

	/**
	 * Handles virtual scroll events
	 * Loads more data chunks when scrolling near the end
	 *
	 * @param index - Current scroll index
	 */
	onScroll(index: number): void {
		const dataLength = this.dataSource.data$().length;
		if (dataLength >= this.pageSize() * this.pageNumber() && index > dataLength - 20 && !this.chunkLoading()) {
			this.chunkLoading.set(true);
			// Use requestAnimationFrame for better performance than setTimeout
			requestAnimationFrame(() => {
				requestAnimationFrame(() => {
					this.chunkLoading.set(false);
				});
			});
		}
	}

	// 7. Public methods
	/**
	 * Apply filters and sorting to table data
	 * Updates the Material table data source with filtered and sorted data
	 */
	applyFilters(): void {
		let data = this.dataSource.data$();
		const filter = this.typeFilter();

		if (filter) {
			data = data.filter((d) => d.type === filter);
		}

		data = [...data].sort((a, b) => {
			let vA: string | number;
			let vB: string | number;
			const field = this.sortField();

			switch (field) {
				case "key":
					vA = a.key;
					vB = b.key;
					break;
				case "type":
					vA = a.type;
					vB = b.type;
					break;
				case "ttl":
					vA = Number(a.ttl ?? 0);
					vB = Number(b.ttl ?? 0);
					break;
				case "size":
					vA = Number(a.size);
					vB = Number(b.size);
					break;
				default:
					vA = a.key;
					vB = b.key;
			}

			const dir = this.sortDir();
			if (vA < vB) return dir === "asc" ? -1 : 1;
			if (vA > vB) return dir === "asc" ? 1 : -1;
			return 0;
		});

		// Check if table is available before setting dataSource
		const table = this.table();
		if (table) {
			table.dataSource = data;
		}
	}

	toggleViewMode(mode: string): void {
		this.viewMode.set(mode as "table" | "tree");
		this.loadKeys();
	}

	onDelimiterChange(delimiter: string): void {
		this.keyDelimiter.set(delimiter);
		this.loadKeys();
	}

	changeKeyDelimiter(): void {
		// Cycle through common delimiters
		const delimiters = [':', '.', '/', '-', '_'];
		const current = this.keyDelimiter();
		const currentIndex = delimiters.indexOf(current);
		const nextIndex = (currentIndex + 1) % delimiters.length;
		const nextDelimiter = delimiters[nextIndex];
		if (nextDelimiter) {
			this.keyDelimiter.set(nextDelimiter);
			this.loadKeys();
		}
	}

	onSearchPatternChange(pattern: string): void {
		this.searchPattern.set(pattern || "*");
		this.loadKeys();
	}

	loadKeys(): void {
		// Update store preferences and load keys
		this.storageStore.loadKeys(this.searchPattern(), this.keyDelimiter());
		// Data source will react to store changes via effect
	}

	addNewKey(): void {
		const dialogRef = this.dialog.open(AddKeyComponent, {
			width: "900px",
			maxHeight: "90vh",
			data: {
				connectionId: this.connectionId()
			},
		});

		dialogRef.afterClosed()
			.pipe(takeUntilDestroyed(this.destroyRef))
			.subscribe((result: ValueEditorOutput | undefined) => {
				if (result) {
					this.isLoading.set(true);
					// Use store directly for adding new keys
					this.redisKeysStore.addValue({
						connectionId: this.connectionId(),
						key: result.key,
						value: typeof result.value === 'object' && 'value' in result.value
							? String(result.value.value)
							: String(result.value),
					...(result.ttl !== undefined ? { ttl: result.ttl } : {})
					});
					this.redisKeysStore.clearCache();
					this.snackBar.open("Key created successfully", "Close", { duration: 3000 });
					this.loadKeys();
					this.isLoading.set(false);
				}
			});
	}

	deleteKey(key: string): void {
		this.dialog.open(ConfirmDialogComponent, {
			data: {
				title: 'Delete Key',
				message: `Are you sure you want to delete the key "${key}"?`,
				confirmText: 'Delete',
				cancelText: 'Cancel',
				confirmColor: 'warn'
			}
		}).afterClosed()
			.pipe(filter((confirmed): confirmed is boolean => confirmed === true))
			.subscribe(() => {
				this.isLoading.set(true);
				this.redisKeysStore.deleteKey({
					connectionId: this.connectionId(),
					key
				});
				this.redisKeysStore.clearCache();
				this.snackBar.open("Key deleted successfully", "Close", { duration: 3000 });
				this.loadKeys();
				this.isLoading.set(false);
			});
	}

	editKey(key: string): void {
		// Find the key data from current data source
		const data = this.dataSource.data$();
		const keyData = data.find(item => item.key === key);

		if (keyData) {
			// Map StorageValueType to RedisDataType
			const redisType = this.mapTypeToRedisDataType(keyData.type);

			// Emit keySelected event to show edit panel
			// The value will be loaded by the edit component
			this.keySelected.emit({
				key: keyData.key,
				type: redisType,
				value: null as any, // Will be loaded by edit component
				ttl: keyData.ttl ?? -1
			});

			// Also load the value in the store for the edit component
			this.redisKeysStore.loadValue({ connectionId: this.connectionId(), key });
		} else {
			// Fallback: if key not found in current data, still try to load it
			// This can happen if the key is filtered out or not yet loaded
			this.snackBar.open("Loading key data...", "Close", { duration: 2000 });
			this.redisKeysStore.loadValue({ connectionId: this.connectionId(), key });

			// Emit with default type - will be updated when value loads
			this.keySelected.emit({
				key: key,
				type: RedisDataType.String, // Default, will be updated when loaded
				value: null as any,
				ttl: -1
			});
		}
	}

	/**
	 * Handle key selection from tree view
	 */
	onTreeKeySelected(event: { key: string, type: RedisDataType, value: RedisValue, ttl: number }): void {
		// Emit to parent for sidebar editing
		this.keySelected.emit(event);
	}

	/**
	 * Maps StorageValueType to RedisDataType enum
	 *
	 * @param type - Storage value type from data source
	 * @returns Corresponding RedisDataType enum value
	 */
	private mapTypeToRedisDataType(type: StorageValueType): RedisDataType {
		switch (type) {
			case StorageValueType.STRING:
				return RedisDataType.String;
			case StorageValueType.LIST:
				return RedisDataType.List;
			case StorageValueType.SET:
				return RedisDataType.Set;
			case StorageValueType.HASH:
				return RedisDataType.Hash;
			case StorageValueType.SORTED_SET:
				return RedisDataType.SortedSet;
			case StorageValueType.STREAM:
				return RedisDataType.Stream;
			default:
				return RedisDataType.String;
		}
	}

	getTypeIcon(type: StorageValueType): string {
		switch (type) {
			case StorageValueType.STRING:
				return "text_fields";
			case StorageValueType.LIST:
				return "list";
			case StorageValueType.SET:
				return "group_work";
			case StorageValueType.HASH:
				return "tag";
			case StorageValueType.SORTED_SET:
				return "sort";
			case StorageValueType.STREAM:
				return "stream";
			default:
				return "description";
		}
	}

	// ========== Pagination ==========

	onPageChange(event: PageEvent): void {
		this.pageSize.set(event.pageSize);
		this.pageNumber.set(event.pageIndex + 1);
		this.loadKeys();
	}

	// ========== Selection ==========

	isAllSelected(): boolean {
		const numSelected = this.selection.selected.length;
		const currentPageData = this.dataSource.data$();
		return numSelected === currentPageData.length && currentPageData.length > 0;
	}

	masterToggle(): void {
		if (this.isAllSelected()) {
			this.selection.clear();
		} else {
			const currentPageData = this.dataSource.data$();
			currentPageData.forEach((row: StorageValue) => this.selection.select(row.key));
		}
	}

	// ========== Bulk Operations ==========

	deleteSelected(): void {
		const selectedKeys = this.selection.selected;

		if (selectedKeys.length === 0) {
			this.snackBar.open("No keys selected", "Close", { duration: 3000 });
			return;
		}

		this.dialog.open(ConfirmDialogComponent, {
			data: {
				title: 'Delete Selected Keys',
				message: `Are you sure you want to delete ${selectedKeys.length} selected key(s)?`,
				confirmText: 'Delete',
				cancelText: 'Cancel',
				confirmColor: 'warn'
			}
		}).afterClosed()
			.pipe(filter((confirmed): confirmed is boolean => confirmed === true))
			.subscribe(() => {
				this.isLoading.set(true);
				this.redisOperationsStore.deleteMultipleKeys({
					connectionId: this.connectionId(),
					keys: selectedKeys
				});
				this.redisKeysStore.clearCache();
				this.snackBar.open(`Deleted ${selectedKeys.length} key(s) successfully`, "Close", { duration: 5000 });
				this.selection.clear();
				this.loadKeys();
				this.isLoading.set(false);
			});
	}

	deleteByPattern(): void {
		this.dialog.open(PromptDialogComponent, {
			data: {
				title: 'Delete by Pattern',
				message: 'Enter pattern to delete (e.g., user:*, session:*):',
				placeholder: 'Pattern',
				defaultValue: this.searchPattern(),
				required: true
			}
		}).afterClosed()
			.pipe(filter((pattern): pattern is string => pattern !== null && pattern.trim().length > 0))
			.subscribe(pattern => {
				this.dialog.open(ConfirmDialogComponent, {
					data: {
						title: 'Confirm Delete',
						message: `Are you sure you want to delete all keys matching "${pattern}"?`,
						confirmText: 'Delete',
						cancelText: 'Cancel',
						confirmColor: 'warn'
					}
				}).afterClosed()
					.pipe(filter((confirmed): confirmed is boolean => confirmed === true))
					.subscribe(() => {
						this.isLoading.set(true);
						this.redisOperationsStore.deleteKeysByPattern({
							connectionId: this.connectionId(),
							pattern
						});
						this.redisKeysStore.clearCache();
						this.snackBar.open(`Deleted keys matching "${pattern}"`, "Close", { duration: 5000 });
						this.loadKeys();
						this.isLoading.set(false);
					});
			});
	}

	// ========== Export/Import ==========

	exportData(format: "json" | "csv"): void {
	const ref = this.dialog.open(ExportDialogComponent, {
		data: { connectionId: this.connectionId() }
	});
	ref.afterClosed().subscribe(success => {
		if (success) this.snackBar.open('Export started!', 'Close', { duration: 3000 });
	});
}

	importData(): void {
		const dialogRef = this.dialog.open(ImportDialogComponent, {
			data: { connectionId: this.connectionId() }
		});

		dialogRef.afterClosed()
			.pipe(takeUntilDestroyed(this.destroyRef))
			.subscribe((success) => {
				if (success) {
					this.redisKeysStore.clearCache();
					this.snackBar.open('Data imported successfully', 'Close', { duration: 3000 });
					this.loadKeys();
				}
			});
	}
}

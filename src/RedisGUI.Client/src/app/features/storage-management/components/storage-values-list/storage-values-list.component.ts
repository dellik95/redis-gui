import { CommonModule } from "@angular/common";
import { Component, OnInit, signal, viewChild, input, inject, effect, ChangeDetectionStrategy, DestroyRef, output } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatTable, MatTableModule } from "@angular/material/table";
import { ScrollingModule } from "@angular/cdk/scrolling";
import { MatTreeModule } from "@angular/material/tree";
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
import { SelectionModel } from "@angular/cdk/collections";
import { FormsModule } from "@angular/forms";
import { MatSelectModule } from "@angular/material/select";
import { MatOptionModule } from "@angular/material/core";
import { finalize } from "rxjs";
import { TogglerComponent, TogglerConfig } from "../../../../shared/components/toggler/toggler.component";
import { StorageValuesDataSource, StorageValue, StorageValueType } from "../../services/storage-values.datasource";
import { BulkActionsToolbarComponent } from "../bulk-actions-toolbar/bulk-actions-toolbar.component";
import { TreeViewListComponent } from "../../../../shared/components/tree-view-list/tree-view-list.component";
import { ValueEditorDialogComponent } from "../value-editor-dialog/value-editor-dialog.component";
import { RedisKeysService } from "../../../../services/api/redis-keys.service";
import { RedisOperationsService } from "../../../../services/api/redis-operations.service";
import { RedisDataType } from "../../../../models/redis-keys.types";

@Component({
	selector: "app-storage-values-list",
	templateUrl: "./storage-values-list.component.html",
	styleUrls: ["./storage-values-list.component.scss"],
	changeDetection: ChangeDetectionStrategy.OnPush,
	imports: [
		MatTableModule,
		ScrollingModule,
		MatTreeModule,
		MatIconModule,
		CommonModule,
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
		FormsModule,
		MatSelectModule,
		MatOptionModule
	],
})
export class StorageValuesListComponent implements OnInit {
	private readonly dialog = inject(MatDialog);
	private readonly snackBar = inject(MatSnackBar);
	private readonly redisKeysService = inject(RedisKeysService);
	private readonly redisOperationsService = inject(RedisOperationsService);
	private readonly destroyRef = inject(DestroyRef);

	// Input from parent component
	connectionId = input.required<string>();

	// Output to parent when key is selected for inline editing
	keySelected = output<{ key: string, type: RedisDataType, value: any, ttl: number }>();

	displayedColumns: string[] = ["select", "type", "key", "ttl", "size", "actions"];

	viewMode = signal<"table" | "tree">("table");
	keyDelimiter = signal<string>(":");
	searchPattern = signal<string>("*");
	isLoading = signal<boolean>(false);

	// Pagination
	totalCount = signal<number>(0);
	pageSize = signal<number>(50);
	pageNumber = signal<number>(1);

	// Selection
	selection = new SelectionModel<string>(true, []);

	readonly dataSource = new StorageValuesDataSource();
	readonly table = viewChild.required(MatTable);

	modesTogglerConfig: TogglerConfig = {
		defaultValue: "table",
		items: [
			{
				value: "table",
				icon: "format_list_bulleted",
			},
			{
				value: "tree",
				icon: "account_tree",
			},
		],
	};

	typeFilter: string = "";
	sortField: string = "key";
	sortDir: string = "asc";

	// Virtualization scroll handler for buffer tuning / chunked loading
	chunkLoading = signal<boolean>(false);

	ngOnInit(): void {
		this.loadKeys();

		// Subscribe to loading state
		effect(() => {
			this.isLoading.set(this.dataSource.loading$());
		}, { allowSignalWrites: true });
	}

	ngOnDestroy(): void {
		this.dataSource.disconnect();
	}

	onScroll(index: number): void {
		const dataLength = this.dataSource.data$().length;
		if (dataLength >= this.pageSize() * this.pageNumber() && index > dataLength - 20 && !this.chunkLoading()) {
			this.chunkLoading.set(true);
			setTimeout(() => {
				this.chunkLoading.set(false);
			}, 800);
		}
	}

	applyFilters(): void {
		let data = this.dataSource.data$();
		if (this.typeFilter) {
			data = data.filter((d) => d.type === this.typeFilter);
		}
		data = [...data].sort((a, b) => {
			let vA: string | number;
			let vB: string | number;
			switch (this.sortField) {
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
			if (vA < vB) return this.sortDir === "asc" ? -1 : 1;
			if (vA > vB) return this.sortDir === "asc" ? 1 : -1;
			return 0;
		});
		this.table().dataSource = data;
	}

	ngAfterViewInit(): void {
		effect(() => {
			this.applyFilters();
		}, { allowSignalWrites: true });
	}

	toggleViewMode(mode: string): void {
		this.viewMode.set(mode as "table" | "tree");
		this.loadKeys();
	}

	changeKeyDelimiter(): void {
		const newDelimiter = this.keyDelimiter() === ":" ? "/" : ":";
		this.keyDelimiter.set(newDelimiter);
		this.loadKeys();
	}

	onSearchPatternChange(pattern: string): void {
		this.searchPattern.set(pattern || "*");
		this.loadKeys();
	}

	loadKeys(): void {
		const mode = this.viewMode() === "table" ? "flat" : "tree";
		this.dataSource.loadKeys(this.connectionId(), this.searchPattern(), this.keyDelimiter(), mode);
		// Clear cache after modifying data
		this.redisKeysService.clearCache();
	}

	addNewKey(): void {
		const dialogRef = this.dialog.open(ValueEditorDialogComponent, {
			width: "900px",
			maxHeight: "90vh",
			data: {
				key: "",
				type: RedisDataType.String,
				value: "",
				isNew: true,
			},
		});

		dialogRef.afterClosed()
			.pipe(takeUntilDestroyed(this.destroyRef))
			.subscribe((result) => {
				if (result) {
					this.isLoading.set(true);
					this.redisKeysService.updateValue(this.connectionId(), result.key, result.type, result.value, result.ttl)
						.pipe(
							finalize(() => this.isLoading.set(false)),
							takeUntilDestroyed(this.destroyRef)
						)
						.subscribe({
							next: () => {
								this.snackBar.open("Key created successfully", "Close", { duration: 3000 });
								this.loadKeys();
							},
							error: (err: Error) => {
								this.snackBar.open(`Error creating key: ${err.message}`, "Close", { duration: 5000 });
							},
						});
				}
			});
	}

	deleteKey(key: string): void {
		if (confirm(`Are you sure you want to delete the key "${key}"?`)) {
			this.isLoading.set(true);
			this.redisKeysService.deleteKey(this.connectionId(), key)
				.pipe(
					finalize(() => this.isLoading.set(false)),
					takeUntilDestroyed(this.destroyRef)
				)
				.subscribe({
					next: () => {
						this.snackBar.open("Key deleted successfully", "Close", { duration: 3000 });
						this.loadKeys();
					},
					error: (err: Error) => {
						this.snackBar.open(`Error deleting key: ${err.message}`, "Close", { duration: 5000 });
					},
				});
		}
	}

	editKey(key: string): void {
		this.isLoading.set(true);
		this.redisKeysService.getValue(this.connectionId(), key)
			.pipe(
				finalize(() => this.isLoading.set(false)),
				takeUntilDestroyed(this.destroyRef)
			)
			.subscribe({
				next: (currentValue) => {
					// Emit to parent for inline editing
					this.keySelected.emit({
						key: key,
						type: this.mapTypeToRedisDataType(currentValue.type),
						value: currentValue.value,
						ttl: currentValue.ttl
					});
				},
				error: (err: Error) => {
					this.snackBar.open(`Error loading key: ${err.message}`, "Close", { duration: 5000 });
				},
			});
	}

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

		if (!confirm(`Delete ${selectedKeys.length} selected key(s)?`)) {
			return;
		}

		this.isLoading.set(true);
		this.redisOperationsService.deleteMultipleKeys(this.connectionId(), selectedKeys)
			.pipe(
				finalize(() => this.isLoading.set(false)),
				takeUntilDestroyed(this.destroyRef)
			)
			.subscribe({
				next: (response) => {
					this.snackBar.open(`Deleted ${response.deletedCount} keys. Failed: ${response.failedKeys.length}`, "Close", { duration: 5000 });
					this.selection.clear();
					this.loadKeys();
				},
				error: (err: Error) => {
					this.snackBar.open(`Error deleting keys: ${err.message}`, "Close", { duration: 5000 });
				},
			});
	}

	deleteByPattern(): void {
		const pattern = prompt("Enter pattern to delete (e.g., user:*, session:*):", this.searchPattern());

		if (!pattern) return;

		if (!confirm(`Delete all keys matching "${pattern}"?`)) {
			return;
		}

		this.isLoading.set(true);
		this.redisOperationsService.deleteKeysByPattern(this.connectionId(), pattern)
			.pipe(
				finalize(() => this.isLoading.set(false)),
				takeUntilDestroyed(this.destroyRef)
			)
			.subscribe({
				next: (response) => {
					this.snackBar.open(`Deleted ${response.deletedCount} keys matching "${response.pattern}"`, "Close", { duration: 5000 });
					this.loadKeys();
				},
				error: (err: Error) => {
					this.snackBar.open(`Error deleting by pattern: ${err.message}`, "Close", { duration: 5000 });
				},
			});
	}

	// ========== Export/Import ==========

	exportData(format: "json" | "csv"): void {
		this.isLoading.set(true);
		this.redisOperationsService.exportData(this.connectionId(), this.searchPattern(), format)
			.pipe(
				finalize(() => this.isLoading.set(false)),
				takeUntilDestroyed(this.destroyRef)
			)
			.subscribe({
				next: (blob) => {
					const url = window.URL.createObjectURL(blob);
					const link = document.createElement("a");
					link.href = url;
					link.download = `redis_export_${new Date().toISOString()}.${format}`;
					link.click();
					window.URL.revokeObjectURL(url);

					this.snackBar.open(`Data exported as ${format.toUpperCase()}`, "Close", { duration: 3000 });
				},
				error: (err: Error) => {
					this.snackBar.open(`Error exporting data: ${err.message}`, "Close", { duration: 5000 });
				},
			});
	}

	importData(): void {
		const input = document.createElement("input");
		input.type = "file";
		input.accept = ".json,.csv";

		input.onchange = (event: any) => {
			const file = event.target.files[0];
			if (!file) return;

			const reader = new FileReader();
			reader.onload = (e: any) => {
				const data = e.target.result;
				const format = file.name.endsWith(".csv") ? "csv" : "json";
				const overwrite = confirm("Overwrite existing keys?");

				this.isLoading.set(true);
				this.redisOperationsService
					.importData(this.connectionId(), {
						data,
						format,
						overwrite,
					})
					.pipe(
						finalize(() => this.isLoading.set(false)),
						takeUntilDestroyed(this.destroyRef)
					)
					.subscribe({
						next: (response) => {
							this.snackBar.open(`Imported: ${response.importedCount}, Skipped: ${response.skippedCount}, Failed: ${response.failedCount}`, "Close", { duration: 5000 });
							this.loadKeys();
						},
						error: (err: Error) => {
							this.snackBar.open(`Error importing data: ${err.message}`, "Close", { duration: 5000 });
						},
					});
			};

			reader.readAsText(file);
		};

		input.click();
	}
}

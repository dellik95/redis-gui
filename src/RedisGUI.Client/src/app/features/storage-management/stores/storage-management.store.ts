import { computed, inject } from '@angular/core';
import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, tap, of, switchMap } from 'rxjs';
import { RedisKeysStore } from '../../../core/stores/redis-keys.store';
import { ValueEditorOutput } from '../components/storage-value-editor/storage-value-editor.component';

// Alias for backward compatibility
export type ValueEditorData = ValueEditorOutput;

/**
 * Storage management state interface
 */
interface StorageManagementState {
	/**
	 * Current connection ID
	 */
	connectionId: string | null;

	/**
	 * Currently selected key for editing
	 */
	selectedKey: ValueEditorData | null;

	/**
	 * Whether a save operation is in progress
	 */
	isSaving: boolean;

	/**
	 * Last operation error
	 */
	error: string | null;

	/**
	 * View preferences
	 */
	viewPreferences: {
		pattern: string;
		delimiter: string;
		viewMode: 'tree' | 'flat';
	} | null;
}

/**
 * Initial state
 */
const initialState: StorageManagementState = {
	connectionId: null,
	selectedKey: null,
	isSaving: false,
	error: null,
	viewPreferences: null
};

/**
 * Storage management store
 * Coordinates storage management operations and state
 *
 * Features:
 * - Unified state management for storage operations
 * - Key selection and editing coordination
 * - View preferences persistence
 * - Error handling
 *
 * @example
 * ```typescript
 * private storageStore = inject(StorageManagementStore);
 *
 * // Initialize with connection
 * this.storageStore.initialize('conn-123');
 *
 * // Load keys
 * this.storageStore.loadKeys('user:*', ':', 'tree');
 *
 * // Select key for editing
 * this.storageStore.selectKey(keyData);
 *
 * // Save value
 * this.storageStore.saveValue(valueData).subscribe();
 * ```
 */
export const StorageManagementStore = signalStore(
	{ providedIn: 'root' },

	// Define state
	withState(initialState),

	// Define computed signals
	withComputed((store) => ({
		/**
		 * Whether a connection is active
		 */
		hasConnection: computed(() => store.connectionId() !== null),

		/**
		 * Whether a key is selected for editing
		 */
		hasSelectedKey: computed(() => store.selectedKey() !== null),

		/**
		 * Whether any operation is in progress
		 */
		isOperationInProgress: computed(() =>
			store.isSaving() || store.connectionId() === null
		)
	})),

	// Define methods
	withMethods((store, redisKeysStore = inject(RedisKeysStore)) => {
		return {
			/**
			 * Initialize store with connection ID
			 * @param connectionId - The connection ID
			 */
			initialize(connectionId: string): void {
				patchState(store, {
					connectionId,
					error: null,
					selectedKey: null,
					isSaving: false
				});
			},

			/**
			 * Load keys with current preferences
			 * @param pattern - Optional pattern override
			 * @param delimiter - Optional delimiter override (for component display grouping)
			 */
			loadKeys(
				pattern?: string,
				delimiter?: string
			): void {
				const connectionId = store.connectionId();
				if (!connectionId) {
					patchState(store, { error: 'No connection selected' });
					return;
				}

				const prefs = store.viewPreferences();
				const finalPattern = pattern ?? prefs?.pattern ?? '*';
				const finalDelimiter = delimiter ?? prefs?.delimiter ?? ':';

				// Update preferences (keep delimiter for component grouping, viewMode removed)
				patchState(store, {
					viewPreferences: {
						pattern: finalPattern,
						delimiter: finalDelimiter,
						viewMode: prefs?.viewMode ?? 'tree' // Keep for backward compatibility but not used in API
					}
				});

				// Call store directly instead of going through service
				redisKeysStore.loadKeys({
					connectionId,
					pattern: finalPattern
				});
			},

			/**
			 * Select a key for editing
			 * @param keyData - The key data to select
			 */
			selectKey(keyData: ValueEditorData | null): void {
				patchState(store, { selectedKey: keyData });
			},

			/**
			 * Save a value
			 * @param valueData - The value data to save
			 */
			saveValue: rxMethod<ValueEditorData>(
				pipe(
					tap(() => {
						patchState(store, { isSaving: true, error: null });
					}),
					switchMap((valueData) => {
						const connectionId = store.connectionId();
						if (!connectionId) {
							patchState(store, {
								error: 'No connection selected',
								isSaving: false
							});
							return of(null);
						}

						// Call store directly instead of going through service
						redisKeysStore.updateValue({
							connectionId,
							key: valueData.key,
							type: valueData.type,
							value: valueData.value,
							...(valueData.ttl !== undefined ? { ttl: valueData.ttl } : {})
						});

						// Clear cache to force refresh
						redisKeysStore.clearCache();

						// Reload keys
						const prefs = store.viewPreferences();
						if (prefs) {
							redisKeysStore.loadKeys({
								connectionId,
								pattern: prefs.pattern
							});
						}

						patchState(store, {
							isSaving: false,
							selectedKey: null,
							error: null
						});

						return of(null);
					})
				)
			),

			/**
			 * Delete a key
			 * @param key - The key to delete
			 */
			deleteKey: rxMethod<string>(
				pipe(
					switchMap((key) => {
						const connectionId = store.connectionId();
						if (!connectionId) {
							patchState(store, { error: 'No connection selected' });
							return of(null);
						}

						// Call store directly instead of going through service
						redisKeysStore.deleteKey({ connectionId, key });

						// Clear cache and reload
						redisKeysStore.clearCache();
						const prefs = store.viewPreferences();
						if (prefs) {
							redisKeysStore.loadKeys({
								connectionId,
								pattern: prefs.pattern
							});
						}

						// Clear selection if deleted key was selected
						if (store.selectedKey()?.key === key) {
							patchState(store, { selectedKey: null });
						}

						patchState(store, { error: null });

						return of(null);
					})
				)
			),

			/**
			 * Cancel current edit operation
			 */
			cancelEdit(): void {
				patchState(store, { selectedKey: null, error: null });
			},

			/**
			 * Clear error state
			 */
			clearError(): void {
				patchState(store, { error: null });
			},

			/**
			 * Reset store to initial state
			 */
			reset(): void {
				patchState(store, initialState);
			}
		};
	})
);


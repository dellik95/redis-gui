import { computed, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { catchError, of, pipe, switchMap, tap } from 'rxjs';
import type {
	ServerInfo,
	ServerConfig,
	ConfigParameter,
	SetConfigRequest,
	SetConfigResponse
} from '../../models/server-info.types';

/**
 * Server info state interface
 */
interface ServerInfoState {
	/**
	 * Server information data
	 */
	serverInfo: ServerInfo | null;

	/**
	 * Configuration parameters
	 */
	config: ConfigParameter[];

	/**
	 * Loading state for server info
	 */
	isLoadingInfo: boolean;

	/**
	 * Loading state for config
	 */
	isLoadingConfig: boolean;

	/**
	 * Loading state for config update
	 */
	isUpdatingConfig: boolean;

	/**
	 * Error message
	 */
	error: string | null;
}

/**
 * Initial server info state
 */
const initialState: ServerInfoState = {
	serverInfo: null,
	config: [],
	isLoadingInfo: false,
	isLoadingConfig: false,
	isUpdatingConfig: false,
	error: null
};

/**
 * Formats bytes to human-readable string
 * @param bytes - Number of bytes
 * @returns Formatted string (e.g., "1.5 GB")
 */
function formatBytes(bytes: number): string {
	if (bytes === 0) return '0 B';

	const k = 1024;
	const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
	const i = Math.floor(Math.log(bytes) / Math.log(k));

	return `${(bytes / Math.pow(k, i)).toFixed(2)} ${sizes[i]}`;
}

/**
 * Global server info store using NgRx SignalStore
 * Provides centralized state management for Redis server information and configuration
 *
 * Features:
 * - Server info loading with computed metrics
 * - Configuration management (load, update)
 * - Memory and uptime formatting
 *
 * @example
 * ```typescript
 * // Inject the store
 * private serverInfoStore = inject(ServerInfoStore);
 *
 * // Load server info
 * this.serverInfoStore.loadServerInfo('conn-123');
 *
 * // Read computed values
 * const memoryPercent = this.serverInfoStore.memoryUsagePercent();
 * const uptime = this.serverInfoStore.uptimeFormatted();
 *
 * // Load and update config
 * this.serverInfoStore.loadConfig({ connectionId: 'conn-123', pattern: 'max*' });
 * ```
 */
export const ServerInfoStore = signalStore(
	{ providedIn: 'root' },

	// Define state
	withState(initialState),

	// Define computed signals
	withComputed((store) => ({
		/**
		 * Memory usage percentage
		 */
		memoryUsagePercent: computed(() => {
			const info = store.serverInfo();
			if (!info || !info.totalSystemMemoryBytes) return 0;
			return (info.usedMemoryBytes / info.totalSystemMemoryBytes) * 100;
		}),

		/**
		 * Formatted uptime string (e.g., "5d 3h 45m")
		 */
		uptimeFormatted: computed(() => {
			const info = store.serverInfo();
			if (!info) return '';

			const seconds = info.uptimeInSeconds;
			const days = Math.floor(seconds / 86400);
			const hours = Math.floor((seconds % 86400) / 3600);
			const minutes = Math.floor((seconds % 3600) / 60);

			return `${days}d ${hours}h ${minutes}m`;
		}),

		/**
		 * Formatted used memory string
		 */
		memoryUsedFormatted: computed(() => {
			const info = store.serverInfo();
			if (!info) return '';
			return formatBytes(info.usedMemoryBytes);
		}),

		/**
		 * Formatted total memory string
		 */
		memoryTotalFormatted: computed(() => {
			const info = store.serverInfo();
			if (!info) return '';
			return formatBytes(info.totalSystemMemoryBytes);
		}),

		/**
		 * Whether any loading operation is in progress
		 */
		isLoading: computed(() => {
			return store.isLoadingInfo() || store.isLoadingConfig() || store.isUpdatingConfig();
		}),

		/**
		 * Hit rate percentage
		 */
		hitRatePercent: computed(() => {
			const info = store.serverInfo();
			if (!info) return 0;
			return info.hitRate;
		}),

		/**
		 * Number of connected clients
		 */
		clientsCount: computed(() => {
			const info = store.serverInfo();
			return info?.connectedClients ?? 0;
		})
	})),

	// Define methods
	withMethods((store, httpClient = inject(HttpClient)) => {
		const BASE_URL = '/api/v1/connections';

		return {
			/**
			 * Load server info for a connection
			 */
			loadServerInfo: rxMethod<string>(
				pipe(
					tap(() => patchState(store, { isLoadingInfo: true, error: null })),
					switchMap((connectionId) =>
						httpClient.get<ServerInfo>(`${BASE_URL}/${connectionId}/info`).pipe(
							tap((serverInfo) =>
								patchState(store, {
									serverInfo,
									isLoadingInfo: false
								})
							),
							catchError((error) => {
								patchState(store, {
									error: error.message || 'Failed to load server info',
									isLoadingInfo: false
								});
								return of(null);
							})
						)
					)
				)
			),

			/**
			 * Load configuration for a connection
			 */
			loadConfig: rxMethod<{ connectionId: string; pattern?: string }>(
				pipe(
					tap(() => patchState(store, { isLoadingConfig: true, error: null })),
					switchMap(({ connectionId, pattern }) => {
						let params = new HttpParams();
						if (pattern) {
							params = params.set('pattern', pattern);
						}

						return httpClient
							.get<ServerConfig>(`${BASE_URL}/${connectionId}/config`, { params })
							.pipe(
								tap((result) =>
									patchState(store, {
										config: result.parameters,
										isLoadingConfig: false
									})
								),
								catchError((error) => {
									patchState(store, {
										error: error.message || 'Failed to load config',
										isLoadingConfig: false
									});
									return of(null);
								})
							);
					})
				)
			),

			/**
			 * Update a configuration parameter
			 */
			updateConfig: rxMethod<{ connectionId: string; request: SetConfigRequest }>(
				pipe(
					tap(() => patchState(store, { isUpdatingConfig: true, error: null })),
					switchMap(({ connectionId, request }) =>
						httpClient
							.post<SetConfigResponse>(`${BASE_URL}/${connectionId}/config`, request)
							.pipe(
								tap((response) => {
									if (response.success) {
										// Update the config in state
										const updatedConfig = store.config().map((param) =>
											param.name === response.parameterName
												? { ...param, value: response.newValue }
												: param
										);
										patchState(store, {
											config: updatedConfig,
											isUpdatingConfig: false
										});
									} else {
										patchState(store, {
											error: response.message,
											isUpdatingConfig: false
										});
									}
								}),
								catchError((error) => {
									patchState(store, {
										error: error.message || 'Failed to update config',
										isUpdatingConfig: false
									});
									return of(null);
								})
							)
					)
				)
			),

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


import { computed } from '@angular/core';
import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import * as signalR from '@microsoft/signalr';
import { SIGNALR_HUBS, SIGNALR_METHODS } from '../constants/api-routes.constants';

/**
 * Pub/Sub message interface
 */
export interface PubSubMessage {
	connectionId: string;
	channel: string;
	message: string;
	timestamp: string;
}

/**
 * Key change event interface
 */
export interface KeyChangeEvent {
	connectionId: string;
	key: string;
	type: string;
	action: 'created' | 'updated' | 'deleted';
	timestamp: string;
}

/**
 * SignalR state interface
 */
interface SignalRState {
	/**
	 * Pub/Sub messages
	 */
	pubSubMessages: PubSubMessage[];

	/**
	 * Key change events
	 */
	keyChangeEvents: KeyChangeEvent[];

	/**
	 * Pub/Sub hub connection status
	 */
	isPubSubConnected: boolean;

	/**
	 * Key change hub connection status
	 */
	isKeyChangeConnected: boolean;

	/**
	 * Active subscription IDs
	 */
	activeSubscriptions: string[];

	/**
	 * Watched connection IDs for key changes
	 */
	watchedConnections: string[];

	/**
	 * Error message
	 */
	error: string | null;
}

/**
 * Initial state
 */
const initialState: SignalRState = {
	pubSubMessages: [],
	keyChangeEvents: [],
	isPubSubConnected: false,
	isKeyChangeConnected: false,
	activeSubscriptions: [],
	watchedConnections: [],
	error: null
};

/**
 * Global SignalR store using NgRx SignalStore
 * Manages real-time SignalR connections for Pub/Sub and Key Change events
 *
 * Features:
 * - Pub/Sub message handling
 * - Key change notifications
 * - Connection state management
 * - Automatic reconnection
 *
 * @example
 * ```typescript
 * // Inject the store
 * private signalRStore = inject(SignalRStore);
 *
 * // Connect to hubs
 * this.signalRStore.connectPubSub();
 * this.signalRStore.connectKeyChange('conn-123');
 *
 * // Join a subscription
 * this.signalRStore.joinSubscription('sub-123');
 *
 * // Read messages
 * const messages = this.signalRStore.pubSubMessages();
 * const events = this.signalRStore.keyChangeEvents();
 *
 * // Disconnect
 * this.signalRStore.disconnectAll();
 * ```
 */
export const SignalRStore = signalStore(
	{ providedIn: 'root' },

	// Define state
	withState(initialState),

	// Define computed signals
	withComputed((store) => ({
		/**
		 * Total number of Pub/Sub messages
		 */
		pubSubMessageCount: computed(() => store.pubSubMessages().length),

		/**
		 * Total number of key change events
		 */
		keyChangeEventCount: computed(() => store.keyChangeEvents().length),

		/**
		 * Latest Pub/Sub message
		 */
		latestPubSubMessage: computed(() => {
			const messages = store.pubSubMessages();
			return messages.length > 0 ? messages[messages.length - 1] : null;
		}),

		/**
		 * Latest key change event
		 */
		latestKeyChangeEvent: computed(() => {
			const events = store.keyChangeEvents();
			return events.length > 0 ? events[events.length - 1] : null;
		}),

		/**
		 * Whether any hub is connected
		 */
		isAnyHubConnected: computed(() => {
			return store.isPubSubConnected() || store.isKeyChangeConnected();
		}),

		/**
		 * Number of active subscriptions
		 */
		subscriptionCount: computed(() => store.activeSubscriptions().length),

		/**
		 * Number of watched connections
		 */
		watchedConnectionCount: computed(() => store.watchedConnections().length)
	})),

	// Define methods
	withMethods((store) => {
		// Hub connections
		let pubSubConnection: signalR.HubConnection | undefined;
		let keyChangeConnection: signalR.HubConnection | undefined;

		/**
		 * Create SignalR connection with reconnection strategy
		 */
		const createConnection = (hubUrl: string, queryParams?: Record<string, string>): signalR.HubConnection => {
			let url = hubUrl;
			if (queryParams) {
				const params = new URLSearchParams(queryParams);
				url = `${hubUrl}?${params.toString()}`;
			}

			return new signalR.HubConnectionBuilder()
				.withUrl(url)
				.withAutomaticReconnect({
					nextRetryDelayInMilliseconds: (retryContext) => {
						if (retryContext.previousRetryCount === 0) return 0;
						if (retryContext.previousRetryCount < 3)
							return retryContext.previousRetryCount * 2000;
						if (retryContext.previousRetryCount < 5) return 10000;
						return 30000;
					}
				})
				.configureLogging(signalR.LogLevel.Warning)
				.build();
		};

		return {
			/**
			 * Connect to Pub/Sub hub
			 */
			connectPubSub(): void {
				if (pubSubConnection) return;

				pubSubConnection = createConnection(SIGNALR_HUBS.PUBSUB);

				// Register event handler
				pubSubConnection.on(SIGNALR_METHODS.PUBSUB.RECEIVE_MESSAGE, (msg: PubSubMessage) => {
					const messages = [...store.pubSubMessages(), msg];
					patchState(store, { pubSubMessages: messages });
				});

				// Setup lifecycle handlers
				pubSubConnection.onreconnecting(() => {
					patchState(store, { isPubSubConnected: false });
				});

				pubSubConnection.onreconnected(() => {
					patchState(store, { isPubSubConnected: true });
				});

				pubSubConnection.onclose(() => {
					patchState(store, { isPubSubConnected: false });
				});

				// Start connection
				pubSubConnection
					.start()
					.then(() => {
						patchState(store, {
							isPubSubConnected: true,
							error: null
						});
					})
					.catch((err) => {
						patchState(store, {
							isPubSubConnected: false,
							error: err.message || 'Failed to connect to PubSub hub'
						});
					});
			},

			/**
			 * Connect to Key Change notification hub
			 * @param connectionId Optional Redis connection ID to watch immediately
			 */
			connectKeyChange(connectionId?: string): void {
				if (keyChangeConnection) return;

				const queryParams = connectionId ? { connectionId } : undefined;
				keyChangeConnection = createConnection(SIGNALR_HUBS.KEY_CHANGE, queryParams);

				// Register event handler
				keyChangeConnection.on(SIGNALR_METHODS.KEY_CHANGE.KEY_CHANGED, (evt: KeyChangeEvent) => {
					const events = [...store.keyChangeEvents(), evt];
					patchState(store, { keyChangeEvents: events });
				});

				// Setup lifecycle handlers
				keyChangeConnection.onreconnecting(() => {
					patchState(store, { isKeyChangeConnected: false });
				});

				keyChangeConnection.onreconnected(() => {
					patchState(store, { isKeyChangeConnected: true });
				});

				keyChangeConnection.onclose(() => {
					patchState(store, { isKeyChangeConnected: false });
				});

				// Start connection
				keyChangeConnection
					.start()
					.then(() => {
						patchState(store, {
							isKeyChangeConnected: true,
							error: null
						});
						if (connectionId) {
							patchState(store, {
								watchedConnections: [...store.watchedConnections(), connectionId]
							});
						}
					})
					.catch((err) => {
						patchState(store, {
							isKeyChangeConnected: false,
							error: err.message || 'Failed to connect to key change hub'
						});
					});
			},

			/**
			 * Join a Pub/Sub subscription group
			 * @param subscriptionId The subscription ID to join
			 */
			async joinSubscription(subscriptionId: string): Promise<void> {
				if (!pubSubConnection || pubSubConnection.state !== signalR.HubConnectionState.Connected) {
					return;
				}

				try {
					await pubSubConnection.invoke(SIGNALR_METHODS.PUBSUB.JOIN_SUBSCRIPTION, subscriptionId);
					patchState(store, {
						activeSubscriptions: [...store.activeSubscriptions(), subscriptionId]
					});
				} catch (err) {
					patchState(store, {
						error: (err as Error).message || 'Failed to join subscription'
					});
				}
			},

			/**
			 * Leave a Pub/Sub subscription group
			 * @param subscriptionId The subscription ID to leave
			 */
			async leaveSubscription(subscriptionId: string): Promise<void> {
				if (!pubSubConnection || pubSubConnection.state !== signalR.HubConnectionState.Connected) {
					return;
				}

				try {
					await pubSubConnection.invoke(SIGNALR_METHODS.PUBSUB.LEAVE_SUBSCRIPTION, subscriptionId);
					patchState(store, {
						activeSubscriptions: store.activeSubscriptions().filter((id) => id !== subscriptionId)
					});
				} catch (err) {
					patchState(store, {
						error: (err as Error).message || 'Failed to leave subscription'
					});
				}
			},

			/**
			 * Watch a Redis connection for key changes
			 * @param redisConnectionId The Redis connection ID to watch
			 */
			async watchConnection(redisConnectionId: string): Promise<void> {
				if (!keyChangeConnection || keyChangeConnection.state !== signalR.HubConnectionState.Connected) {
					return;
				}

				try {
					await keyChangeConnection.invoke(SIGNALR_METHODS.KEY_CHANGE.WATCH_CONNECTION, redisConnectionId);
					patchState(store, {
						watchedConnections: [...store.watchedConnections(), redisConnectionId]
					});
				} catch (err) {
					patchState(store, {
						error: (err as Error).message || 'Failed to watch connection'
					});
				}
			},

			/**
			 * Stop watching a Redis connection for key changes
			 * @param redisConnectionId The Redis connection ID to unwatch
			 */
			async unwatchConnection(redisConnectionId: string): Promise<void> {
				if (!keyChangeConnection || keyChangeConnection.state !== signalR.HubConnectionState.Connected) {
					return;
				}

				try {
					await keyChangeConnection.invoke(SIGNALR_METHODS.KEY_CHANGE.UNWATCH_CONNECTION, redisConnectionId);
					patchState(store, {
						watchedConnections: store.watchedConnections().filter((id) => id !== redisConnectionId)
					});
				} catch (err) {
					patchState(store, {
						error: (err as Error).message || 'Failed to unwatch connection'
					});
				}
			},

			/**
			 * Disconnect from Pub/Sub hub
			 */
			disconnectPubSub(): void {
				if (!pubSubConnection) return;

				pubSubConnection
					.stop()
					.then(() => {
						pubSubConnection = undefined;
						patchState(store, {
							isPubSubConnected: false,
							activeSubscriptions: []
						});
					})
					.catch((err) => {
						patchState(store, {
							error: err.message || 'Failed to disconnect from PubSub hub'
						});
					});
			},

			/**
			 * Disconnect from Key Change hub
			 */
			disconnectKeyChange(): void {
				if (!keyChangeConnection) return;

				keyChangeConnection
					.stop()
					.then(() => {
						keyChangeConnection = undefined;
						patchState(store, {
							isKeyChangeConnected: false,
							watchedConnections: []
						});
					})
					.catch((err) => {
						patchState(store, {
							error: err.message || 'Failed to disconnect from key change hub'
						});
					});
			},

			/**
			 * Disconnect from all hubs
			 */
			disconnectAll(): void {
				if (pubSubConnection) {
					pubSubConnection.stop().catch(() => {
						// Silent fail
					});
					pubSubConnection = undefined;
				}

				if (keyChangeConnection) {
					keyChangeConnection.stop().catch(() => {
						// Silent fail
					});
					keyChangeConnection = undefined;
				}

				patchState(store, {
					isPubSubConnected: false,
					isKeyChangeConnected: false,
					activeSubscriptions: [],
					watchedConnections: []
				});
			},

			/**
			 * Clear all Pub/Sub messages
			 */
			clearPubSubMessages(): void {
				patchState(store, { pubSubMessages: [] });
			},

			/**
			 * Clear all key change events
			 */
			clearKeyChangeEvents(): void {
				patchState(store, { keyChangeEvents: [] });
			},

			/**
			 * Clear all messages and events
			 */
			clearAll(): void {
				patchState(store, {
					pubSubMessages: [],
					keyChangeEvents: []
				});
			},

			/**
			 * Clear error state
			 */
			clearError(): void {
				patchState(store, { error: null });
			},

			/**
			 * Reset store to initial state (disconnects all hubs)
			 */
			reset(): void {
				if (pubSubConnection) {
					pubSubConnection.stop().catch(() => {
						// Silent fail
					});
					pubSubConnection = undefined;
				}
				if (keyChangeConnection) {
					keyChangeConnection.stop().catch(() => {
						// Silent fail
					});
					keyChangeConnection = undefined;
				}

				patchState(store, initialState);
			}
		};
	})
);

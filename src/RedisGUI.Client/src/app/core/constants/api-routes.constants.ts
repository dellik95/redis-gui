/**
 * API Route Constants
 *
 * Centralized definition of all API routes to ensure consistency
 * between client and backend endpoints.
 *
 * All routes are prefixed with /api/v1 as defined in Program.cs
 */

export const API_ROUTES = {
  /**
   * API base path
   */
  BASE: '/api/v1',

  /**
   * Authentication endpoints
   */
  AUTH: {
    BASE: '/api/v1/auth',
    REGISTER: '/api/v1/auth/register',
    LOGIN: '/api/v1/auth/login'
  },

  /**
   * Connection management endpoints
   */
  CONNECTIONS: {
    BASE: '/api/v1/connections',
    LIST: '/api/v1/connections',
    GET: (id: string) => `/api/v1/connections/${id}`,
    CREATE: '/api/v1/connections',
    UPDATE: (id: string) => `/api/v1/connections/${id}`,
    DELETE: (id: string) => `/api/v1/connections/${id}`,
    CHECK: '/api/v1/connections/check',
    HEALTH: (connectionId: string) => `/api/v1/connections/${connectionId}/health`,
    METRICS: (connectionId: string) => `/api/v1/connections/${connectionId}/metrics`,
    INFO: (connectionId: string) => `/api/v1/connections/${connectionId}/info`,
    CONFIG: (connectionId: string) => `/api/v1/connections/${connectionId}/config`
  },

  /**
   * Redis storage endpoints
   */
  STORAGE: {
    BASE: '/api/v1/storage',
    GET_ALL: (connectionId: string, pattern?: string, pageSize?: number, pageNumber?: number) => {
      const params = new URLSearchParams();
      if (pattern) params.set('pattern', pattern);
      if (pageSize) params.set('pageSize', pageSize.toString());
      if (pageNumber) params.set('pageNumber', pageNumber.toString());
      const query = params.toString();
      return `/api/v1/storage/${connectionId}${query ? `?${query}` : ''}`;
    },
    GET_VALUE: (connectionId: string, key: string) =>
      `/api/v1/storage/${connectionId}/${encodeURIComponent(key)}`,
    ADD_VALUE: (connectionId: string) => `/api/v1/storage/${connectionId}`,
    UPDATE_VALUE: (connectionId: string, key: string) =>
      `/api/v1/storage/${connectionId}/keys/${encodeURIComponent(key)}`,
    DELETE_VALUE: (connectionId: string, key: string) =>
      `/api/v1/storage/${connectionId}/${encodeURIComponent(key)}`,
    DELETE_MULTIPLE: (connectionId: string) =>
      `/api/v1/storage/${connectionId}/keys/delete-multiple`,
    DELETE_BY_PATTERN: (connectionId: string, pattern: string) =>
      `/api/v1/storage/${connectionId}/keys/pattern/${encodeURIComponent(pattern)}`,
    KEYS: (connectionId: string) => `/api/v1/storage/${connectionId}/keys`,
    KEY: (connectionId: string, key: string) =>
      `/api/v1/storage/${connectionId}/keys/${encodeURIComponent(key)}`,
    KEYSPACE: (connectionId: string) => `/api/v1/storage/${connectionId}/keyspace`,
    SLOWLOG: (id: string, count: number) => `/api/v1/storage/${id}/slowlog?count=${count}`,
    SEED: (connectionId: string) => `/api/v1/storage/${connectionId}/seed`,
    RENAME_KEY: (connectionId: string) => `/api/v1/storage/${connectionId}/keys/rename`,
    DUPLICATE_KEY: (connectionId: string) => `/api/v1/storage/${connectionId}/keys/duplicate`,
    BULK_TTL: (connectionId: string) => `/api/v1/storage/${connectionId}/keys/bulk-ttl`,
    IMPORT: (connectionId: string) => `/api/v1/storage/${connectionId}/import`,
    EXPORT: (connectionId: string, pattern?: string, format?: string) => {
      const params = new URLSearchParams();
      if (pattern) params.set('pattern', pattern);
      if (format) params.set('format', format);
      const query = params.toString();
      return `/api/v1/storage/${connectionId}/export${query ? `?${query}` : ''}`;
    }
  },

  /**
   * Redis operations endpoints
   */
  REDIS: {
    BASE: '/api/v1',
    EXECUTE_TRANSACTION: (connectionId: string) => `/api/v1/transaction/${connectionId}/execute`,
    EXECUTE_LUA_SCRIPT: (connectionId: string) => `/api/v1/script/${connectionId}/execute`,
    PUBSUB_SUBSCRIBE: (connectionId: string, channel: string) =>
      `/api/v1/pubsub/${connectionId}/subscribe/${encodeURIComponent(channel)}`,
    PUBSUB_UNSUBSCRIBE: (connectionId: string, subscriptionId: string) =>
      `/api/v1/pubsub/${connectionId}/subscribe/${subscriptionId}`,
    PUBSUB_PUBLISH: (connectionId: string) => `/api/v1/pubsub/${connectionId}/publish`
  },

  /**
   * Audit endpoints
   */
  AUDIT: {
    BASE: '/api/v1/audit',
    GET_LOGS: (
      userId?: string,
      action?: string,
      startDate?: string,
      endDate?: string,
      pageNumber?: number,
      pageSize?: number
    ) => {
      const params = new URLSearchParams();
      if (userId) params.set('userId', userId);
      if (action) params.set('action', action);
      if (startDate) params.set('startDate', startDate);
      if (endDate) params.set('endDate', endDate);
      if (pageNumber) params.set('pageNumber', pageNumber.toString());
      if (pageSize) params.set('pageSize', pageSize.toString());
      const query = params.toString();
      return `/api/v1/audit${query ? `?${query}` : ''}`;
    }
  }
} as const;

/**
 * SignalR Hub Routes
 *
 * Centralized definition of all SignalR hub routes
 */
export const SIGNALR_HUBS = {
  KEY_CHANGE: '/hub/keychange',

  /**
   * Metrics hub for Redis metrics broadcasting
   */
  METRICS: '/hub/metrics'
} as const;

/**
 * SignalR Hub Method Names
 *
 * Centralized definition of all SignalR hub method names
 * These must match the backend interface method names exactly
 */
export const SIGNALR_METHODS = {
  /**
   * Pub/Sub hub methods
   */

  /**
   * Key Change hub methods
   */
  KEY_CHANGE: {
    /**
     * Server-to-client: Receive a key change notification
     * Matches: IKeyChangeClientMethods.KeyChanged
     */
    KEY_CHANGED: 'KeyChanged',

    /**
     * Client-to-server: Subscribe to key changes for a specific Redis connection
     * Matches: KeyChangeHub.WatchConnection
     */
    WATCH_CONNECTION: 'WatchConnection',

    /**
     * Client-to-server: Unsubscribe from key changes for a specific Redis connection
     * Matches: KeyChangeHub.UnwatchConnection
     */
    UNWATCH_CONNECTION: 'UnwatchConnection'
  },

  /**
   * Metrics hub methods
   */
  METRICS: {
    /**
     * Server-to-client: Receive updated Redis metrics
     * Matches: IClientMethods.NotifyUserAsync
     */
    NOTIFY_USER: 'NotifyUserAsync'
  }
} as const;

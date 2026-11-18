import { computed, inject } from '@angular/core';
import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import {
  Subject,
  catchError,
  concatMap,
  EMPTY,
  expand,
  exhaustMap,
  merge,
  of,
  pipe,
  switchMap,
  takeUntil,
  tap
} from 'rxjs';
import { HttpClient, HttpParams } from '@angular/common/http';
import {
  RedisDataType,
  RedisValueData,
  RedisKeyInfo,
  GetRedisValueResponse,
  REDIS_TTL,
  toRedisDataType,
  RedisKeyMutationResponse
} from '../../models/redis-keys.types';
import { API_ROUTES } from '../constants/api-routes.constants';

export const MAX_RETAINED_KEYS = 10_000;
const MAX_EMPTY_CONTINUATION_HOPS = 5;

export interface KeyScanPage {
  keys: {
    key: string;
    type: string;
    ttlSeconds: number | null;
    keyBase64?: string | null | undefined;
    isBinary?: boolean | undefined;
  }[];
  nextCursor: string | null;
  serverMode?: string | undefined;
}

export interface LoadKeysRequest {
  connectionId: string;
  pattern?: string | undefined;
  database?: number | undefined;
  append?: boolean | undefined;
  autoContinueEmptyPages?: boolean | undefined;
}

export interface RedisKeyValue {
  key: string;
  keyBase64?: string | null | undefined;
  isBinary?: boolean | undefined;
  value: unknown;
  type: RedisDataType;
  ttl?: number;
}

interface RedisKeysState {
  keys: RedisKeyInfo[];
  selectedKey: RedisKeyValue | null;
  isLoading: boolean;
  error: string | null;
  lastFetchParams: {
    connectionId: string;
    pattern: string;
    database?: number;
  } | null;
  activeDatabase: number;
  keyspace: Record<number, number>;
  lastFetched: number | null;
  nextCursor: string | null;
  serverMode: string;
  retentionLimitReached: boolean;
  scanEpoch: number;
}

const initialState: RedisKeysState = {
  keys: [],
  selectedKey: null,
  isLoading: false,
  error: null,
  lastFetchParams: null,
  activeDatabase: 0,
  keyspace: {},
  lastFetched: null,
  nextCursor: null,
  serverMode: 'Unknown',
  retentionLimitReached: false,
  scanEpoch: 0
};

export function redisKeyIdentity(item: { key: string; keyBase64?: string | null | undefined }): string {
  return item.keyBase64 ? `b64:${item.keyBase64}` : `txt:${item.key}`;
}

export function requiresQueryKeyRoute(key: string, keyBase64?: string | null): boolean {
  return !!keyBase64 && key.length >= 0;
}

export const RedisKeysStore = signalStore(
  { providedIn: 'root' },

  withState(initialState),

  withComputed(store => ({
    needsRefresh: computed(() => {
      const lastFetched = store.lastFetched();
      if (!lastFetched) return true;
      return Date.now() - lastFetched > 5000;
    }),

    totalKeys: computed(() => store.keys().length),

    hasKeys: computed(() => store.keys().length > 0),

    isClusterMode: computed(() => store.serverMode() === 'Cluster')
  })),

  withMethods((store, httpClient = inject(HttpClient)) => {
    const resetScan$ = new Subject<void>();
    const sessionEnded$ = new Subject<void>();

    const fetchScanPage = (
      connectionId: string,
      pattern: string,
      database: number,
      cursor: string | null
    ) => {
      let params = new HttpParams()
        .set('pattern', pattern)
        .set('database', database)
        .set('count', 100);
      if (cursor) {
        params = params.set('cursor', cursor);
      }
      return httpClient.get<KeyScanPage>(API_ROUTES.STORAGE.SCAN(connectionId), { params });
    };

    const executeScan = (
      req: LoadKeysRequest,
      epoch: number,
      append: boolean
    ) => {
      const pattern = req.pattern ?? '*';
      const targetDb = req.database ?? store.activeDatabase();
      const initialCursor = append ? store.nextCursor() : null;

      if (append && (!initialCursor || store.keys().length >= MAX_RETAINED_KEYS)) {
        if (append && store.keys().length >= MAX_RETAINED_KEYS) {
          patchState(store, { retentionLimitReached: true, isLoading: false });
        }
        return EMPTY;
      }

      patchState(store, {
        isLoading: true,
        error: null,
        ...(!append
          ? { keys: [], nextCursor: null, retentionLimitReached: false }
          : {})
      });

      const autoContinue = req.autoContinueEmptyPages === true;
      let hops = 0;

      const cancelSignal$ = merge(resetScan$, sessionEnded$);

      const stream$ = fetchScanPage(req.connectionId, pattern, targetDb, initialCursor).pipe(
        expand(response => {
          if (
            !autoContinue ||
            response.keys.length > 0 ||
            !response.nextCursor ||
            hops >= MAX_EMPTY_CONTINUATION_HOPS
          ) {
            return EMPTY;
          }
          hops++;
          return fetchScanPage(req.connectionId, pattern, targetDb, response.nextCursor);
        }),
        takeUntil(cancelSignal$),
        tap(response => {
          if (store.scanEpoch() !== epoch) return;

          const fetchedAt = Date.now();
          const incoming: RedisKeyInfo[] = response.keys.map(item => ({
            key: item.key,
            keyBase64: item.keyBase64 ?? null,
            isBinary: item.isBinary ?? false,
            type: item.type,
            ttl: item.ttlSeconds === null ? null : Math.ceil(item.ttlSeconds),
            size: 0,
            memoryUsage: null,
            fetchedAt
          }));

          const existing = store.keys();
          const merged = new Map<string, RedisKeyInfo>(
            existing.map(k => [redisKeyIdentity(k), k])
          );
          let limitReached = existing.length >= MAX_RETAINED_KEYS;

          for (const item of incoming) {
            const id = redisKeyIdentity(item);
            if (!merged.has(id) && merged.size >= MAX_RETAINED_KEYS) {
              limitReached = true;
              break;
            }
            merged.set(id, item);
          }

          const hasMore =
            autoContinue && response.keys.length === 0 && response.nextCursor !== null && hops < MAX_EMPTY_CONTINUATION_HOPS;

          patchState(store, {
            keys: [...merged.values()],
            nextCursor: limitReached ? response.nextCursor : response.nextCursor,
            isLoading: hasMore,
            activeDatabase: targetDb,
            serverMode: response.serverMode ?? store.serverMode(),
            retentionLimitReached: limitReached,
            lastFetchParams: {
              connectionId: req.connectionId,
              pattern,
              database: targetDb
            },
            lastFetched: fetchedAt
          });
        }),
        catchError(error => {
          if (store.scanEpoch() === epoch) {
            patchState(store, {
              error: error?.error?.detail || error?.error?.message || error?.message || 'Failed to load keys',
              isLoading: false
            });
          }
          return of(null);
        })
      );

      return stream$;
    };

    const resetQueue = rxMethod<LoadKeysRequest>(
      pipe(
        switchMap(req => {
          resetScan$.next();
          const nextEpoch = store.scanEpoch() + 1;
          patchState(store, { scanEpoch: nextEpoch });
          return executeScan(req, nextEpoch, false);
        })
      )
    );

    const appendQueue = rxMethod<LoadKeysRequest>(
      pipe(
        exhaustMap(req => executeScan(req, store.scanEpoch(), true))
      )
    );

    return {
      setActiveDatabase(db: number) {
        patchState(store, { activeDatabase: db, lastFetched: null });
      },

      syncKeyspace(databases: Record<number, number>) {
        patchState(store, { keyspace: databases });
      },

      loadKeyspace: rxMethod<{ connectionId: string }>(
        pipe(
          switchMap(({ connectionId }) =>
            httpClient
              .get<{ databases: Record<number, number> }>(API_ROUTES.STORAGE.KEYSPACE(connectionId))
              .pipe(
                takeUntil(sessionEnded$),
                tap(res => {
                  if (res?.databases) {
                    patchState(store, { keyspace: res.databases });
                  }
                }),
                catchError(() => of(null))
              )
          )
        )
      ),

      loadKeys(req: LoadKeysRequest): void {
        if (req.append) {
          if (store.isLoading() || !store.nextCursor()) {
            return;
          }
          appendQueue(req);
        } else {
          resetQueue(req);
        }
      },

      loadValue: rxMethod<{
        connectionId: string;
        key: string;
        keyBase64?: string | null | undefined;
        database?: number;
      }>(
        pipe(
          tap(() => patchState(store, { isLoading: true, error: null })),
          switchMap(({ connectionId, key, keyBase64, database }) => {
            const targetDb = database ?? store.activeDatabase();
            const useQuery = requiresQueryKeyRoute(key, keyBase64);
            const url = useQuery
              ? API_ROUTES.STORAGE.KEY_BY_QUERY(connectionId, key, keyBase64, targetDb)
              : API_ROUTES.STORAGE.GET_VALUE(connectionId, key);

            let params = new HttpParams();
            if (!useQuery && targetDb !== undefined && targetDb !== null) {
              params = params.set('database', targetDb.toString());
            }

            return httpClient
              .get<GetRedisValueResponse>(url, useQuery ? {} : { params })
              .pipe(
                takeUntil(sessionEnded$),
                tap(response => {
                  let ttlVal = response.ttl ?? REDIS_TTL.Persistent;
                  if (response.timeToLive && ttlVal === REDIS_TTL.Persistent) {
                    const parts = response.timeToLive.split(':');
                    if (parts.length === 3) {
                      ttlVal = Math.round(
                        parseFloat(parts[0] ?? '0') * 3600 +
                          parseFloat(parts[1] ?? '0') * 60 +
                          parseFloat(parts[2] ?? '0')
                      );
                    }
                  }

                  const value: RedisKeyValue = {
                    key: response.key,
                    keyBase64: response.keyBase64 ?? keyBase64 ?? null,
                    isBinary: response.isBinary ?? false,
                    value: response.value,
                    type: toRedisDataType(response.type),
                    ttl: ttlVal
                  };
                  patchState(store, {
                    selectedKey: value,
                    isLoading: false
                  });
                }),
                catchError(error => {
                  patchState(store, {
                    error: error.message || 'Failed to load value',
                    isLoading: false
                  });
                  return of(null);
                })
              );
          })
        )
      ),

      updateValue: rxMethod<{
        connectionId: string;
        key: string;
        keyBase64?: string | null | undefined;
        type: RedisDataType;
        value: RedisValueData;
        ttl?: number;
        database?: number;
        onSuccess?: () => void;
        onError?: () => void;
      }>(
        pipe(
          tap(() => patchState(store, { isLoading: true, error: null })),
          concatMap(({ connectionId, key, keyBase64, type, value, ttl, database, onSuccess, onError }) => {
            const targetDb = database ?? store.activeDatabase();
            const useQuery = requiresQueryKeyRoute(key, keyBase64);
            const url = useQuery
              ? API_ROUTES.STORAGE.KEY_BY_QUERY(connectionId, key, keyBase64, targetDb)
              : API_ROUTES.STORAGE.UPDATE_VALUE(connectionId, key);

            const requestBody = {
              connectionId,
              key,
              ...(keyBase64 ? { keyBase64 } : {}),
              type,
              value,
              ttl,
              database: targetDb
            };

            let params = new HttpParams();
            if (!useQuery && targetDb !== undefined && targetDb !== null) {
              params = params.set('database', targetDb.toString());
            }

            return httpClient
              .put<void>(url, requestBody, useQuery ? {} : { params })
              .pipe(
                takeUntil(sessionEnded$),
                tap(() => {
                  patchState(store, {
                    isLoading: false,
                    lastFetched: null
                  });
                  onSuccess?.();
                }),
                catchError(error => {
                  onError?.();
                  patchState(store, {
                    error: error.message || 'Failed to update value',
                    isLoading: false
                  });
                  return of(null);
                })
              );
          })
        )
      ),

      addValue: rxMethod<{
        connectionId: string;
        key: string;
        keyBase64?: string | null | undefined;
        type?: RedisDataType;
        value: RedisValueData | string;
        ttl?: number;
        database?: number;
        onSuccess?: () => void;
      }>(
        pipe(
          tap(() => patchState(store, { isLoading: true, error: null })),
          switchMap(
            ({
              connectionId,
              key,
              keyBase64,
              type = RedisDataType.String,
              value,
              ttl,
              database,
              onSuccess
            }) => {
              const targetDb = database ?? store.activeDatabase();

              const valObj =
                typeof value === 'object' && value !== null
                  ? value
                  : { value: String(value ?? '') };

              let params = new HttpParams();
              if (targetDb !== undefined && targetDb !== null) {
                params = params.set('database', targetDb.toString());
              }

              return httpClient
                .post<void>(
                  API_ROUTES.STORAGE.KEYS(connectionId),
                  {
                    connectionId,
                    key,
                    ...(keyBase64 ? { keyBase64 } : {}),
                    type,
                    value: valObj,
                    ttl,
                    database: targetDb
                  },
                  { params }
                )
                .pipe(
                  takeUntil(sessionEnded$),
                  tap(() => {
                    patchState(store, {
                      isLoading: false,
                      lastFetched: null
                    });
                    onSuccess?.();
                  }),
                  catchError(error => {
                    patchState(store, {
                      error: error.message || 'Failed to add value',
                      isLoading: false
                    });
                    return of(null);
                  })
                );
            }
          )
        )
      ),

      deleteKey: rxMethod<{
        connectionId: string;
        key: string;
        keyBase64?: string | null | undefined;
        database?: number;
        onSuccess?: () => void;
      }>(
        pipe(
          tap(({ key, keyBase64 }) => {
            const targetId = redisKeyIdentity({ key, keyBase64 });
            const selected = store.selectedKey();
            const selectedMatches = selected ? redisKeyIdentity(selected) === targetId : false;
            patchState(store, {
              keys: store.keys().filter(k => redisKeyIdentity(k) !== targetId),
              selectedKey: selectedMatches ? null : selected,
              isLoading: true,
              error: null,
              lastFetched: null,
              lastFetchParams: null
            });
          }),
          switchMap(({ connectionId, key, keyBase64, database, onSuccess }) => {
            const targetDb = database ?? store.activeDatabase();
            const useQuery = requiresQueryKeyRoute(key, keyBase64);
            const url = useQuery
              ? API_ROUTES.STORAGE.KEY_BY_QUERY(connectionId, key, keyBase64, targetDb)
              : API_ROUTES.STORAGE.KEY(connectionId, key);

            let params = new HttpParams();
            if (!useQuery && targetDb !== undefined && targetDb !== null) {
              params = params.set('database', targetDb.toString());
            }

            return httpClient
              .delete<boolean>(url, useQuery ? {} : { params })
              .pipe(
                takeUntil(sessionEnded$),
                tap(() => {
                  patchState(store, {
                    isLoading: false,
                    lastFetched: null,
                    lastFetchParams: null
                  });
                  onSuccess?.();
                }),
                catchError(error => {
                  patchState(store, {
                    error: error.message || 'Failed to delete key',
                    isLoading: false
                  });
                  return of(null);
                })
              );
          })
        )
      ),

      renameKey: rxMethod<{
        connectionId: string;
        oldKey: string;
        oldKeyBase64?: string | null | undefined;
        newKey: string;
        newKeyBase64?: string | null | undefined;
        overwrite?: boolean;
        database?: number;
        onSuccess?: () => void;
      }>(
        pipe(
          tap(() => patchState(store, { isLoading: true, error: null })),
          switchMap(({ connectionId, oldKey, oldKeyBase64, newKey, newKeyBase64, overwrite = false, database, onSuccess }) =>
            httpClient
              .post<RedisKeyMutationResponse>(API_ROUTES.STORAGE.RENAME_KEY(connectionId), {
                oldKey,
                ...(oldKeyBase64 ? { oldKeyBase64 } : {}),
                newKey,
                ...(newKeyBase64 ? { newKeyBase64 } : {}),
                overwrite,
                database
              })
              .pipe(
                takeUntil(sessionEnded$),
                tap(() => {
                  patchState(store, { isLoading: false, lastFetched: null });
                  onSuccess?.();
                }),
                catchError(error => {
                  patchState(store, {
                    error:
                      error.error?.detail ||
                      error.error?.message ||
                      error.message ||
                      'Failed to rename key',
                    isLoading: false
                  });
                  return of(null);
                })
              )
          )
        )
      ),

      duplicateKey: rxMethod<{
        connectionId: string;
        sourceKey: string;
        sourceKeyBase64?: string | null | undefined;
        targetKey: string;
        targetKeyBase64?: string | null | undefined;
        database?: number;
        onSuccess?: () => void;
      }>(
        pipe(
          tap(() => patchState(store, { isLoading: true, error: null })),
          switchMap(({ connectionId, sourceKey, sourceKeyBase64, targetKey, targetKeyBase64, database, onSuccess }) =>
            httpClient
              .post<RedisKeyMutationResponse>(API_ROUTES.STORAGE.DUPLICATE_KEY(connectionId), {
                sourceKey,
                ...(sourceKeyBase64 ? { sourceKeyBase64 } : {}),
                targetKey,
                ...(targetKeyBase64 ? { targetKeyBase64 } : {}),
                database
              })
              .pipe(
                takeUntil(sessionEnded$),
                tap(() => {
                  patchState(store, { isLoading: false, lastFetched: null });
                  onSuccess?.();
                }),
                catchError(error => {
                  patchState(store, {
                    error:
                      error.error?.detail ||
                      error.error?.message ||
                      error.message ||
                      'Failed to duplicate key',
                    isLoading: false
                  });
                  return of(null);
                })
              )
          )
        )
      ),

      selectKey(key: RedisKeyValue | null): void {
        patchState(store, { selectedKey: key });
      },

      clearError(): void {
        patchState(store, { error: null });
      },

      clearCache(): void {
        patchState(store, {
          lastFetched: null,
          lastFetchParams: null
        });
      },

      reset(): void {
        resetScan$.next();
        patchState(store, initialState);
      },

      resetSession(): void {
        resetScan$.next();
        sessionEnded$.next();
        patchState(store, initialState);
      }
    };
  })
);

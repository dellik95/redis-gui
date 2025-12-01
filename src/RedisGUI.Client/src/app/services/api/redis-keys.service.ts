import { inject, Injectable } from "@angular/core";
import { HttpClient, HttpParams } from "@angular/common/http";
import { Observable, throwError, shareReplay } from "rxjs";
import { 
    GetGroupedRedisKeysResponse, 
    RedisDataType, 
    RedisValueData, 
    UpdateRedisValueRequest 
} from "../../models/redis-keys.types";

@Injectable({
    providedIn: "root"
})
export class RedisKeysService {
    private readonly httpClient = inject(HttpClient);
    private readonly BASE_URL = '/api/v1/storage';
    
    // Cache for grouped keys with 5-second validity
    private readonly keysCache = new Map<string, Observable<GetGroupedRedisKeysResponse>>();
    private readonly CACHE_DURATION = 5000;

    /**
     * Get Redis keys grouped by delimiter with tree or flat view
     * @param connectionId - The connection ID
     * @param pattern - Key pattern (default: *)
     * @param delimiter - Key delimiter for grouping (default: :)
     * @param viewMode - View mode: tree or flat
     * @returns Observable of grouped Redis keys response
     */
    getGroupedKeys(
        connectionId: string, 
        pattern: string = '*', 
        delimiter: string = ':', 
        viewMode: 'tree' | 'flat' = 'tree'
    ): Observable<GetGroupedRedisKeysResponse> {
        if (!connectionId) {
            return throwError(() => new Error('Connection ID is required'));
        }

        const cacheKey = `${connectionId}-${pattern}-${delimiter}-${viewMode}`;
        
        // Check cache
        const cached = this.keysCache.get(cacheKey);
        if (cached) {
            return cached;
        }

        const params = new HttpParams()
            .set('pattern', pattern)
            .set('delimiter', delimiter)
            .set('viewMode', viewMode);

        const request$ = this.httpClient.get<GetGroupedRedisKeysResponse>(
            `${this.BASE_URL}/${connectionId}/grouped`,
            { params }
        ).pipe(
            shareReplay({ bufferSize: 1, refCount: true })
        );

        // Cache the observable
        this.keysCache.set(cacheKey, request$);
        
        // Clear cache after duration
        setTimeout(() => {
            this.keysCache.delete(cacheKey);
        }, this.CACHE_DURATION);

        return request$;
    }

    /**
     * Get a single Redis value by key
     * @param connectionId - The connection ID
     * @param key - The Redis key
     * @returns Observable of the key value
     */
    getValue(connectionId: string, key: string): Observable<any> {
        if (!connectionId || !key) {
            return throwError(() => new Error('Connection ID and key are required'));
        }

        return this.httpClient.get<any>(
            `${this.BASE_URL}/${connectionId}/keys/${encodeURIComponent(key)}`
        );
    }

    /**
     * Update a Redis value with type-specific data
     * @param connectionId - The connection ID
     * @param key - The Redis key
     * @param type - The Redis data type
     * @param value - The value data
     * @param ttl - Optional TTL in seconds
     * @returns Observable of void
     */
    updateValue(
        connectionId: string,
        key: string,
        type: RedisDataType,
        value: RedisValueData,
        ttl?: number
    ): Observable<void> {
        if (!connectionId || !key) {
            return throwError(() => new Error('Connection ID and key are required'));
        }

        const request: UpdateRedisValueRequest = {
            connectionId,
            key,
            type,
            value,
            ttl
        };

        return this.httpClient.put<void>(
            `${this.BASE_URL}/${connectionId}/keys/${encodeURIComponent(key)}`,
            request
        );
    }

    /**
     * Add a new Redis key-value pair
     * @param connectionId - The connection ID
     * @param key - The Redis key
     * @param value - The value as string
     * @param ttl - Optional TTL in seconds
     * @returns Observable of void
     */
    addValue(
        connectionId: string,
        key: string,
        value: string,
        ttl?: number
    ): Observable<void> {
        if (!connectionId || !key) {
            return throwError(() => new Error('Connection ID and key are required'));
        }

        return this.httpClient.post<void>(
            `${this.BASE_URL}/${connectionId}/keys`,
            { key, value, ttl }
        );
    }

    /**
     * Delete a Redis key
     * @param connectionId - The connection ID
     * @param key - The Redis key to delete
     * @returns Observable of boolean indicating success
     */
    deleteKey(connectionId: string, key: string): Observable<boolean> {
        if (!connectionId || !key) {
            return throwError(() => new Error('Connection ID and key are required'));
        }

        return this.httpClient.delete<boolean>(
            `${this.BASE_URL}/${connectionId}/keys/${encodeURIComponent(key)}`
        );
    }

    /**
     * Clear the keys cache
     * Useful when data has been modified and cache needs to be invalidated
     */
    clearCache(): void {
        this.keysCache.clear();
    }
}

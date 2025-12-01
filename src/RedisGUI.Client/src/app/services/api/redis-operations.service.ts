import { inject, Injectable } from "@angular/core";
import { HttpClient, HttpParams } from "@angular/common/http";
import { Observable, throwError } from "rxjs";
import {
    PagedRedisValuesResponse,
    DeleteKeysByPatternResponse,
    DeleteMultipleKeysResponse,
    ExportRedisDataResponse,
    ImportRedisDataRequest,
    ImportRedisDataResponse,
    PublishMessageRequest,
    SubscribeToChannelResponse,
    ExecuteTransactionRequest,
    ExecuteTransactionResponse,
    ExecuteLuaScriptRequest,
    ExecuteLuaScriptResponse,
    GetConnectionHealthResponse
} from "../../models/redis-advanced.types";

@Injectable({
    providedIn: "root"
})
export class RedisOperationsService {
    private readonly httpClient = inject(HttpClient);

    // ========== Pagination ==========

    /**
     * Get paginated Redis values
     * @param connectionId - The connection ID
     * @param pattern - Key pattern (default: *)
     * @param pageNumber - Page number (starting from 1)
     * @param pageSize - Number of items per page
     * @returns Observable of paged values response
     */
    getPagedValues(
        connectionId: string,
        pattern: string = '*',
        pageNumber: number = 1,
        pageSize: number = 50
    ): Observable<PagedRedisValuesResponse> {
        if (!connectionId) {
            return throwError(() => new Error('Connection ID is required'));
        }

        const params = new HttpParams()
            .set('pattern', pattern)
            .set('pageNumber', pageNumber.toString())
            .set('pageSize', pageSize.toString());

        return this.httpClient.get<PagedRedisValuesResponse>(
            `/api/v1/storage/${connectionId}/keys`,
            { params }
        );
    }

    // ========== Bulk Delete ==========

    /**
     * Delete keys matching a pattern
     * @param connectionId - The connection ID
     * @param pattern - Pattern to match keys
     * @returns Observable of delete response
     */
    deleteKeysByPattern(
        connectionId: string,
        pattern: string
    ): Observable<DeleteKeysByPatternResponse> {
        if (!connectionId || !pattern) {
            return throwError(() => new Error('Connection ID and pattern are required'));
        }

        return this.httpClient.delete<DeleteKeysByPatternResponse>(
            `/api/v1/storage/${connectionId}/keys/pattern/${encodeURIComponent(pattern)}`
        );
    }

    /**
     * Delete multiple specific keys
     * @param connectionId - The connection ID
     * @param keys - Array of keys to delete
     * @returns Observable of delete response
     */
    deleteMultipleKeys(
        connectionId: string,
        keys: string[]
    ): Observable<DeleteMultipleKeysResponse> {
        if (!connectionId || !keys || keys.length === 0) {
            return throwError(() => new Error('Connection ID and keys are required'));
        }

        return this.httpClient.post<DeleteMultipleKeysResponse>(
            `/api/v1/storage/${connectionId}/keys/delete-multiple`,
            { keys }
        );
    }

    // ========== Export/Import ==========

    /**
     * Export Redis data to JSON or CSV
     * @param connectionId - The connection ID
     * @param pattern - Key pattern to export
     * @param format - Export format (json or csv)
     * @returns Observable of blob data
     */
    exportData(
        connectionId: string,
        pattern: string = '*',
        format: 'json' | 'csv' = 'json'
    ): Observable<Blob> {
        if (!connectionId) {
            return throwError(() => new Error('Connection ID is required'));
        }

        const params = new HttpParams()
            .set('pattern', pattern)
            .set('format', format);

        return this.httpClient.get(
            `/api/v1/storage/${connectionId}/export`,
            { 
                params,
                responseType: 'blob'
            }
        );
    }

    /**
     * Import Redis data from JSON or CSV
     * @param connectionId - The connection ID
     * @param request - Import request with data and format
     * @returns Observable of import response
     */
    importData(
        connectionId: string,
        request: ImportRedisDataRequest
    ): Observable<ImportRedisDataResponse> {
        if (!connectionId || !request) {
            return throwError(() => new Error('Connection ID and import data are required'));
        }

        return this.httpClient.post<ImportRedisDataResponse>(
            `/api/v1/storage/${connectionId}/import`,
            request
        );
    }

    // ========== Pub/Sub ==========

    /**
     * Publish a message to a Redis channel
     * @param connectionId - The connection ID
     * @param request - Publish message request
     * @returns Observable of void
     */
    publishMessage(
        connectionId: string,
        request: PublishMessageRequest
    ): Observable<void> {
        if (!connectionId || !request) {
            return throwError(() => new Error('Connection ID and message data are required'));
        }

        return this.httpClient.post<void>(
            `/api/v1/pubsub/${connectionId}/publish`,
            request
        );
    }

    /**
     * Subscribe to a Redis channel
     * @param connectionId - The connection ID
     * @param channel - Channel name to subscribe to
     * @returns Observable of subscription response
     */
    subscribeToChannel(
        connectionId: string,
        channel: string
    ): Observable<SubscribeToChannelResponse> {
        if (!connectionId || !channel) {
            return throwError(() => new Error('Connection ID and channel are required'));
        }

        return this.httpClient.post<SubscribeToChannelResponse>(
            `/api/v1/pubsub/${connectionId}/subscribe/${encodeURIComponent(channel)}`,
            {}
        );
    }

    /**
     * Unsubscribe from a Redis channel
     * @param connectionId - The connection ID
     * @param subscriptionId - Subscription ID to unsubscribe
     * @returns Observable of void
     */
    unsubscribeFromChannel(
        connectionId: string,
        subscriptionId: string
    ): Observable<void> {
        if (!connectionId || !subscriptionId) {
            return throwError(() => new Error('Connection ID and subscription ID are required'));
        }

        return this.httpClient.delete<void>(
            `/api/v1/pubsub/${connectionId}/subscribe/${subscriptionId}`
        );
    }

    // ========== Transactions ==========

    /**
     * Execute a Redis transaction (MULTI/EXEC)
     * @param connectionId - The connection ID
     * @param request - Transaction execution request
     * @returns Observable of transaction response
     */
    executeTransaction(
        connectionId: string,
        request: ExecuteTransactionRequest
    ): Observable<ExecuteTransactionResponse> {
        if (!connectionId || !request) {
            return throwError(() => new Error('Connection ID and transaction data are required'));
        }

        return this.httpClient.post<ExecuteTransactionResponse>(
            `/api/v1/transaction/${connectionId}/execute`,
            request
        );
    }

    // ========== Lua Scripts ==========

    /**
     * Execute a Lua script in Redis
     * @param connectionId - The connection ID
     * @param request - Lua script execution request
     * @returns Observable of script execution response
     */
    executeLuaScript(
        connectionId: string,
        request: ExecuteLuaScriptRequest
    ): Observable<ExecuteLuaScriptResponse> {
        if (!connectionId || !request) {
            return throwError(() => new Error('Connection ID and script data are required'));
        }

        return this.httpClient.post<ExecuteLuaScriptResponse>(
            `/api/v1/script/${connectionId}/execute`,
            request
        );
    }

    // ========== Health Monitoring ==========

    /**
     * Get connection health status
     * @param connectionId - The connection ID
     * @returns Observable of health response
     */
    getConnectionHealth(connectionId: string): Observable<GetConnectionHealthResponse> {
        if (!connectionId) {
            return throwError(() => new Error('Connection ID is required'));
        }

        return this.httpClient.get<GetConnectionHealthResponse>(
            `/api/v1/connections/${connectionId}/health`
        );
    }

}

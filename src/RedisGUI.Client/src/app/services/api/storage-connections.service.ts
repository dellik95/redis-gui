import { inject, Injectable } from "@angular/core";
import { Observable, throwError, shareReplay } from "rxjs";
import { Connection as ConnectionType } from "../../models/connection.type";
import { HttpClient } from "@angular/common/http";
import { CheckConnectionRequest, CreateConnectionRequest, UpdateConnectionRequest } from "../../models/connection-request.types";

@Injectable({
    providedIn: "root"
})
export class StorageConnectionsService {
    private readonly httpClient = inject(HttpClient);
    private readonly BASE_URL = '/api/v1/connections';
    
    // Cache for connections list
    private connectionsCache$: Observable<ConnectionType[]> | null = null;
    private readonly CACHE_DURATION = 30000; // 30 seconds

    /**
     * Get all connections
     * @returns Observable of connections array
     */
    getConnections(): Observable<ConnectionType[]> {
        // Return cached observable if available
        if (this.connectionsCache$) {
            return this.connectionsCache$;
        }

        this.connectionsCache$ = this.httpClient.get<ConnectionType[]>(this.BASE_URL).pipe(
            shareReplay({ bufferSize: 1, refCount: true })
        );

        // Clear cache after duration
        setTimeout(() => {
            this.connectionsCache$ = null;
        }, this.CACHE_DURATION);

        return this.connectionsCache$;
    }

    /**
     * Save a new connection
     * @param connection - Connection data to create
     * @returns Observable of created connection
     */
    saveConnection(connection: CreateConnectionRequest): Observable<any> {
        if (!connection) {
            return throwError(() => new Error('Connection data is required'));
        }

        return this.httpClient.post<any>(this.BASE_URL, connection);
    }

    /**
     * Update an existing connection
     * @param id - Connection ID
     * @param connection - Updated connection data
     * @returns Observable of void
     */
    updateConnection(id: string, connection: UpdateConnectionRequest): Observable<void> {
        if (!id || !connection) {
            return throwError(() => new Error('Connection ID and data are required'));
        }

        return this.httpClient.put<void>(`${this.BASE_URL}/${id}`, connection);
    }

    /**
     * Delete a connection
     * @param id - Connection ID to delete
     * @returns Observable of delete result
     */
    deleteRecord(id: string): Observable<any> {
        if (!id) {
            return throwError(() => new Error('Connection ID is required'));
        }

        return this.httpClient.delete(`${this.BASE_URL}/${id}`);
    }

    /**
     * Check if a connection is valid
     * @param connection - Connection data to check
     * @returns Observable of connection check result
     */
    checkConnection(connection: Partial<CheckConnectionRequest>): Observable<any> {
        if (!connection) {
            return throwError(() => new Error('Connection data is required'));
        }

        return this.httpClient.post<any>(`${this.BASE_URL}/check`, connection);
    }

    /**
     * Clear the connections cache
     * Call this after creating, updating, or deleting connections
     */
    clearCache(): void {
        this.connectionsCache$ = null;
    }
}

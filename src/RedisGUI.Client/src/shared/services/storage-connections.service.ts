import { inject, Injectable } from "@angular/core";
import { Observable, takeLast } from "rxjs";
import { Connection as ConnectionType } from "../types/connection.type";
import { HttpClient } from "@angular/common/http";
import { CheckConnectionRequest, CreateConnectionRequest } from "../types/connection-request.types";

@Injectable({
    providedIn: "root"
})
export class StorageConnectionsService {
    private httpClient: HttpClient = inject(HttpClient);

    getConnections(): Observable<ConnectionType[]> {
        return this.httpClient.get<ConnectionType[]>("/api/v1/connections");
    }

    saveConnection(connection: CreateConnectionRequest): Observable<any> {
        return this.httpClient.post<any>("/api/v1/connections", connection).pipe(takeLast(1));
    }

    deleteRecord(id: string): Observable<any> {
        return this.httpClient.delete(`/api/v1/connections/${id}`).pipe(takeLast(1));
    }

    checkConnection(connection: Partial<CheckConnectionRequest>): Observable<any> {
        return this.httpClient.post<any>("/api/v1/connections/check", connection).pipe(takeLast(1));
    }
}

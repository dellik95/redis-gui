import { inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { ConnectionType } from "../types/connection.type";
import { HttpClient } from "@angular/common/http";
import { CreateConnectionRequest } from "../types/create-connectionrequest.type";

@Injectable({
    providedIn: "root"
})
export class RedisConnectionsService {
    private httpClient: HttpClient = inject(HttpClient);

    getConnections(): Observable<ConnectionType[]> {
        return this.httpClient.get<ConnectionType[]>("/api/v1/connections");
    }

    saveConnection(connection: Partial<CreateConnectionRequest>): Observable<any> {
        return this.httpClient.post<any>("/api/v1/connections", connection);
    }

    deleteRecord(id: string): Observable<any> {
        return this.httpClient.delete(`/api/v1/connections/${id}`);
    }
}

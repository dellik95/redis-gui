import { Injectable } from '@angular/core';
import { interval, map, Observable } from 'rxjs';
import { HttpTransportType, HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr"
import { SystemMetrics } from '../types/system-metrics.type';

@Injectable({
  providedIn: 'root'
})
export class StorageResourseDetailsService {
  private hubConnection: HubConnection | null = null;

  startConnection(id: string): Observable<void> {
    if (this.hubConnection == null) {
      this.hubConnection = new HubConnectionBuilder()
        .withUrl(`/hub/metrics?connectionId=${id}`)
        .withAutomaticReconnect()
        .build();
    }
    return new Observable<void>((observer) => {
      this.hubConnection!
        .start()
        .then(() => {
          observer.next();
          observer.complete();
        })
        .catch((error) => {
          observer.error(error);
        });
    });
  }

  stopConnection() {
    this.hubConnection!.stop()
      .then(() => {
        console.log('Connection stopped.');
      })
      .catch((error) => {
        console.error('Error to stop connection:', error);
      });
  }

  receiveMessage(): Observable<SystemMetrics> {
    return new Observable<SystemMetrics>((observer) => {
      this.hubConnection!.on('NotifyUserAsync', (message: SystemMetrics) => {
        observer.next(message);
      });
    });
  }
}
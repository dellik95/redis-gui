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
        .configureLogging(LogLevel.Trace)
        .build();
    }
    return new Observable<void>((observer) => {
      this.hubConnection!
        .start()
        .then(() => {
          console.log('Connection established with SignalR hub');
          observer.next();
          observer.complete();
        })
        .catch((error) => {
          console.error('Error connecting to SignalR hub:', error);
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
        console.log(message);
        observer.next(message);
      });
    });
  }
}
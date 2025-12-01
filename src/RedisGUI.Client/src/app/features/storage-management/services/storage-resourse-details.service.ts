import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { SignalRHubService } from '../../../../app/core/services/signalr-hub.service';
import { SystemMetrics } from '../models/system-metrics.type';

/**
 * SignalR service for real-time storage resource metrics
 * Connects to the metrics hub and streams system metrics data
 */
@Injectable({
  providedIn: 'root'
})
export class StorageResourseDetailsService extends SignalRHubService<SystemMetrics> {
  protected override hubUrl = '/hub/metrics';
  protected override eventName = 'NotifyUserAsync';

  /**
   * Start connection with connection ID
   */
  startConnection(id: string): Observable<void> {
    return this.connect({ connectionId: id });
  }

  /**
   * Stop the connection
   */
  stopConnection(): Observable<void> {
    return this.disconnect();
  }

  /**
   * Receive system metrics messages
   */
  receiveMessage(): Observable<SystemMetrics> {
    return this.onMessage();
  }

  protected override onConnected(): void {
    console.log('Storage metrics connection established');
  }

  protected override onDisconnected(): void {
    console.log('Storage metrics connection closed');
  }

  protected override onConnectionError(error: any): void {
    console.error('Storage metrics connection error:', error);
  }
}

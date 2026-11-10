import { Injectable } from '@angular/core';
import { SignalRHubService } from '../signalr-hub.service';
import { KeyChangeEvent } from '../../stores/signalr.store';
import { SIGNALR_HUBS, SIGNALR_METHODS } from '../../constants/api-routes.constants';

/**
 * Key Change Hub Service
 * Manages SignalR connection for Redis key change notifications
 * 
 * Matches backend: KeyChangeHub and IKeyChangeClientMethods
 */
@Injectable({ providedIn: 'root' })
export class KeyChangeHubService extends SignalRHubService<KeyChangeEvent> {
  protected override hubUrl = SIGNALR_HUBS.KEY_CHANGE;
  protected override eventName = SIGNALR_METHODS.KEY_CHANGE.KEY_CHANGED;
}


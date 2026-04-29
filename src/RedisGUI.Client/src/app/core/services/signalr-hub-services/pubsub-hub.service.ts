import { Injectable } from '@angular/core';
import { SignalRHubService } from '../signalr-hub.service';
import { PubSubMessage } from '../../stores/signalr.store';
import { SIGNALR_HUBS, SIGNALR_METHODS } from '../../constants/api-routes.constants';

/**
 * Pub/Sub Hub Service
 * Manages SignalR connection for Redis Pub/Sub messages
 * 
 * Matches backend: PubSubHub and IPubSubClientMethods
 */
@Injectable({ providedIn: 'root' })
export class PubSubHubService extends SignalRHubService<PubSubMessage> {
  protected override hubUrl = SIGNALR_HUBS.PUBSUB;
  protected override eventName = SIGNALR_METHODS.PUBSUB.RECEIVE_MESSAGE;
}


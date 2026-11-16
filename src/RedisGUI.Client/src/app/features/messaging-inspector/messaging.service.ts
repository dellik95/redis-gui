import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface StreamEntry { id: string; fields: { name: string; value: string }[]; }
export interface StreamSnapshot { length: number; groups: number; lastGeneratedId: string; entries: StreamEntry[]; }
export interface LiveMessage { channel: string; payload: string; receivedAt: string; }

@Injectable({ providedIn: 'root' })
export class MessagingService {
  private readonly http = inject(HttpClient);
  stream(connectionId: string, key: string) {
    return this.http.get<StreamSnapshot>(`/api/v1/messaging/${connectionId}/stream`, { params: { key } });
  }
  channelInfo(connectionId: string, channel: string) {
    return this.http.get<{ subscribers: number; patterns: number }>(`/api/v1/messaging/${connectionId}/channel`, { params: { channel } });
  }
  publish(connectionId: string, channel: string, message: string) {
    return this.http.post<{ subscribers: number }>(`/api/v1/messaging/${connectionId}/publish`, { channel, message });
  }
}

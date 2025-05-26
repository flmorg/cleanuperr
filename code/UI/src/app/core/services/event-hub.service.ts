import { Injectable } from '@angular/core';
import { BaseSignalRService } from './base-signalr.service';
import { Event } from '../models/event.models';
import { SignalRHubConfig } from '../models/signalr.models';
import { environment } from '../../../environments/environment';
import * as signalR from '@microsoft/signalr';

/**
 * Service for connecting to the events SignalR hub
 */
@Injectable({
  providedIn: 'root'
})
export class EventHubService extends BaseSignalRService<Event> {
  constructor() {
    // Configuration for the events hub
    const config: SignalRHubConfig = {
      hubUrl: `${environment.apiUrl}/api/hubs/events`,
      maxReconnectAttempts: 0, // Infinite reconnection attempts for self-hosted
      reconnectDelayMs: 2000,
      bufferSize: 1000, // Keep more events in buffer
      healthCheckIntervalMs: 30000 // Check connection every 30 seconds
    };
    
    super(config, 'EventReceived');
  }
  
  /**
   * Override to handle both EventReceived and RecentEventsReceived events
   */
  protected override registerSignalREvents(): void {
    // Call base implementation for standard connection events
    super.registerSignalREvents();

    // Handle recent events response (bulk load)
    this.hubConnection.on('RecentEventsReceived', (events: Event[]) => {
      this.messageSubject.next(events);
      console.log(`Received ${events.length} recent events`);
    });
  }
  
  /**
   * Request recent events from the server
   */
  public requestRecentEvents(count: number = 100): void {
    if (this.hubConnection && 
        this.hubConnection.state === signalR.HubConnectionState.Connected) {
      this.hubConnection.invoke('GetRecentEvents', count)
        .catch(err => console.error('Error while requesting recent events:', err));
    }
  }
  
  /**
   * Override to request recent events when connection is established
   */
  protected override onConnectionEstablished(): void {
    this.requestRecentEvents();
  }
  
  /**
   * Get the buffered events
   */
  public getBufferedEvents(): Event[] {
    return this.getBufferedMessages();
  }
  
  /**
   * Get events as an observable
   */
  public getEvents() {
    return this.getMessages();
  }

  /**
   * Get new events as an observable
   */
  public getNewEvents() {
    return this.getMessages();
  }

  /**
   * Clear current events
   */
  public clearEvents(): void {
    this.messageSubject.next([]);
  }
} 
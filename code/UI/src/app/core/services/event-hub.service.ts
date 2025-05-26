import { Injectable, inject } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { Event } from '../models/event.models';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class EventHubService {
  private hubConnection!: HubConnection;
  private eventsSubject = new BehaviorSubject<Event[]>([]);
  private connectionStatusSubject = new BehaviorSubject<boolean>(false);
  private newEventSubject = new Subject<Event>();

  constructor() {
    this.createConnection();
  }

  /**
   * Create SignalR connection
   */
  private createConnection(): void {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/eventhub`)
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .configureLogging(LogLevel.Information)
      .build();

    this.setupConnectionEvents();
    this.setupEventHandlers();
  }

  /**
   * Setup connection event handlers
   */
  private setupConnectionEvents(): void {
    this.hubConnection.onclose(() => {
      this.connectionStatusSubject.next(false);
      console.log('EventHub connection closed');
    });

    this.hubConnection.onreconnecting(() => {
      this.connectionStatusSubject.next(false);
      console.log('EventHub reconnecting...');
    });

    this.hubConnection.onreconnected(() => {
      this.connectionStatusSubject.next(true);
      console.log('EventHub reconnected');
      this.joinEventsGroup();
    });
  }

  /**
   * Setup event handlers from server
   */
  private setupEventHandlers(): void {
    // Handle new events from server
    this.hubConnection.on('EventReceived', (event: Event) => {
      const currentEvents = this.eventsSubject.value;
      const updatedEvents = [event, ...currentEvents].slice(0, 1000); // Keep latest 1000 events
      this.eventsSubject.next(updatedEvents);
      this.newEventSubject.next(event);
    });

    // Handle recent events response
    this.hubConnection.on('RecentEventsReceived', (events: Event[]) => {
      this.eventsSubject.next(events);
    });
  }

  /**
   * Start the connection
   */
  async startConnection(): Promise<void> {
    try {
      await this.hubConnection.start();
      this.connectionStatusSubject.next(true);
      console.log('EventHub connection started');
      await this.joinEventsGroup();
      await this.requestRecentEvents();
    } catch (error) {
      this.connectionStatusSubject.next(false);
      console.error('Error starting EventHub connection:', error);
      throw error;
    }
  }

  /**
   * Stop the connection
   */
  async stopConnection(): Promise<void> {
    try {
      await this.hubConnection.stop();
      this.connectionStatusSubject.next(false);
      console.log('EventHub connection stopped');
    } catch (error) {
      console.error('Error stopping EventHub connection:', error);
    }
  }

  /**
   * Join events group to receive all events
   */
  async joinEventsGroup(): Promise<void> {
    if (this.hubConnection.state === 'Connected') {
      try {
        await this.hubConnection.invoke('JoinEventsGroup');
      } catch (error) {
        console.error('Error joining events group:', error);
      }
    }
  }

  /**
   * Leave events group
   */
  async leaveEventsGroup(): Promise<void> {
    if (this.hubConnection.state === 'Connected') {
      try {
        await this.hubConnection.invoke('LeaveEventsGroup');
      } catch (error) {
        console.error('Error leaving events group:', error);
      }
    }
  }

  /**
   * Join severity-specific group
   */
  async joinSeverityGroup(severity: string): Promise<void> {
    if (this.hubConnection.state === 'Connected') {
      try {
        await this.hubConnection.invoke('JoinSeverityGroup', severity);
      } catch (error) {
        console.error(`Error joining severity group ${severity}:`, error);
      }
    }
  }

  /**
   * Leave severity-specific group
   */
  async leaveSeverityGroup(severity: string): Promise<void> {
    if (this.hubConnection.state === 'Connected') {
      try {
        await this.hubConnection.invoke('LeaveSeverityGroup', severity);
      } catch (error) {
        console.error(`Error leaving severity group ${severity}:`, error);
      }
    }
  }

  /**
   * Join event type-specific group
   */
  async joinTypeGroup(eventType: string): Promise<void> {
    if (this.hubConnection.state === 'Connected') {
      try {
        await this.hubConnection.invoke('JoinTypeGroup', eventType);
      } catch (error) {
        console.error(`Error joining type group ${eventType}:`, error);
      }
    }
  }

  /**
   * Leave event type-specific group
   */
  async leaveTypeGroup(eventType: string): Promise<void> {
    if (this.hubConnection.state === 'Connected') {
      try {
        await this.hubConnection.invoke('LeaveTypeGroup', eventType);
      } catch (error) {
        console.error(`Error leaving type group ${eventType}:`, error);
      }
    }
  }

  /**
   * Request recent events from server
   */
  async requestRecentEvents(count: number = 100): Promise<void> {
    if (this.hubConnection.state === 'Connected') {
      try {
        await this.hubConnection.invoke('GetRecentEvents', count);
      } catch (error) {
        console.error('Error requesting recent events:', error);
      }
    }
  }

  /**
   * Get events observable
   */
  getEvents(): Observable<Event[]> {
    return this.eventsSubject.asObservable();
  }

  /**
   * Get connection status observable
   */
  getConnectionStatus(): Observable<boolean> {
    return this.connectionStatusSubject.asObservable();
  }

  /**
   * Get new event observable
   */
  getNewEvents(): Observable<Event> {
    return this.newEventSubject.asObservable();
  }

  /**
   * Get current connection state
   */
  get connectionState(): string {
    return this.hubConnection.state;
  }

  /**
   * Clear current events
   */
  clearEvents(): void {
    this.eventsSubject.next([]);
  }
} 
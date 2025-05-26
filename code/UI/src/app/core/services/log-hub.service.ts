import { Injectable, inject } from '@angular/core';
import { BaseSignalRService } from './base-signalr.service';
import { LogEntry, SignalRHubConfig } from '../models/signalr.models';
import { environment } from '../../../environments/environment';
import * as signalR from '@microsoft/signalr';

/**
 * Service for connecting to the logs SignalR hub
 */
@Injectable({
  providedIn: 'root'
})
export class LogHubService extends BaseSignalRService<LogEntry> {
  constructor() {
    // Default configuration for the logs hub
    const config: SignalRHubConfig = {
      hubUrl: `${environment.apiUrl}/api/hubs/logs`,
      maxReconnectAttempts: 0, // Infinite reconnection attempts
      reconnectDelayMs: 2000,
      bufferSize: 100,
      healthCheckIntervalMs: 30000 // Check connection every 30 seconds
    };
    
    super(config, 'ReceiveLog');
  }
  
  /**
   * Request recent logs from the server
   */
  public requestRecentLogs(): void {
    if (this.hubConnection && 
        this.hubConnection.state === signalR.HubConnectionState.Connected) {
      this.hubConnection.invoke('RequestRecentLogs')
        .catch(err => console.error('Error while requesting recent logs:', err));
    }
  }
  
  /**
   * Override to request recent logs when connection is established
   */
  protected override onConnectionEstablished(): void {
    this.requestRecentLogs();
  }
  
  /**
   * Get the buffered logs
   */
  public getBufferedLogs(): LogEntry[] {
    return this.getBufferedMessages();
  }
  
  /**
   * Get logs as an observable
   */
  public getLogs() {
    return this.getMessages();
  }
}

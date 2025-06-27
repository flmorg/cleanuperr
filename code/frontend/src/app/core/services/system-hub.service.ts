import { Injectable, inject } from '@angular/core';
import { BaseSignalRService } from './base-signalr.service';
import { ApplicationPathService } from './base-path.service';
import { SignalRHubConfig } from '../models/signalr.models';
import * as signalR from '@microsoft/signalr';

/**
 * System status message from the system hub
 */
export interface SystemStatus {
  timestamp: Date;
  status: 'online' | 'offline' | 'degraded';
  message: string;
  serverVersion?: string;
  components?: {
    name: string;
    status: 'online' | 'offline' | 'degraded';
  }[];
}

/**
 * Service for connecting to the system monitoring SignalR hub
 */
@Injectable({
  providedIn: 'root'
})
export class SystemHubService extends BaseSignalRService<SystemStatus> {
  constructor() {
    // Default configuration for the system hub
    const config: SignalRHubConfig = {
      hubUrl: '',
      maxReconnectAttempts: 0, // Infinite reconnection attempts
      reconnectDelayMs: 2000,
      bufferSize: 10, // Only need a few status updates
      healthCheckIntervalMs: 30000 // Check connection every 30 seconds
    };
    
    super(config, 'ReceiveSystemStatus');
  }
  
  /**
   * Request the current system status
   */
  public requestSystemStatus(): void {
    if (this.hubConnection && 
        this.hubConnection.state === signalR.HubConnectionState.Connected) {
      this.hubConnection.invoke('RequestSystemStatus')
        .catch(err => console.error('Error while requesting system status:', err));
    }
  }
  
  /**
   * Override to request system status when connection is established
   */
  protected override onConnectionEstablished(): void {
    this.requestSystemStatus();
  }
  
  /**
   * Get the latest system status
   */
  public getLatestStatus(): SystemStatus | undefined {
    const messages = this.getBufferedMessages();
    return messages.length > 0 ? messages[messages.length - 1] : undefined;
  }
}

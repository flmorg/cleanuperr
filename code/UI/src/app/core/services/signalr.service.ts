import { Injectable, OnDestroy } from '@angular/core';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import * as signalR from '@microsoft/signalr';

// Define the LogEntry interface locally to avoid dependency issues
export interface LogEntry {
  timestamp: Date;
  level: string;
  message: string;
  exception?: string;
  category?: string;
  jobName?: string;
  instanceName?: string;
}

@Injectable({
  providedIn: 'root'
})
export class SignalrService implements OnDestroy {
  private hubConnection!: signalR.HubConnection;
  private hubUrl = 'http://localhost:5000/api/hubs/logs';
  private logSubject = new BehaviorSubject<LogEntry[]>([]);
  private connectionStatusSubject = new BehaviorSubject<boolean>(false);
  private destroy$ = new Subject<void>();
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private reconnectDelayMs = 2000;
  private bufferSize = 100;
  private logBuffer: LogEntry[] = [];

  constructor() { }

  /**
   * Start the SignalR connection to the hub
   */
  public startConnection(): Promise<void> {
    if (this.hubConnection) {
      return Promise.resolve();
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl)
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.previousRetryCount >= this.maxReconnectAttempts) {
            return null; // Stop trying after max attempts
          }
          
          // Implement exponential backoff
          return Math.min(this.reconnectDelayMs * Math.pow(2, retryContext.previousRetryCount), 30000);
        }
      })
      .build();

    this.registerSignalREvents();

    return this.hubConnection.start()
      .then(() => {
        console.log('SignalR connection started');
        this.connectionStatusSubject.next(true);
        this.reconnectAttempts = 0;
        this.requestRecentLogs();
      })
      .catch(err => {
        console.error('Error while starting SignalR connection:', err);
        this.connectionStatusSubject.next(false);
        
        if (this.reconnectAttempts < this.maxReconnectAttempts) {
          this.reconnectAttempts++;
          setTimeout(() => this.startConnection(), this.reconnectDelayMs);
        }
        
        throw err;
      });
  }

  /**
   * Stop the SignalR connection
   */
  public stopConnection(): Promise<void> {
    if (!this.hubConnection) {
      return Promise.resolve();
    }

    return this.hubConnection.stop()
      .then(() => {
        console.log('SignalR connection stopped');
        this.connectionStatusSubject.next(false);
      })
      .catch(err => {
        console.error('Error while stopping SignalR connection:', err);
        throw err;
      });
  }

  /**
   * Register event handlers for SignalR events
   */
  private registerSignalREvents(): void {
    this.hubConnection.on('ReceiveLog', (logEntry: LogEntry) => {
      this.addToBuffer(logEntry);
      const currentLogs = this.logSubject.value;
      this.logSubject.next([...currentLogs, logEntry]);
    });

    this.hubConnection.onreconnected(() => {
      console.log('SignalR connection reconnected');
      this.connectionStatusSubject.next(true);
      this.reconnectAttempts = 0;
      
      // Request recent logs to ensure we have the latest data
      this.requestRecentLogs();
    });

    this.hubConnection.onreconnecting(() => {
      console.log('SignalR connection reconnecting...');
      this.connectionStatusSubject.next(false);
    });

    this.hubConnection.onclose(() => {
      console.log('SignalR connection closed');
      this.connectionStatusSubject.next(false);
    });
  }

  /**
   * Request recent logs from the server
   */
  public requestRecentLogs(): void {
    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
      this.hubConnection.invoke('RequestRecentLogs')
        .catch(err => console.error('Error while requesting recent logs:', err));
    }
  }

  /**
   * Add a log entry to the buffer
   */
  private addToBuffer(logEntry: LogEntry): void {
    this.logBuffer.push(logEntry);
    
    // Trim buffer if it exceeds the limit
    if (this.logBuffer.length > this.bufferSize) {
      this.logBuffer.shift();
    }
  }

  /**
   * Get all logs from the buffer
   */
  public getBufferedLogs(): LogEntry[] {
    return [...this.logBuffer];
  }

  /**
   * Get logs as an observable
   */
  public getLogs(): Observable<LogEntry[]> {
    return this.logSubject.asObservable();
  }

  /**
   * Get connection status as an observable
   */
  public getConnectionStatus(): Observable<boolean> {
    return this.connectionStatusSubject.asObservable();
  }

  /**
   * Clean up resources
   */
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.stopConnection();
  }
}

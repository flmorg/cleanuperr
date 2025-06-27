/**
 * Models for SignalR connections and messages
 */

/**
 * Configuration options for a SignalR hub connection
 */
export interface SignalRHubConfig {
  /** URL to the SignalR hub endpoint */
  hubUrl: string;
  /** Maximum number of reconnection attempts (0 for infinite) */
  maxReconnectAttempts: number;
  /** Initial delay between reconnection attempts in milliseconds (will be subject to backoff) */
  reconnectDelayMs: number;
  /** Maximum size of the message buffer */
  bufferSize: number;
  /** Interval in milliseconds to check connection health (0 to disable) */
  healthCheckIntervalMs: number;
}

/**
 * Standard log entry message format received from the server
 */
export interface LogEntry {
  timestamp: Date;
  level: string;
  message: string;
  exception?: string;
  category?: string;
  jobName?: string;
  instanceName?: string;
}

import { DownloadClientType } from './enums';

/**
 * Represents a download client configuration object
 */
export interface DownloadClientConfig {
  /**
   * Collection of download clients configured for the application
   */
  clients: ClientConfig[];
}

/**
 * Represents an individual download client configuration
 */
export interface ClientConfig {
  /**
   * Whether this client is enabled
   */
  enabled: boolean;
  
  /**
   * Unique identifier for this client
   */
  id: string;
  
  /**
   * Friendly name for this client
   */
  name: string;
  
  /**
   * Type of download client
   */
  type: DownloadClientType;
  
  /**
   * Host address for the download client
   */
  host: string;
  
  /**
   * Username for authentication
   */
  username: string;
  
  /**
   * Password for authentication (only included in update)
   */
  password?: string;
  
  /**
   * The base URL path component, used by clients like Transmission and Deluge
   */
  urlBase: string;
}

/**
 * Update DTO model for download client configuration
 */
export interface DownloadClientConfigUpdateDto extends DownloadClientConfig {
  /**
   * Clients with potentially sensitive data for updates
   */
  clients: ClientConfigUpdateDto[];
}

/**
 * Update DTO for client configuration (includes password)
 */
export interface ClientConfigUpdateDto extends ClientConfig {
  /**
   * Password for authentication (only included in update)
   */
  password?: string;
}

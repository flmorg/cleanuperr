/**
 * LidarrConfig model definitions for the UI
 * These models represent the structures used in the API for Lidarr configuration
 */

import { ArrInstance } from "./arr-config.model";

/**
 * Main LidarrConfig model representing the configuration for Lidarr integration
 */
export interface LidarrConfig {
  failedImportMaxStrikes: number;
  instances: ArrInstance[];
}

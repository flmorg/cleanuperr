/**
 * RadarrConfig model definitions for the UI
 * These models represent the structures used in the API for Radarr configuration
 */

import { ArrInstance } from "./arr-config.model";

/**
 * Main RadarrConfig model representing the configuration for Radarr integration
 */
export interface RadarrConfig {
  failedImportMaxStrikes: number;
  instances: ArrInstance[];
}

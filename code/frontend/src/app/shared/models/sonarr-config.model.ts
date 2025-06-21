/**
 * SonarrConfig model definitions for the UI
 * These models represent the structures used in the API for Sonarr configuration
 */

import { ArrInstance } from "./arr-config.model";

/**
 * Main SonarrConfig model representing the configuration for Sonarr integration
 */
export interface SonarrConfig {
  failedImportMaxStrikes: number;
  instances: ArrInstance[];
}

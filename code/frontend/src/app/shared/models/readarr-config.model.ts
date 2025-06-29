/**
 * ReadarrConfig model definitions for the UI
 * These models represent the structures used in the API for Readarr configuration
 */

import { ArrInstance } from "./arr-config.model";

/**
 * Main ReadarrConfig model representing the configuration for Readarr integration
 */
export interface ReadarrConfig {
  failedImportMaxStrikes: number;
  instances: ArrInstance[];
} 
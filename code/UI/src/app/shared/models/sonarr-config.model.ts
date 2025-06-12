/**
 * SonarrConfig model definitions for the UI
 * These models represent the structures used in the API for Sonarr configuration
 */

import { ArrInstance } from "./arr-config.model";

/**
 * Defines the possible search types for Sonarr
 */
export enum SonarrSearchType {
  Episode = 'Episode',
  Season = 'Season',
  Series = 'Series'
}

/**
 * Main SonarrConfig model representing the configuration for Sonarr integration
 */
export interface SonarrConfig {
  enabled: boolean;
  failedImportMaxStrikes: number;
  instances: ArrInstance[];
  searchType: SonarrSearchType;
}

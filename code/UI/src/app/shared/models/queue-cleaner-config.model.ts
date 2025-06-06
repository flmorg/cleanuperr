export enum ScheduleUnit {
  Seconds = 'Seconds',
  Minutes = 'Minutes',
  Hours = 'Hours'
}

export interface JobSchedule {
  every: number;
  type: ScheduleUnit;
}

export enum BlocklistType {
  Blacklist = 'Blacklist',
  Whitelist = 'Whitelist'
}

export interface BlocklistSettings {
  path: string;
  type: BlocklistType;
}

// Nested configuration interfaces
export interface FailedImportConfig {
  maxStrikes: number;
  ignorePrivate: boolean;
  deletePrivate: boolean;
  ignorePatterns: string[];
}

export interface StalledConfig {
  maxStrikes: number;
  resetStrikesOnProgress: boolean;
  ignorePrivate: boolean;
  deletePrivate: boolean;
  downloadingMetadataMaxStrikes: number;
}

export interface SlowConfig {
  maxStrikes: number;
  resetStrikesOnProgress: boolean;
  ignorePrivate: boolean;
  deletePrivate: boolean;
  minSpeed: string;
  maxTime: number;
  ignoreAboveSize: string;
}

export interface ContentBlockerConfig {
  enabled: boolean;
  ignorePrivate: boolean;
  deletePrivate: boolean;
  sonarrBlocklist?: BlocklistSettings;
  radarrBlocklist?: BlocklistSettings;
  lidarrBlocklist?: BlocklistSettings;
  customBlocklists?: BlocklistSettings[];
}

export interface QueueCleanerConfig {
  enabled: boolean;
  cronExpression: string;
  jobSchedule?: JobSchedule; // UI-only field, not sent to API
  runSequentially: boolean;
  ignoredDownloadsPath: string;
  
  // Nested configurations
  failedImport: FailedImportConfig;
  stalled: StalledConfig;
  slow: SlowConfig;
  contentBlocker: ContentBlockerConfig;
  
  // Legacy flat properties for backward compatibility
  // These will be mapped to/from the nested structure
  failedImportMaxStrikes?: number;
  failedImportIgnorePrivate?: boolean;
  failedImportDeletePrivate?: boolean;
  failedImportIgnorePatterns?: string[];
  stalledMaxStrikes?: number;
  stalledResetStrikesOnProgress?: boolean;
  stalledIgnorePrivate?: boolean;
  stalledDeletePrivate?: boolean;
  downloadingMetadataMaxStrikes?: number;
  slowMaxStrikes?: number;
  slowResetStrikesOnProgress?: boolean;
  slowIgnorePrivate?: boolean;
  slowDeletePrivate?: boolean;
  slowMinSpeed?: string;
  slowMaxTime?: number;
  slowIgnoreAboveSize?: string;
}

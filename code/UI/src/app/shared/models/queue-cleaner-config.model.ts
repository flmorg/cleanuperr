export enum ScheduleUnit {
  Seconds = 'Seconds',
  Minutes = 'Minutes',
  Hours = 'Hours'
}

export interface JobSchedule {
  every: number;
  type: ScheduleUnit;
}

export interface QueueCleanerConfig {
  enabled: boolean;
  cronExpression: string;
  jobSchedule?: JobSchedule; // UI-only field, not sent to API
  runSequentially: boolean;
  ignoredDownloadsPath: string;
  
  // Failed Import settings
  failedImportMaxStrikes: number;
  failedImportIgnorePrivate: boolean;
  failedImportDeletePrivate: boolean;
  failedImportIgnorePatterns: string[];
  
  // Stalled settings
  stalledMaxStrikes: number;
  stalledResetStrikesOnProgress: boolean;
  stalledIgnorePrivate: boolean;
  stalledDeletePrivate: boolean;
  
  // Downloading Metadata settings
  downloadingMetadataMaxStrikes: number;
  
  // Slow Download settings
  slowMaxStrikes: number;
  slowResetStrikesOnProgress: boolean;
  slowIgnorePrivate: boolean;
  slowDeletePrivate: boolean;
  slowMinSpeed: string;
  slowMaxTime: number;
  slowIgnoreAboveSize: string;
}

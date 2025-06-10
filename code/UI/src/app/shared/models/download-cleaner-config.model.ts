import { ScheduleOptions, ScheduleUnit } from './queue-cleaner-config.model';

// Reuse the schedule unit enum and options from queue cleaner

export interface DownloadCleanerConfig {
  enabled: boolean;
  cronExpression: string;
  useAdvancedScheduling: boolean;
  categories: CleanCategory[];
  deletePrivate: boolean;
  unlinkedTargetCategory: string;
  unlinkedUseTag: boolean;
  unlinkedIgnoredRootDir: string;
  unlinkedCategories: string[];
}

export interface CleanCategory {
  name: string;
  deleteAfterDays: number;
  minimumSizeBytes?: number;
  maximumSizeBytes?: number;
  ignoredPaths?: string[];
}

export interface JobSchedule {
  every: number;
  type: ScheduleUnit;
}

// Default configuration
export const defaultDownloadCleanerConfig: DownloadCleanerConfig = {
  enabled: false,
  cronExpression: '0 0 * * * ?',
  useAdvancedScheduling: false,
  categories: [],
  deletePrivate: false,
  unlinkedTargetCategory: 'cleanuparr-unlinked',
  unlinkedUseTag: false,
  unlinkedIgnoredRootDir: '',
  unlinkedCategories: []
};

import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { JobSchedule, QueueCleanerConfig, ScheduleUnit } from '../../shared/models/queue-cleaner-config.model';

@Injectable({
  providedIn: 'root'
})
export class ConfigurationService {
  private readonly apiUrl = environment.apiUrl;
  private readonly http = inject(HttpClient);

  /**
   * Get queue cleaner configuration
   */
  getQueueCleanerConfig(): Observable<QueueCleanerConfig> {
    return this.http.get<QueueCleanerConfig>(`${this.apiUrl}/api/configuration/queue_cleaner`)
      .pipe(
        map(response => this.transformQueueCleanerResponse(response)),
        catchError(error => {
          console.error('Error fetching queue cleaner config:', error);
          return throwError(() => new Error('Failed to load queue cleaner configuration'));
        })
      );
  }

  /**
   * Update queue cleaner configuration
   */
  updateQueueCleanerConfig(config: QueueCleanerConfig): Observable<any> {
    // Create a copy to avoid modifying the original
    const configToSend = this.prepareQueueCleanerConfigForSending({ ...config });
    
    return this.http.put<any>(`${this.apiUrl}/api/configuration/queue_cleaner`, configToSend)
      .pipe(
        catchError(error => {
          console.error('Error updating queue cleaner config:', error);
          return throwError(() => new Error(error.error?.error || 'Failed to update queue cleaner configuration'));
        })
      );
  }

  /**
   * Transform the API response to our frontend model
   * Convert property names from PascalCase to camelCase
   */
  private transformQueueCleanerResponse(response: any): QueueCleanerConfig {
    const config: QueueCleanerConfig = {
      enabled: response.Enabled,
      cronExpression: response.CronExpression,
      runSequentially: response.RunSequentially,
      ignoredDownloadsPath: response.IgnoredDownloadsPath || '',
      
      // Failed Import settings
      failedImportMaxStrikes: response.FailedImportMaxStrikes,
      failedImportIgnorePrivate: response.FailedImportIgnorePrivate,
      failedImportDeletePrivate: response.FailedImportDeletePrivate,
      failedImportIgnorePatterns: response.FailedImportIgnorePatterns || [],
      
      // Stalled settings
      stalledMaxStrikes: response.StalledMaxStrikes,
      stalledResetStrikesOnProgress: response.StalledResetStrikesOnProgress,
      stalledIgnorePrivate: response.StalledIgnorePrivate,
      stalledDeletePrivate: response.StalledDeletePrivate,
      
      // Downloading Metadata settings
      downloadingMetadataMaxStrikes: response.DownloadingMetadataMaxStrikes,
      
      // Slow Download settings
      slowMaxStrikes: response.SlowMaxStrikes,
      slowResetStrikesOnProgress: response.SlowResetStrikesOnProgress,
      slowIgnorePrivate: response.SlowIgnorePrivate,
      slowDeletePrivate: response.SlowDeletePrivate,
      slowMinSpeed: response.SlowMinSpeed || '',
      slowMaxTime: response.SlowMaxTime,
      slowIgnoreAboveSize: response.SlowIgnoreAboveSize || '',
    };

    // Attempt to extract job schedule from cron expression
    // This is just UI sugar, not sent back to API
    config.jobSchedule = this.tryExtractJobScheduleFromCron(config.cronExpression);

    return config;
  }

  /**
   * Prepare configuration object for sending to API
   * Convert property names from camelCase to PascalCase
   */
  private prepareQueueCleanerConfigForSending(config: QueueCleanerConfig): any {
    // If we have a job schedule, update the cron expression
    if (config.jobSchedule) {
      config.cronExpression = this.convertJobScheduleToCron(config.jobSchedule);
    }

    // Remove UI-only properties
    const { jobSchedule, ...rest } = config;

    // Convert to PascalCase for backend
    return {
      Enabled: rest.enabled,
      CronExpression: rest.cronExpression,
      RunSequentially: rest.runSequentially,
      IgnoredDownloadsPath: rest.ignoredDownloadsPath,
      
      // Failed Import settings
      FailedImportMaxStrikes: rest.failedImportMaxStrikes,
      FailedImportIgnorePrivate: rest.failedImportIgnorePrivate,
      FailedImportDeletePrivate: rest.failedImportDeletePrivate,
      FailedImportIgnorePatterns: rest.failedImportIgnorePatterns,
      
      // Stalled settings
      StalledMaxStrikes: rest.stalledMaxStrikes,
      StalledResetStrikesOnProgress: rest.stalledResetStrikesOnProgress,
      StalledIgnorePrivate: rest.stalledIgnorePrivate,
      StalledDeletePrivate: rest.stalledDeletePrivate,
      
      // Downloading Metadata settings
      DownloadingMetadataMaxStrikes: rest.downloadingMetadataMaxStrikes,
      
      // Slow Download settings
      SlowMaxStrikes: rest.slowMaxStrikes,
      SlowResetStrikesOnProgress: rest.slowResetStrikesOnProgress,
      SlowIgnorePrivate: rest.slowIgnorePrivate,
      SlowDeletePrivate: rest.slowDeletePrivate,
      SlowMinSpeed: rest.slowMinSpeed,
      SlowMaxTime: rest.slowMaxTime,
      SlowIgnoreAboveSize: rest.slowIgnoreAboveSize,
    };
  }

  /**
   * Try to extract a JobSchedule from a cron expression
   * Only handles the simple cases we're generating
   */
  private tryExtractJobScheduleFromCron(cronExpression: string): JobSchedule | undefined {
    // Patterns we support:
    // Seconds: */n * * ? * * *
    // Minutes: 0 */n * ? * * *
    // Hours: 0 0 */n ? * * *
    try {
      const parts = cronExpression.split(' ');
      
      if (parts.length !== 7) return undefined;
      
      // Every n seconds
      if (parts[0].startsWith('*/') && parts[1] === '*') {
        const seconds = parseInt(parts[0].substring(2));
        if (!isNaN(seconds) && seconds > 0 && seconds < 60) {
          return { every: seconds, type: ScheduleUnit.Seconds };
        }
      }
      
      // Every n minutes
      if (parts[0] === '0' && parts[1].startsWith('*/')) {
        const minutes = parseInt(parts[1].substring(2));
        if (!isNaN(minutes) && minutes > 0 && minutes < 60) {
          return { every: minutes, type: ScheduleUnit.Minutes };
        }
      }
      
      // Every n hours
      if (parts[0] === '0' && parts[1] === '0' && parts[2].startsWith('*/')) {
        const hours = parseInt(parts[2].substring(2));
        if (!isNaN(hours) && hours > 0 && hours < 24) {
          return { every: hours, type: ScheduleUnit.Hours };
        }
      }
    } catch (e) {
      console.warn('Could not parse cron expression:', cronExpression);
    }
    
    return undefined;
  }

  /**
   * Convert a JobSchedule to a cron expression
   */
  private convertJobScheduleToCron(schedule: JobSchedule): string {
    if (!schedule || schedule.every <= 0) {
      return '0 0/5 * * * ?'; // Default: every 5 minutes
    }

    switch (schedule.type) {
      case ScheduleUnit.Seconds:
        if (schedule.every < 60) {
          return `*/${schedule.every} * * ? * * *`;
        }
        break;
        
      case ScheduleUnit.Minutes:
        if (schedule.every < 60) {
          return `0 */${schedule.every} * ? * * *`;
        }
        break;
        
      case ScheduleUnit.Hours:
        if (schedule.every < 24) {
          return `0 0 */${schedule.every} ? * * *`;
        }
        break;
    }

    // Fallback to default
    return '0 0/5 * * * ?';
  }
}

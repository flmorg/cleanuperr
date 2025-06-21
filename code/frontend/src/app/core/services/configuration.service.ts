import { HttpClient } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { Observable, catchError, map, throwError } from "rxjs";
import { JobSchedule, QueueCleanerConfig, ScheduleUnit } from "../../shared/models/queue-cleaner-config.model";
import { ContentBlockerConfig, JobSchedule as ContentBlockerJobSchedule, ScheduleUnit as ContentBlockerScheduleUnit } from "../../shared/models/content-blocker-config.model";
import { SonarrConfig } from "../../shared/models/sonarr-config.model";
import { RadarrConfig } from "../../shared/models/radarr-config.model";
import { LidarrConfig } from "../../shared/models/lidarr-config.model";
import { ClientConfig, DownloadClientConfig, CreateDownloadClientDto } from "../../shared/models/download-client-config.model";
import { ArrInstance, CreateArrInstanceDto } from "../../shared/models/arr-config.model";
import { GeneralConfig } from "../../shared/models/general-config.model";
import { BasePathService } from "./base-path.service";

@Injectable({
  providedIn: "root",
})
export class ConfigurationService {
  private readonly basePathService = inject(BasePathService);
  private readonly http = inject(HttpClient);

  /**
   * Get general configuration
   */
  getGeneralConfig(): Observable<GeneralConfig> {
    return this.http.get<GeneralConfig>(this.basePathService.buildApiUrl('/configuration/general')).pipe(
      catchError((error) => {
        console.error("Error fetching general config:", error);
        return throwError(() => new Error("Failed to load general configuration"));
      })
    );
  }

  /**
   * Update general configuration
   */
  updateGeneralConfig(config: GeneralConfig): Observable<any> {
    return this.http.put<any>(this.basePathService.buildApiUrl('/configuration/general'), config).pipe(
      catchError((error) => {
        console.error("Error updating general config:", error);
        return throwError(() => new Error(error.error?.error || "Failed to update general configuration"));
      })
    );
  }

  /**
   * Get queue cleaner configuration
   */
  getQueueCleanerConfig(): Observable<QueueCleanerConfig> {
    return this.http.get<QueueCleanerConfig>(this.basePathService.buildApiUrl('/configuration/queue_cleaner')).pipe(
      map((response) => {
        response.jobSchedule = this.tryExtractJobScheduleFromCron(response.cronExpression);
        return response;
      }),
      catchError((error) => {
        console.error("Error fetching queue cleaner config:", error);
        return throwError(() => new Error("Failed to load queue cleaner configuration"));
      })
    );
  }

  /**
   * Update queue cleaner configuration
   */
  updateQueueCleanerConfig(config: QueueCleanerConfig): Observable<QueueCleanerConfig> {
    config.cronExpression = this.convertJobScheduleToCron(config.jobSchedule!);
    return this.http.put<QueueCleanerConfig>(this.basePathService.buildApiUrl('/configuration/queue_cleaner'), config).pipe(
      catchError((error) => {
        console.error("Error updating queue cleaner config:", error);
        return throwError(() => new Error(error.error?.error || "Failed to update queue cleaner configuration"));
      })
    );
  }

  /**
   * Get content blocker configuration
   */
  getContentBlockerConfig(): Observable<ContentBlockerConfig> {
    return this.http.get<ContentBlockerConfig>(this.basePathService.buildApiUrl('/configuration/content_blocker')).pipe(
      map((response) => {
        response.jobSchedule = this.tryExtractContentBlockerJobScheduleFromCron(response.cronExpression);
        return response;
      }),
      catchError((error) => {
        console.error("Error fetching content blocker config:", error);
        return throwError(() => new Error("Failed to load content blocker configuration"));
      })
    );
  }

  /**
   * Update content blocker configuration
   */
  updateContentBlockerConfig(config: ContentBlockerConfig): Observable<void> {
    // Generate cron expression if using basic scheduling
    if (!config.useAdvancedScheduling && config.jobSchedule) {
      config.cronExpression = this.convertContentBlockerJobScheduleToCron(config.jobSchedule);
    }
    return this.http.put<void>(this.basePathService.buildApiUrl('/configuration/content_blocker'), config).pipe(
      catchError((error) => {
        console.error("Error updating content blocker config:", error);
        return throwError(() => new Error(error.error?.error || "Failed to update content blocker configuration"));
      })
    );
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
      const parts = cronExpression.split(" ");

      if (parts.length !== 7) return undefined;

      // Every n seconds
      if (parts[0].startsWith("*/") && parts[1] === "*") {
        const seconds = parseInt(parts[0].substring(2));
        if (!isNaN(seconds) && seconds > 0 && seconds < 60) {
          return { every: seconds, type: ScheduleUnit.Seconds };
        }
      }

      // Every n minutes
      if (parts[0] === "0" && parts[1].startsWith("*/")) {
        const minutes = parseInt(parts[1].substring(2));
        if (!isNaN(minutes) && minutes > 0 && minutes < 60) {
          return { every: minutes, type: ScheduleUnit.Minutes };
        }
      }

      // Every n hours
      if (parts[0] === "0" && parts[1] === "0" && parts[2].startsWith("*/")) {
        const hours = parseInt(parts[2].substring(2));
        if (!isNaN(hours) && hours > 0 && hours < 24) {
          return { every: hours, type: ScheduleUnit.Hours };
        }
      }
    } catch (e) {
      console.warn("Could not parse cron expression:", cronExpression);
    }

    return undefined;
  }

  /**
   * Convert a JobSchedule to a cron expression
   */
  private convertJobScheduleToCron(schedule: JobSchedule): string {
    if (!schedule || schedule.every <= 0) {
      return "0 0/5 * * * ?"; // Default: every 5 minutes
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
    return "0 0/5 * * * ?";
  }

  /**
   * Try to extract a ContentBlockerJobSchedule from a cron expression
   * Only handles the simple cases we're generating
   */
  private tryExtractContentBlockerJobScheduleFromCron(cronExpression: string): ContentBlockerJobSchedule | undefined {
    // Patterns we support:
    // Seconds: */n * * ? * * *
    // Minutes: 0 */n * ? * * *
    // Hours: 0 0 */n ? * * *
    try {
      const parts = cronExpression.split(" ");

      if (parts.length !== 7) return undefined;

      // Every n seconds
      if (parts[0].startsWith("*/") && parts[1] === "*") {
        const seconds = parseInt(parts[0].substring(2));
        if (!isNaN(seconds) && seconds > 0 && seconds < 60) {
          return { every: seconds, type: ContentBlockerScheduleUnit.Seconds };
        }
      }

      // Every n minutes
      if (parts[0] === "0" && parts[1].startsWith("*/")) {
        const minutes = parseInt(parts[1].substring(2));
        if (!isNaN(minutes) && minutes > 0 && minutes < 60) {
          return { every: minutes, type: ContentBlockerScheduleUnit.Minutes };
        }
      }

      // Every n hours
      if (parts[0] === "0" && parts[1] === "0" && parts[2].startsWith("*/")) {
        const hours = parseInt(parts[2].substring(2));
        if (!isNaN(hours) && hours > 0 && hours < 24) {
          return { every: hours, type: ContentBlockerScheduleUnit.Hours };
        }
      }
    } catch (e) {
      console.warn("Could not parse cron expression:", cronExpression);
    }

    return undefined;
  }

  /**
   * Convert a ContentBlockerJobSchedule to a cron expression
   */
  private convertContentBlockerJobScheduleToCron(schedule: ContentBlockerJobSchedule): string {
    if (!schedule || schedule.every <= 0) {
      return "0 0/5 * * * ?"; // Default: every 5 minutes
    }

    switch (schedule.type) {
      case ContentBlockerScheduleUnit.Seconds:
        if (schedule.every < 60) {
          return `*/${schedule.every} * * ? * * *`;
        }
        break;

      case ContentBlockerScheduleUnit.Minutes:
        if (schedule.every < 60) {
          return `0 */${schedule.every} * ? * * *`;
        }
        break;

      case ContentBlockerScheduleUnit.Hours:
        if (schedule.every < 24) {
          return `0 0 */${schedule.every} ? * * *`;
        }
        break;
    }

    // Fallback to default
    return "0 0/5 * * * ?";
  }

  /**
   * Get Sonarr configuration
   */
  getSonarrConfig(): Observable<SonarrConfig> {
    return this.http.get<SonarrConfig>(this.basePathService.buildApiUrl('/configuration/sonarr')).pipe(
      catchError((error) => {
        console.error("Error fetching Sonarr config:", error);
        return throwError(() => new Error("Failed to load Sonarr configuration"));
      })
    );
  }
  /**
   * Update Sonarr configuration (global settings only)
   */
  updateSonarrConfig(config: {failedImportMaxStrikes: number}): Observable<any> {
    return this.http.put<any>(this.basePathService.buildApiUrl('/configuration/sonarr'), config).pipe(
      catchError((error) => {
        console.error("Error updating Sonarr config:", error);
        return throwError(() => new Error(error.error?.error || "Failed to update Sonarr configuration"));
      })
    );
  }

  /**
   * Get Radarr configuration
   */
  getRadarrConfig(): Observable<RadarrConfig> {
    return this.http.get<RadarrConfig>(this.basePathService.buildApiUrl('/configuration/radarr')).pipe(
      catchError((error) => {
        console.error("Error fetching Radarr config:", error);
        return throwError(() => new Error("Failed to load Radarr configuration"));
      })
    );
  }
  /**
   * Update Radarr configuration
   */
  updateRadarrConfig(config: {failedImportMaxStrikes: number}): Observable<any> {
    return this.http.put<any>(this.basePathService.buildApiUrl('/configuration/radarr'), config).pipe(
      catchError((error) => {
        console.error("Error updating Radarr config:", error);
        return throwError(() => new Error(error.error?.error || "Failed to update Radarr configuration"));
      })
    );
  }

  /**
   * Get Lidarr configuration
   */
  getLidarrConfig(): Observable<LidarrConfig> {
    return this.http.get<LidarrConfig>(this.basePathService.buildApiUrl('/configuration/lidarr')).pipe(
      catchError((error) => {
        console.error("Error fetching Lidarr config:", error);
        return throwError(() => new Error("Failed to load Lidarr configuration"));
      })
    );
  }
  /**
   * Update Lidarr configuration
   */
  updateLidarrConfig(config: {failedImportMaxStrikes: number}): Observable<any> {
    return this.http.put<any>(this.basePathService.buildApiUrl('/configuration/lidarr'), config).pipe(
      catchError((error) => {
        console.error("Error updating Lidarr config:", error);
        return throwError(() => new Error(error.error?.error || "Failed to update Lidarr configuration"));
      })
    );
  }

  /**
   * Get Download Client configuration
   */
  getDownloadClientConfig(): Observable<DownloadClientConfig> {
    return this.http.get<DownloadClientConfig>(this.basePathService.buildApiUrl('/configuration/download_client')).pipe(
      catchError((error) => {
        console.error("Error fetching Download Client config:", error);
        return throwError(() => new Error("Failed to load Download Client configuration"));
      })
    );
  }
  
  /**
   * Update Download Client configuration
   */
  updateDownloadClientConfig(config: DownloadClientConfig): Observable<DownloadClientConfig> {
    return this.http.put<DownloadClientConfig>(this.basePathService.buildApiUrl('/configuration/download_client'), config).pipe(
      catchError((error) => {
        console.error("Error updating Download Client config:", error);
        return throwError(() => new Error(error.error?.error || "Failed to update Download Client configuration"));
      })
    );
  }
  
  /**
   * Create a new Download Client
   */
  createDownloadClient(client: CreateDownloadClientDto): Observable<ClientConfig> {
    return this.http.post<ClientConfig>(this.basePathService.buildApiUrl('/configuration/download_client'), client).pipe(
      catchError((error) => {
        console.error("Error creating Download Client:", error);
        return throwError(() => new Error(error.error?.error || "Failed to create Download Client"));
      })
    );
  }
  
  /**
   * Update a specific Download Client by ID
   */
  updateDownloadClient(id: string, client: ClientConfig): Observable<ClientConfig> {
    return this.http.put<ClientConfig>(this.basePathService.buildApiUrl(`/configuration/download_client/${id}`), client).pipe(
      catchError((error) => {
        console.error(`Error updating Download Client with ID ${id}:`, error);
        return throwError(() => new Error(error.error?.error || `Failed to update Download Client with ID ${id}`));
      })
    );
  }
  
  /**
   * Delete a Download Client by ID
   */
  deleteDownloadClient(id: string): Observable<void> {
    return this.http.delete<void>(this.basePathService.buildApiUrl(`/configuration/download_client/${id}`)).pipe(
      catchError((error) => {
        console.error(`Error deleting Download Client with ID ${id}:`, error);
        return throwError(() => new Error(error.error?.error || `Failed to delete Download Client with ID ${id}`));
      })
    );
  }

  // ===== SONARR INSTANCE MANAGEMENT =====

  /**
   * Create a new Sonarr instance
   */
  createSonarrInstance(instance: CreateArrInstanceDto): Observable<ArrInstance> {
    return this.http.post<ArrInstance>(this.basePathService.buildApiUrl('/configuration/sonarr/instances'), instance).pipe(
      catchError((error) => {
        console.error("Error creating Sonarr instance:", error);
        return throwError(() => new Error(error.error?.error || "Failed to create Sonarr instance"));
      })
    );
  }

  /**
   * Update a Sonarr instance by ID
   */
  updateSonarrInstance(id: string, instance: CreateArrInstanceDto): Observable<ArrInstance> {
    return this.http.put<ArrInstance>(this.basePathService.buildApiUrl(`/configuration/sonarr/instances/${id}`), instance).pipe(
      catchError((error) => {
        console.error(`Error updating Sonarr instance with ID ${id}:`, error);
        return throwError(() => new Error(error.error?.error || `Failed to update Sonarr instance with ID ${id}`));
      })
    );
  }

  /**
   * Delete a Sonarr instance by ID
   */
  deleteSonarrInstance(id: string): Observable<void> {
    return this.http.delete<void>(this.basePathService.buildApiUrl(`/configuration/sonarr/instances/${id}`)).pipe(
      catchError((error) => {
        console.error(`Error deleting Sonarr instance with ID ${id}:`, error);
        return throwError(() => new Error(error.error?.error || `Failed to delete Sonarr instance with ID ${id}`));
      })
    );
  }

  // ===== RADARR INSTANCE MANAGEMENT =====

  /**
   * Create a new Radarr instance
   */
  createRadarrInstance(instance: CreateArrInstanceDto): Observable<ArrInstance> {
    return this.http.post<ArrInstance>(this.basePathService.buildApiUrl('/configuration/radarr/instances'), instance).pipe(
      catchError((error) => {
        console.error("Error creating Radarr instance:", error);
        return throwError(() => new Error(error.error?.error || "Failed to create Radarr instance"));
      })
    );
  }

  /**
   * Update a Radarr instance by ID
   */
  updateRadarrInstance(id: string, instance: CreateArrInstanceDto): Observable<ArrInstance> {
    return this.http.put<ArrInstance>(this.basePathService.buildApiUrl(`/configuration/radarr/instances/${id}`), instance).pipe(
      catchError((error) => {
        console.error(`Error updating Radarr instance with ID ${id}:`, error);
        return throwError(() => new Error(error.error?.error || `Failed to update Radarr instance with ID ${id}`));
      })
    );
  }

  /**
   * Delete a Radarr instance by ID
   */
  deleteRadarrInstance(id: string): Observable<void> {
    return this.http.delete<void>(this.basePathService.buildApiUrl(`/configuration/radarr/instances/${id}`)).pipe(
      catchError((error) => {
        console.error(`Error deleting Radarr instance with ID ${id}:`, error);
        return throwError(() => new Error(error.error?.error || `Failed to delete Radarr instance with ID ${id}`));
      })
    );
  }

  // ===== LIDARR INSTANCE MANAGEMENT =====

  /**
   * Create a new Lidarr instance
   */
  createLidarrInstance(instance: CreateArrInstanceDto): Observable<ArrInstance> {
    return this.http.post<ArrInstance>(this.basePathService.buildApiUrl('/configuration/lidarr/instances'), instance).pipe(
      catchError((error) => {
        console.error("Error creating Lidarr instance:", error);
        return throwError(() => new Error(error.error?.error || "Failed to create Lidarr instance"));
      })
    );
  }

  /**
   * Update a Lidarr instance by ID
   */
  updateLidarrInstance(id: string, instance: CreateArrInstanceDto): Observable<ArrInstance> {
    return this.http.put<ArrInstance>(this.basePathService.buildApiUrl(`/configuration/lidarr/instances/${id}`), instance).pipe(
      catchError((error) => {
        console.error(`Error updating Lidarr instance with ID ${id}:`, error);
        return throwError(() => new Error(error.error?.error || `Failed to update Lidarr instance with ID ${id}`));
      })
    );
  }

  /**
   * Delete a Lidarr instance by ID
   */
  deleteLidarrInstance(id: string): Observable<void> {
    return this.http.delete<void>(this.basePathService.buildApiUrl(`/configuration/lidarr/instances/${id}`)).pipe(
      catchError((error) => {
        console.error(`Error deleting Lidarr instance with ID ${id}:`, error);
        return throwError(() => new Error(error.error?.error || `Failed to delete Lidarr instance with ID ${id}`));
      })
    );
  }
}

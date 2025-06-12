import { HttpClient } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { Observable, catchError, map, throwError } from "rxjs";
import { environment } from "../../../environments/environment";
import { JobSchedule, QueueCleanerConfig, ScheduleUnit } from "../../shared/models/queue-cleaner-config.model";
import { SonarrConfig } from "../../shared/models/sonarr-config.model";
import { RadarrConfig } from "../../shared/models/radarr-config.model";
import { LidarrConfig } from "../../shared/models/lidarr-config.model";

@Injectable({
  providedIn: "root",
})
export class ConfigurationService {
  private readonly apiUrl = environment.apiUrl;
  private readonly http = inject(HttpClient);

  /**
   * Get queue cleaner configuration
   */
  getQueueCleanerConfig(): Observable<QueueCleanerConfig> {
    return this.http.get<QueueCleanerConfig>(`${this.apiUrl}/api/configuration/queue_cleaner`).pipe(
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
    return this.http.put<QueueCleanerConfig>(`${this.apiUrl}/api/configuration/queue_cleaner`, config).pipe(
      catchError((error) => {
        console.error("Error updating queue cleaner config:", error);
        return throwError(() => new Error(error.error?.error || "Failed to update queue cleaner configuration"));
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
   * Get Sonarr configuration
   */
  getSonarrConfig(): Observable<SonarrConfig> {
    return this.http.get<SonarrConfig>(`${this.apiUrl}/api/configuration/sonarr`).pipe(
      catchError((error) => {
        console.error("Error fetching Sonarr config:", error);
        return throwError(() => new Error("Failed to load Sonarr configuration"));
      })
    );
  }
  /**
   * Update Sonarr configuration
   */
  updateSonarrConfig(config: SonarrConfig): Observable<SonarrConfig> {
    return this.http.put<SonarrConfig>(`${this.apiUrl}/api/configuration/sonarr`, config).pipe(
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
    return this.http.get<RadarrConfig>(`${this.apiUrl}/api/configuration/radarr`).pipe(
      catchError((error) => {
        console.error("Error fetching Radarr config:", error);
        return throwError(() => new Error("Failed to load Radarr configuration"));
      })
    );
  }
  /**
   * Update Radarr configuration
   */
  updateRadarrConfig(config: RadarrConfig): Observable<RadarrConfig> {
    return this.http.put<RadarrConfig>(`${this.apiUrl}/api/configuration/radarr`, config).pipe(
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
    return this.http.get<LidarrConfig>(`${this.apiUrl}/api/configuration/lidarr`).pipe(
      catchError((error) => {
        console.error("Error fetching Lidarr config:", error);
        return throwError(() => new Error("Failed to load Lidarr configuration"));
      })
    );
  }
  /**
   * Update Lidarr configuration
   */
  updateLidarrConfig(config: LidarrConfig): Observable<LidarrConfig> {
    return this.http.put<LidarrConfig>(`${this.apiUrl}/api/configuration/lidarr`, config).pipe(
      catchError((error) => {
        console.error("Error updating Lidarr config:", error);
        return throwError(() => new Error(error.error?.error || "Failed to update Lidarr configuration"));
      })
    );
  }
}

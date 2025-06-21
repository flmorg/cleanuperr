import { Injectable, inject, signal } from "@angular/core";
import { HttpClient, HttpErrorResponse } from "@angular/common/http";
import { finalize } from "rxjs/operators";
import { DownloadCleanerConfig, defaultDownloadCleanerConfig } from "../../shared/models/download-cleaner-config.model";
import { NotificationService } from "../../core/services/notification.service";
import { BasePathService } from "../../core/services/base-path.service";

@Injectable()
export class DownloadCleanerConfigStore {
  // Inject required services
  private http = inject(HttpClient);
  private notificationService = inject(NotificationService);
  private readonly basePathService = inject(BasePathService);

  // State signals
  private _loading = signal<boolean>(false);
  private _config = signal<DownloadCleanerConfig>(defaultDownloadCleanerConfig);
  private _saving = signal<boolean>(false);
  private _error = signal<string | null>(null);
  
  // Public computed signals
  readonly loading = this._loading.asReadonly();
  readonly config = this._config.asReadonly();
  readonly saving = this._saving.asReadonly();
  readonly error = this._error.asReadonly();

  constructor() {
    // Load config on initialization
    this.loadDownloadCleanerConfig();
  }

  /**
   * Load download cleaner configuration from the API
   */
  loadDownloadCleanerConfig(): void {
    // Reset error and set loading state
    this._error.set(null);
    this._loading.set(true);

    // API call to get download cleaner config
    this.http.get<DownloadCleanerConfig>(this.basePathService.buildApiUrl('/configuration/download_cleaner'))
      .pipe(
        // Always finalize to reset loading state
        finalize(() => this._loading.set(false))
      )
      .subscribe({
        next: (config) => {
          // Successfully loaded config
          this._config.set(config);
        },
        error: (error: HttpErrorResponse) => {
          // Handle error
          console.error('Error loading download cleaner config', error);
          this._error.set(`Failed to load download cleaner configuration: ${error.message}`);
        }
      });
  }

  /**
   * Generate a cron expression from a job schedule
   */
  generateCronExpression(schedule: { every: number; type: string }): string {
    if (!schedule) {
      return "0 0 * * * ?"; // Default: every hour
    }
    
    // Cron format: Seconds Minutes Hours Day-of-month Month Day-of-week Year
    switch (schedule.type) {
      case 'Seconds':
        return `0/${schedule.every} * * ? * * *`; // Every n seconds
      
      case 'Minutes':
        return `0 0/${schedule.every} * ? * * *`; // Every n minutes
      
      case 'Hours':
        return `0 0 0/${schedule.every} ? * * *`; // Every n hours
      
      default:
        return "0 0 * * * ?"; // Default: every hour
    }
  }

  /**
   * Parse a cron expression back to a job schedule
   */
  parseCronExpression(cronExpression: string): { every: number; type: string } | null {
    if (!cronExpression) {
      return null;
    }

    // Handle common patterns
    const patterns = [
      // Every n seconds: "0/n * * ? * * *"
      { regex: /^0\/(\d+) \* \* \? \* \* \*$/, type: 'Seconds' },
      // Every n minutes: "0 0/n * ? * * *"
      { regex: /^0 0\/(\d+) \* \? \* \* \*$/, type: 'Minutes' },
      // Every n hours: "0 0 0/n ? * * *"
      { regex: /^0 0 0\/(\d+) \? \* \* \*$/, type: 'Hours' },
    ];

    for (const pattern of patterns) {
      const match = cronExpression.match(pattern.regex);
      if (match) {
        const every = parseInt(match[1], 10);
        return { every, type: pattern.type };
      }
    }

    return null; // Couldn't parse, use advanced mode
  }

  /**
   * Save download cleaner configuration to the API
   * @param config The configuration to save
   * @returns Promise that resolves when save is complete
   */
  saveDownloadCleanerConfig(config: DownloadCleanerConfig): Promise<boolean> {
    // Set saving state and reset error
    this._saving.set(true);
    this._error.set(null);

    return new Promise<boolean>((resolve, reject) => {
      // API call to update download cleaner config
      this.http.put<any>(this.basePathService.buildApiUrl('/configuration/download_cleaner'), config)
        .pipe(
          // Always finalize to reset saving state
          finalize(() => this._saving.set(false))
        )
        .subscribe({
          next: (response) => {
            // Update local state with saved config
            this._config.set(config);
            // Resolve without showing notification, let the component handle that
            resolve(true);
          },
          error: (error: HttpErrorResponse) => {
            // Handle error
            console.error('Error saving download cleaner config', error);
            const errorMessage = error.error?.message || error.message || 'Unknown error';
            this._error.set(`Failed to save download cleaner configuration: ${errorMessage}`);
            // Let the component handle the error notification
            reject(error); // Pass the original error to preserve all details
          }
        });
    });
  }
}

import { Injectable, inject, signal } from "@angular/core";
import { HttpClient, HttpErrorResponse } from "@angular/common/http";
import { finalize } from "rxjs/operators";
import { CleanCategory, DownloadCleanerConfig, defaultDownloadCleanerConfig } from "../../shared/models/download-cleaner-config.model";
import { environment } from "../../../environments/environment";
import { NotificationService } from "../../core/services/notification.service";

@Injectable()
export class DownloadCleanerConfigStore {
  // Inject required services
  private http = inject(HttpClient);
  private notificationService = inject(NotificationService);

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

  // API endpoints
  private apiUrl = `${environment.apiUrl}/api/Configuration`;

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
    this.http.get<DownloadCleanerConfig>(`${this.apiUrl}/download_cleaner`)
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
   * Save download cleaner configuration to the API
   * @param config The configuration to save
   * @returns Promise that resolves when save is complete
   */
  saveDownloadCleanerConfig(config: DownloadCleanerConfig): Promise<boolean> {
    // Set saving state and reset error
    this._saving.set(true);
    this._error.set(null);

    return new Promise<boolean>((resolve) => {
      // API call to update download cleaner config
      this.http.put<any>(`${this.apiUrl}/download_cleaner`, config)
        .pipe(
          // Always finalize to reset saving state
          finalize(() => this._saving.set(false))
        )
        .subscribe({
          next: (response) => {
            // Update local state with saved config
            this._config.set(config);
            // Show success notification
            this.notificationService.showSuccess('Download cleaner configuration saved successfully');
            resolve(true);
          },
          error: (error: HttpErrorResponse) => {
            // Handle error
            console.error('Error saving download cleaner config', error);
            const errorMessage = error.error?.message || error.message || 'Unknown error';
            this._error.set(`Failed to save download cleaner configuration: ${errorMessage}`);
            // Show error notification
            this.notificationService.showError(`Failed to save: ${errorMessage}`);
            resolve(false);
          }
        });
    });
  }
}

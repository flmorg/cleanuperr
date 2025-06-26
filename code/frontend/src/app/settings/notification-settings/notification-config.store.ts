import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { catchError, finalize, of, tap } from 'rxjs';
import { NotificationsConfig } from '../../shared/models/notifications-config.model';
import { ApplicationPathService } from '../../core/services/base-path.service';

@Injectable()
export class NotificationConfigStore {
  // State signals
  private _config = signal<NotificationsConfig | null>(null);
  private _loading = signal<boolean>(false);
  private _saving = signal<boolean>(false);
  private _error = signal<string | null>(null);

  // Public selectors
  readonly config = this._config.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly saving = this._saving.asReadonly();
  readonly error = this._error.asReadonly();

  // Inject dependencies
  private readonly http = inject(HttpClient);
  private readonly ApplicationPathService = inject(ApplicationPathService);

  constructor() {
    // Load the configuration when the store is created
    this.loadConfig();
  }

  /**
   * Load notification configuration from the API
   */
  loadConfig(): void {
    if (this._loading()) return;

    this._loading.set(true);
    this._error.set(null);

    this.http.get<NotificationsConfig>(this.ApplicationPathService.buildApiUrl('/configuration/notifications'))
      .pipe(
        tap((config) => {
          this._config.set(config);
          this._error.set(null);
        }),
        catchError((error: HttpErrorResponse) => {
          console.error('Error loading notification configuration:', error);
          this._error.set(error.message || 'Failed to load notification configuration');
          return of(null);
        }),
        finalize(() => {
          this._loading.set(false);
        })
      )
      .subscribe();
  }

  /**
   * Save notification configuration to the API
   */
  saveConfig(config: NotificationsConfig): void {
    if (this._saving()) return;

    this._saving.set(true);
    this._error.set(null);

    this.http.put<any>(this.ApplicationPathService.buildApiUrl('/configuration/notifications'), config)
      .pipe(
        tap(() => {
          this._config.set(config);
          this._error.set(null);
        }),
        catchError((error: HttpErrorResponse) => {
          console.error('Error saving notification configuration:', error);
          this._error.set(error.message || 'Failed to save notification configuration');
          return of(null);
        }),
        finalize(() => {
          this._saving.set(false);
        })
      )
      .subscribe();
  }
} 

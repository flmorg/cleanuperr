import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { catchError, finalize, of, tap } from 'rxjs';
import { GeneralConfig } from '../../shared/models/general-config.model';
import { BasePathService } from '../../core/services/base-path.service';

@Injectable()
export class GeneralConfigStore {
  // State signals
  private _config = signal<GeneralConfig | null>(null);
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
  private readonly basePathService = inject(BasePathService);

  constructor() {
    // Load the configuration when the store is created
    this.loadConfig();
  }

  /**
   * Load general configuration from the API
   */
  loadConfig(): void {
    if (this._loading()) return;

    this._loading.set(true);
    this._error.set(null);

    this.http.get<GeneralConfig>(this.basePathService.buildApiUrl('/configuration/general'))
      .pipe(
        tap((config) => {
          this._config.set(config);
          this._error.set(null);
        }),
        catchError((error: HttpErrorResponse) => {
          console.error('Error loading general configuration:', error);
          this._error.set(error.message || 'Failed to load general configuration');
          return of(null);
        }),
        finalize(() => {
          this._loading.set(false);
        })
      )
      .subscribe();
  }

  /**
   * Save general configuration to the API
   */
  saveConfig(config: GeneralConfig): void {
    if (this._saving()) return;

    this._saving.set(true);
    this._error.set(null);

    this.http.put<any>(this.basePathService.buildApiUrl('/configuration/general'), config)
      .pipe(
        tap(() => {
          // Don't set config - let the form stay as-is with string enum values
          this._error.set(null);
        }),
        catchError((error: HttpErrorResponse) => {
          console.error('Error saving general configuration:', error);
          this._error.set(error.message || 'Failed to save general configuration');
          return of(null);
        }),
        finalize(() => {
          this._saving.set(false);
        })
      )
      .subscribe();
  }
}

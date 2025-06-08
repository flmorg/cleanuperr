import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { GeneralConfig } from '../../shared/models/general-config.model';
import { catchError, finalize, of, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable()
export class GeneralConfigStore {
  // API endpoints
  private readonly baseUrl = environment.apiUrl;
  
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

  // Inject HttpClient
  private http = inject(HttpClient);

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

    this.http.get<GeneralConfig>(`${this.baseUrl}/api/configuration/general`)
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

    this.http.put<any>(`${this.baseUrl}/api/configuration/general`, config)
      .pipe(
        tap(() => {
          this._config.set(config);
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

import { Injectable, inject } from "@angular/core";
import { patchState, signalStore, withHooks, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { HttpClient, HttpErrorResponse } from "@angular/common/http";
import { EMPTY, Observable, catchError, switchMap, tap } from 'rxjs';
import { DownloadCleanerConfig } from "../../shared/models/download-cleaner-config.model";
import { BasePathService } from "../../core/services/base-path.service";

export interface DownloadCleanerConfigState {
  config: DownloadCleanerConfig | null;
  loading: boolean;
  saving: boolean;
  error: string | null;
}

const initialState: DownloadCleanerConfigState = {
  config: null,
  loading: false,
  saving: false,
  error: null
};

@Injectable()
export class DownloadCleanerConfigStore extends signalStore(
  withState(initialState),
  withMethods((store, http = inject(HttpClient), basePathService = inject(BasePathService)) => ({
    
    /**
     * Load download cleaner configuration from the API
     */
    loadDownloadCleanerConfig: rxMethod<void>(
      pipe => pipe.pipe(
        tap(() => patchState(store, { loading: true, error: null })),
        switchMap(() => http.get<DownloadCleanerConfig>(basePathService.buildApiUrl('/configuration/download_cleaner')).pipe(
          tap({
            next: (config) => patchState(store, { config, loading: false }),
            error: (error: HttpErrorResponse) => {
              patchState(store, { 
                loading: false, 
                error: `Failed to load download cleaner configuration: ${error.message}` 
              });
            }
          }),
          catchError(() => EMPTY)
        ))
      )
    ),

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
    },

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
    },

    /**
     * Save download cleaner configuration to the API
     */
    saveDownloadCleanerConfig: rxMethod<DownloadCleanerConfig>(
      (config$: Observable<DownloadCleanerConfig>) => config$.pipe(
        tap(() => patchState(store, { saving: true, error: null })),
        switchMap(config => http.put<any>(basePathService.buildApiUrl('/configuration/download_cleaner'), config).pipe(
          tap({
            next: () => {
              // Successfully saved - just update saving state
              // Don't update config to avoid triggering form effects
              patchState(store, { 
                saving: false 
              });
            },
            error: (error: HttpErrorResponse) => {
              const errorMessage = error.error?.message || error.message || 'Unknown error';
              patchState(store, { 
                saving: false, 
                error: `Failed to save download cleaner configuration: ${errorMessage}` 
              });
            }
          }),
          catchError(() => EMPTY)
        ))
      )
    ),

    /**
     * Reset any errors
     */
    resetError() {
      patchState(store, { error: null });
    }
  })),
  withHooks({
    onInit({ loadDownloadCleanerConfig }) {
      loadDownloadCleanerConfig();
    }
  })
) {}

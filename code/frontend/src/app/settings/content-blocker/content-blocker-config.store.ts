import { Injectable, inject } from '@angular/core';
import { patchState, signalStore, withHooks, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { ContentBlockerConfig, JobSchedule, ScheduleUnit } from '../../shared/models/content-blocker-config.model';
import { ConfigurationService } from '../../core/services/configuration.service';
import { EMPTY, Observable, catchError, switchMap, tap } from 'rxjs';

export interface ContentBlockerConfigState {
  config: ContentBlockerConfig | null;
  loading: boolean;
  saving: boolean;
  error: string | null;
}

const initialState: ContentBlockerConfigState = {
  config: null,
  loading: false,
  saving: false,
  error: null
};

@Injectable()
export class ContentBlockerConfigStore extends signalStore(
  withState(initialState),
  withMethods((store, configService = inject(ConfigurationService)) => ({
    
    /**
     * Load the content blocker configuration
     */
    loadConfig: rxMethod<void>(
      pipe => pipe.pipe(
        tap(() => patchState(store, { loading: true, error: null })),
        switchMap(() => configService.getContentBlockerConfig().pipe(
          tap({
            next: (config) => patchState(store, { config, loading: false }),
            error: (error) => {
              patchState(store, { 
                loading: false, 
                error: error.message || 'Failed to load configuration' 
              });
            }
          }),
          catchError(() => EMPTY)
        ))
      )
    ),
    
    /**
     * Save the content blocker configuration
     */
    saveConfig: rxMethod<ContentBlockerConfig>(
      (config$: Observable<ContentBlockerConfig>) => config$.pipe(
        tap(() => patchState(store, { saving: true, error: null })),
        switchMap(config => configService.updateContentBlockerConfig(config).pipe(
          tap({
            next: () => {
              // Don't set config - let the form stay as-is with string enum values
              patchState(store, { 
                saving: false 
              });
            },
            error: (error) => {
              patchState(store, { 
                saving: false, 
                error: error.message || 'Failed to save configuration' 
              });
            }
          }),
          catchError(() => EMPTY)
        ))
      )
    ),
    
    /**
     * Update config in the store without saving to the backend
     */
    updateConfigLocally(config: Partial<ContentBlockerConfig>) {
      const currentConfig = store.config();
      if (currentConfig) {
        patchState(store, {
          config: { ...currentConfig, ...config }
        });
      }
    },
    
    /**
     * Reset any errors
     */
    resetError() {
      patchState(store, { error: null });
    },

    /**
     * Generate a cron expression from a job schedule
     */
    generateCronExpression(schedule: JobSchedule): string {
      if (!schedule) {
        return "0/5 * * * * ?"; // Default: every 5 seconds
      }
      
      // Cron format: Seconds Minutes Hours Day-of-month Month Day-of-week Year
      switch (schedule.type) {
        case ScheduleUnit.Seconds:
          return `0/${schedule.every} * * ? * * *`; // Every n seconds
        
        case ScheduleUnit.Minutes:
          return `0 0/${schedule.every} * ? * * *`; // Every n minutes
        
        case ScheduleUnit.Hours:
          return `0 0 0/${schedule.every} ? * * *`; // Every n hours
        
        default:
          return "0/5 * * * * ?"; // Default: every 5 seconds
      }
    }
  })),
  withHooks({
    onInit({ loadConfig }) {
      loadConfig();
    }
  })
) {} 
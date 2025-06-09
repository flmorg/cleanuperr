import { Injectable, inject } from '@angular/core';
import { patchState, signalStore, withHooks, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { QueueCleanerConfig, JobSchedule, ScheduleUnit } from '../../shared/models/queue-cleaner-config.model';
import { ConfigurationService } from '../../core/services/configuration.service';
import { EMPTY, Observable, catchError, switchMap, tap } from 'rxjs';
import { MessageService } from 'primeng/api';

export interface QueueCleanerConfigState {
  config: QueueCleanerConfig | null;
  loading: boolean;
  saving: boolean;
  error: string | null;
}

const initialState: QueueCleanerConfigState = {
  config: null,
  loading: false,
  saving: false,
  error: null
};

@Injectable()
export class QueueCleanerConfigStore extends signalStore(
  withState(initialState),
  withMethods((store, configService = inject(ConfigurationService), messageService = inject(MessageService)) => ({
    
    /**
     * Load the queue cleaner configuration
     */
    loadConfig: rxMethod<void>(
      pipe => pipe.pipe(
        tap(() => patchState(store, { loading: true, error: null })),
        switchMap(() => configService.getQueueCleanerConfig().pipe(
          tap({
            next: (config) => patchState(store, { config, loading: false }),
            error: (error) => {
              patchState(store, { 
                loading: false, 
                error: error.message || 'Failed to load configuration' 
              });
              messageService.add({
                severity: 'error',
                summary: 'Load Error',
                detail: error.message || 'Failed to load queue cleaner configuration',
                life: 5000
              });
            }
          }),
          catchError(() => EMPTY)
        ))
      )
    ),
    
    /**
     * Save the queue cleaner configuration
     */
    saveConfig: rxMethod<QueueCleanerConfig>(
      (config$: Observable<QueueCleanerConfig>) => config$.pipe(
        tap(() => patchState(store, { saving: true, error: null })),
        switchMap(config => configService.updateQueueCleanerConfig(config).pipe(
          tap({
            next: () => {
              patchState(store, { 
                config, 
                saving: false 
              });
              messageService.add({
                severity: 'success',
                summary: 'Success',
                detail: 'Queue cleaner configuration saved successfully',
                life: 3000
              });
            },
            error: (error) => {
              patchState(store, { 
                saving: false, 
                error: error.message || 'Failed to save configuration' 
              });
              messageService.add({
                severity: 'error',
                summary: 'Save Error',
                detail: error.message || 'Failed to save queue cleaner configuration',
                life: 5000
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
    updateConfigLocally(config: Partial<QueueCleanerConfig>) {
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
        return "0 0/5 * * * ?"; // Default: every 5 minutes
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
          return "0 0/5 * * * ?"; // Default: every 5 minutes
      }
    }
  })),
  withHooks({
    onInit({ loadConfig }) {
      loadConfig();
    }
  })
) {}

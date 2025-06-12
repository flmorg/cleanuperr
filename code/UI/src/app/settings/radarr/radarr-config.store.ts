import { Injectable, inject } from '@angular/core';
import { patchState, signalStore, withHooks, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { RadarrConfig } from '../../shared/models/radarr-config.model';
import { ConfigurationService } from '../../core/services/configuration.service';
import { EMPTY, Observable, catchError, switchMap, tap } from 'rxjs';

export interface RadarrConfigState {
  config: RadarrConfig | null;
  loading: boolean;
  saving: boolean;
  error: string | null;
}

const initialState: RadarrConfigState = {
  config: null,
  loading: false,
  saving: false,
  error: null
};

@Injectable()
export class RadarrConfigStore extends signalStore(
  withState(initialState),
  withMethods((store, configService = inject(ConfigurationService)) => ({
    
    /**
     * Load the Radarr configuration
     */
    loadConfig: rxMethod<void>(
      pipe => pipe.pipe(
        tap(() => patchState(store, { loading: true, error: null })),
        switchMap(() => configService.getRadarrConfig().pipe(
          tap({
            next: (config) => patchState(store, { config, loading: false }),
            error: (error) => {
              patchState(store, { 
                loading: false, 
                error: error.message || 'Failed to load Radarr configuration' 
              });
            }
          }),
          catchError(() => EMPTY)
        ))
      )
    ),
    
    /**
     * Save the Radarr configuration
     */
    saveConfig: rxMethod<RadarrConfig>(
      (config$: Observable<RadarrConfig>) => config$.pipe(
        tap(() => patchState(store, { saving: true, error: null })),
        switchMap(config => configService.updateRadarrConfig(config).pipe(
          tap({
            next: () => {
              patchState(store, { 
                config, 
                saving: false 
              });
            },
            error: (error) => {
              patchState(store, { 
                saving: false, 
                error: error.message || 'Failed to save Radarr configuration' 
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
    updateConfigLocally(config: Partial<RadarrConfig>) {
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
    }
  })),
  withHooks({
    onInit({ loadConfig }) {
      loadConfig();
    }
  })
) {}

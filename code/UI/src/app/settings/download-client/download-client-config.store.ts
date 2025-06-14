import { Injectable, inject } from '@angular/core';
import { patchState, signalStore, withHooks, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { ClientConfig, DownloadClientConfig } from '../../shared/models/download-client-config.model';
import { ConfigurationService } from '../../core/services/configuration.service';
import { EMPTY, Observable, catchError, switchMap, tap } from 'rxjs';

export interface DownloadClientConfigState {
  config: DownloadClientConfig | null;
  loading: boolean;
  saving: boolean;
  error: string | null;
}

const initialState: DownloadClientConfigState = {
  config: null,
  loading: false,
  saving: false,
  error: null
};

@Injectable()
export class DownloadClientConfigStore extends signalStore(
  withState(initialState),
  withMethods((store, configService = inject(ConfigurationService)) => ({
    
    /**
     * Load the Download Client configuration
     */
    loadConfig: rxMethod<void>(
      pipe => pipe.pipe(
        tap(() => patchState(store, { loading: true, error: null })),
        switchMap(() => configService.getDownloadClientConfig().pipe(
          tap({
            next: (config) => patchState(store, { config, loading: false }),
            error: (error) => {
              patchState(store, { 
                loading: false, 
                error: error.message || 'Failed to load Download Client configuration' 
              });
            }
          }),
          catchError(() => EMPTY)
        ))
      )
    ),
    
    /**
     * Save the Download Client configuration
     */
    saveConfig: rxMethod<DownloadClientConfig>(
      (config$: Observable<DownloadClientConfig>) => config$.pipe(
        tap(() => patchState(store, { saving: true, error: null })),
        switchMap(config => configService.updateDownloadClientConfig(config).pipe(
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
                error: error.message || 'Failed to save Download Client configuration' 
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
    updateConfigLocally(config: Partial<DownloadClientConfig>) {
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
     * Create a new download client
     */
    createClient: rxMethod<ClientConfig>(
      (client$: Observable<ClientConfig>) => client$.pipe(
        tap(() => patchState(store, { saving: true, error: null })),
        switchMap(client => configService.createDownloadClient(client).pipe(
          tap({
            next: (newClient) => {
              const currentConfig = store.config();
              if (currentConfig) {
                // Add the new client to the clients array
                const updatedClients = [...currentConfig.clients, newClient];
                
                patchState(store, { 
                  config: { clients: updatedClients },
                  saving: false 
                });
              }
            },
            error: (error) => {
              patchState(store, { 
                saving: false, 
                error: error.message || 'Failed to create Download Client' 
              });
            }
          }),
          catchError(() => EMPTY)
        ))
      )
    ),
    
    /**
     * Update a specific download client by ID
     */
    updateClient: rxMethod<{ id: string, client: ClientConfig }>(
      (params$: Observable<{ id: string, client: ClientConfig }>) => params$.pipe(
        tap(() => patchState(store, { saving: true, error: null })),
        switchMap(({ id, client }) => configService.updateDownloadClient(id, client).pipe(
          tap({
            next: (updatedClient) => {
              const currentConfig = store.config();
              if (currentConfig) {
                // Find and replace the updated client in the clients array
                const updatedClients = currentConfig.clients.map((c: ClientConfig) => 
                  c.id === id ? updatedClient : c
                );
                
                patchState(store, { 
                  config: { clients: updatedClients },
                  saving: false 
                });
              }
            },
            error: (error) => {
              patchState(store, { 
                saving: false, 
                error: error.message || `Failed to update Download Client with ID ${id}` 
              });
            }
          }),
          catchError(() => EMPTY)
        ))
      )
    ),
    
    /**
     * Delete a download client by ID
     */
    deleteClient: rxMethod<string>(
      (id$: Observable<string>) => id$.pipe(
        tap(() => patchState(store, { saving: true, error: null })),
        switchMap(id => configService.deleteDownloadClient(id).pipe(
          tap({
            next: () => {
              const currentConfig = store.config();
              if (currentConfig) {
                // Remove the client from the clients array
                const updatedClients = currentConfig.clients.filter((c: ClientConfig) => c.id !== id);
                
                patchState(store, { 
                  config: { clients: updatedClients },
                  saving: false 
                });
              }
            },
            error: (error) => {
              patchState(store, { 
                saving: false, 
                error: error.message || `Failed to delete Download Client with ID ${id}` 
              });
            }
          }),
          catchError(() => EMPTY)
        ))
      )
    )
  })),
  withHooks({
    onInit({ loadConfig }) {
      loadConfig();
    }
  })
) {}

import { ApplicationConfig, provideZoneChangeDetection, isDevMode } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { providePrimeNG } from 'primeng/config';
import { ConfirmationService } from 'primeng/api';

import { routes } from './app.routes';
import { provideServiceWorker } from '@angular/service-worker';
import Noir from './app.preset';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }), 
    provideRouter(routes), 
    provideHttpClient(withInterceptorsFromDi()),
    { provide: ConfirmationService },
    provideAnimationsAsync(),
    providePrimeNG({
      theme: {
        preset: Noir
      }
    }),
    provideServiceWorker('ngsw-worker.js', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerWhenStable:30000'
    })
  ]
};

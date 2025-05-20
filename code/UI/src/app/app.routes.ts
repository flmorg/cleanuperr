import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'logs', pathMatch: 'full' },
  { path: 'logs', loadComponent: () => import('./logging/logs-viewer/logs-viewer.component').then(m => m.LogsViewerComponent) }
];

import { Routes } from '@angular/router';
import { pendingChangesGuard } from './core/guards/pending-changes.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', loadComponent: () => import('./dashboard/dashboard-page/dashboard-page.component').then(m => m.DashboardPageComponent) },
  { path: 'logs', loadComponent: () => import('./logging/logs-viewer/logs-viewer.component').then(m => m.LogsViewerComponent) },
  { path: 'events', loadComponent: () => import('./events/events-viewer/events-viewer.component').then(m => m.EventsViewerComponent) },
  { 
    path: 'settings', 
    loadComponent: () => import('./settings/settings-page/settings-page.component').then(m => m.SettingsPageComponent),
    canDeactivate: [pendingChangesGuard] 
  }
];

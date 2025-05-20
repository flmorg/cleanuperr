import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', loadComponent: () => import('./dashboard/dashboard-page/dashboard-page.component').then(m => m.DashboardPageComponent) },
  { path: 'logs', loadComponent: () => import('./logging/logs-viewer/logs-viewer.component').then(m => m.LogsViewerComponent) },
  { path: 'settings', loadComponent: () => import('./settings/settings-page/settings-page.component').then(m => m.SettingsPageComponent) }
];

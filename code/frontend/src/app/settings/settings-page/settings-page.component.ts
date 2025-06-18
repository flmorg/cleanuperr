import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { environment } from '../../../environments/environment';
import { CanComponentDeactivate } from '../../core/guards';

// PrimeNG Components
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { ButtonModule } from 'primeng/button';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ConfirmDialogModule } from 'primeng/confirmdialog';

// Custom Components and Services
import { QueueCleanerSettingsComponent } from '../queue-cleaner/queue-cleaner-settings.component';
import { GeneralSettingsComponent } from '../general-settings/general-settings.component';
import { DownloadCleanerSettingsComponent } from '../download-cleaner/download-cleaner-settings.component';
import { SonarrSettingsComponent } from '../sonarr/sonarr-settings.component';
import { NotificationSettingsComponent } from "../notification-settings/notification-settings.component";

// Define interfaces for settings page
interface LogLevel {
  name: string;
  value: string;
}

interface Category {
  name: string;
  code: string;
}

@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    CardModule,
    ButtonModule,
    ToastModule,
    ConfirmDialogModule,
    QueueCleanerSettingsComponent,
    GeneralSettingsComponent,
    DownloadCleanerSettingsComponent,
    NotificationSettingsComponent
],
  providers: [MessageService, ConfirmationService],
  templateUrl: './settings-page.component.html',
  styleUrl: './settings-page.component.scss'
})
export class SettingsPageComponent implements OnInit, CanComponentDeactivate {
  logLevels: LogLevel[] = [
    { name: 'Trace', value: 'trace' },
    { name: 'Debug', value: 'debug' },
    { name: 'Information', value: 'information' },
    { name: 'Warning', value: 'warning' },
    { name: 'Error', value: 'error' },
    { name: 'Critical', value: 'critical' },
    { name: 'None', value: 'none' }
  ];

  categories: Category[] = [
    { name: 'All', code: '*' },
    { name: 'System', code: 'SYS' },
    { name: 'Commands', code: 'CMD' },
    { name: 'Database', code: 'DB' },
    { name: 'Network', code: 'NET' },
    { name: 'Jobs', code: 'JOBS' },
    { name: 'Imports', code: 'IMPORTS' },
    { name: 'Media', code: 'MEDIA' }
  ];
  
  // API URLs from environment
  private apiUrl = environment.apiUrl;
  
  // Services
  private messageService = inject(MessageService);
  
  // Reference to the settings components
  @ViewChild(QueueCleanerSettingsComponent) queueCleanerSettings!: QueueCleanerSettingsComponent;
  @ViewChild(GeneralSettingsComponent) generalSettings!: GeneralSettingsComponent;
  @ViewChild(DownloadCleanerSettingsComponent) downloadCleanerSettings!: DownloadCleanerSettingsComponent;
  @ViewChild(SonarrSettingsComponent) sonarrSettings!: SonarrSettingsComponent;
  @ViewChild(NotificationSettingsComponent) notificationSettings!: NotificationSettingsComponent;

  ngOnInit(): void {
    // Future implementation for other settings sections
  }
  
  /**
   * Implements CanComponentDeactivate interface
   * Check if any settings components have unsaved changes
   */
  canDeactivate(): boolean {
    // Check if queue cleaner settings has unsaved changes
    if (this.queueCleanerSettings?.canDeactivate() === false) {
      return false;
    }
    
    // Check if general settings has unsaved changes
    if (this.generalSettings?.canDeactivate() === false) {
      return false;
    }
    
    // Check if download cleaner settings has unsaved changes
    if (this.downloadCleanerSettings?.canDeactivate() === false) {
      return false;
    }
    
    // Check if sonarr settings has unsaved changes
    if (this.sonarrSettings?.canDeactivate() === false) {
      return false;
    }
    
    // Check if notification settings has unsaved changes
    if (this.notificationSettings?.canDeactivate() === false) {
      return false;
    }
    
    return true;
  }
}

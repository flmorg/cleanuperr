import { Component, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { PanelModule } from 'primeng/panel';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { CanComponentDeactivate } from '../../core/guards';

// PrimeNG Components
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ConfirmDialogModule } from 'primeng/confirmdialog';

// Custom Components and Services
import { QueueCleanerSettingsComponent } from '../queue-cleaner/queue-cleaner-settings.component';
import { GeneralSettingsComponent } from '../general-settings/general-settings.component';
import { DownloadCleanerSettingsComponent } from '../download-cleaner/download-cleaner-settings.component';
import { SonarrSettingsComponent } from '../sonarr/sonarr-settings.component';
import { ContentBlockerSettingsComponent } from "../content-blocker/content-blocker-settings.component";

@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    PanelModule,
    ButtonModule,
    DropdownModule,
    CardModule,
    ToastModule,
    ConfirmDialogModule,
    QueueCleanerSettingsComponent,
    GeneralSettingsComponent,
    DownloadCleanerSettingsComponent,
    ContentBlockerSettingsComponent
],
  providers: [MessageService, ConfirmationService],
  templateUrl: './settings-page.component.html',
  styleUrl: './settings-page.component.scss'
})
export class SettingsPageComponent implements CanComponentDeactivate {
  // Reference to the settings components
  @ViewChild(QueueCleanerSettingsComponent) queueCleanerSettings!: QueueCleanerSettingsComponent;
  @ViewChild(GeneralSettingsComponent) generalSettings!: GeneralSettingsComponent;
  @ViewChild(DownloadCleanerSettingsComponent) downloadCleanerSettings!: DownloadCleanerSettingsComponent;
  @ViewChild(SonarrSettingsComponent) sonarrSettings!: SonarrSettingsComponent;

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
    
    return true;
  }
}

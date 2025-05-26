import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { environment } from '../../../environments/environment';

// PrimeNG Components
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { InputSwitchModule } from 'primeng/inputswitch';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { SliderModule } from 'primeng/slider';
import { RadioButtonModule } from 'primeng/radiobutton';
import { InputNumberModule } from 'primeng/inputnumber';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

// Define interfaces for our settings
interface LogLevel {
  label: string;
  value: string;
}

interface MaxLogOption {
  label: string;
  value: number;
}

@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CardModule,
    InputTextModule,
    InputSwitchModule,
    ButtonModule,
    DropdownModule,
    SliderModule,
    RadioButtonModule,
    InputNumberModule,
    TagModule,
    ToastModule
  ],
  providers: [MessageService],
  templateUrl: './settings-page.component.html',
  styleUrl: './settings-page.component.scss'
})
export class SettingsPageComponent implements OnInit {
  // API Settings
  apiUrl = environment.apiUrl;
  apiKey = '';
  apiTimeout = 30;
  
  // UI Settings
  theme = 'light';
  fontSize = 14;
  
  // Logging Settings
  enableLogs = true;
  logLevel: LogLevel = { label: 'Information', value: 'Information' };
  enableNotifications = true;
  
  // Log Viewer Settings
  autoRefresh = false;
  refreshInterval = 30;
  maxLogEntries: number = 100;
  
  // Available options for select lists
  logLevels: LogLevel[] = [
    { label: 'Debug', value: 'Debug' },
    { label: 'Information', value: 'Information' },
    { label: 'Warning', value: 'Warning' },
    { label: 'Error', value: 'Error' },
    { label: 'Critical', value: 'Critical' }
  ];
  
  maxLogOptions: MaxLogOption[] = [
    { label: '50 entries', value: 50 },
    { label: '100 entries', value: 100 },
    { label: '250 entries', value: 250 },
    { label: '500 entries', value: 500 },
    { label: '1000 entries', value: 1000 }
  ];
  
  // UI state
  showSaveNotification = false;
  
  constructor(private messageService: MessageService) {}
  
  ngOnInit() {
    // Load saved settings if available
    this.loadSettings();
  }
  
  getSeverity(level: string): string {
    const normalizedLevel = level?.toLowerCase() || '';
    
    switch (normalizedLevel) {
      case 'error':
      case 'critical':
      case 'fatal':
        return 'danger';
      case 'warning':
        return 'warning';
      case 'information':
      case 'info':
        return 'info';
      case 'debug':
      case 'trace':
        return 'success';
      default:
        return 'info';
    }
  }
  
  saveSettings() {
    // Here we would normally save the settings to local storage or a backend API
    console.log('Saving settings:', {
      apiUrl: this.apiUrl,
      apiKey: this.apiKey ? '******' : null, // Don't log actual API key for security
      apiTimeout: this.apiTimeout,
      theme: this.theme,
      fontSize: this.fontSize,
      enableLogs: this.enableLogs,
      logLevel: this.logLevel,
      enableNotifications: this.enableNotifications,
      autoRefresh: this.autoRefresh,
      refreshInterval: this.refreshInterval,
      maxLogEntries: this.maxLogEntries
    });
    
    // Show success message
    this.messageService.add({
      key: 'settings',
      severity: 'success',
      summary: 'Settings Saved',
      detail: 'Your settings have been successfully saved.',
      life: 3000
    });
    
    this.showSaveNotification = true;
    setTimeout(() => {
      this.showSaveNotification = false;
    }, 3000);
  }
  
  resetToDefaults() {
    // Reset to default values
    this.apiUrl = environment.apiUrl;
    this.apiKey = '';
    this.apiTimeout = 30;
    this.theme = 'light';
    this.fontSize = 14;
    this.enableLogs = true;
    this.logLevel = { label: 'Information', value: 'Information' };
    this.enableNotifications = true;
    this.autoRefresh = false;
    this.refreshInterval = 30;
    this.maxLogEntries = 100;
    
    // Show info message
    this.messageService.add({
      key: 'settings',
      severity: 'info',
      summary: 'Settings Reset',
      detail: 'All settings have been reset to their default values.',
      life: 3000
    });
    
    this.showSaveNotification = true;
    setTimeout(() => {
      this.showSaveNotification = false;
    }, 3000);
  }
  
  private loadSettings() {
    // In a real application, we would load settings from local storage or an API
    // For now, we'll just use the default values set in the class
    console.log('Loading settings from storage...');
  }

  resetApiUrl(): void {
    this.apiUrl = environment.apiUrl;
  }
}

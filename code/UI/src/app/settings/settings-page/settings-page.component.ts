import { Component, OnInit, computed, inject, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { environment } from '../../../environments/environment';

// PrimeNG Components
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { DropdownModule } from 'primeng/dropdown';
import { PasswordModule } from 'primeng/password';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { CheckboxModule } from 'primeng/checkbox';
import { AccordionModule } from 'primeng/accordion';
import { SelectButtonModule } from 'primeng/selectbutton';
import { ChipsModule } from 'primeng/chips';
import { TagModule } from 'primeng/tag';
import { SliderModule } from 'primeng/slider';
import { RadioButtonModule } from 'primeng/radiobutton';
import { DividerModule } from 'primeng/divider';
import { InputGroupModule } from 'primeng/inputgroup';
import { InputGroupAddonModule } from 'primeng/inputgroupaddon';

// Custom Components and Services
import { ByteSizeInputComponent } from '../../shared/components/byte-size-input/byte-size-input.component';
import { QueueCleanerConfigStore } from '../store/queue-cleaner-config.store';
import { QueueCleanerConfig, ScheduleUnit } from '../../shared/models/queue-cleaner-config.model';
import { toSignal } from '@angular/core/rxjs-interop';

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
    ReactiveFormsModule,
    CardModule,
    InputTextModule,
    CheckboxModule,
    ButtonModule,
    InputNumberModule,
    DropdownModule,
    PasswordModule,
    ToastModule,
    TagModule,
    SliderModule,
    RadioButtonModule,
    AccordionModule,
    DividerModule,
    SelectButtonModule,
    ChipsModule,
    ByteSizeInputComponent
  ],
  providers: [MessageService, QueueCleanerConfigStore],
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

  // Schedule unit options for job schedules
  scheduleUnitOptions = [
    { label: 'Seconds', value: ScheduleUnit.Seconds },
    { label: 'Minutes', value: ScheduleUnit.Minutes },
    { label: 'Hours', value: ScheduleUnit.Hours }
  ];
  
  // UI state
  showSaveNotification = false;

  // Queue Cleaner Configuration Form
  queueCleanerForm: FormGroup;
  
  // Inject the necessary services
  private formBuilder = inject(FormBuilder);
  private messageService = inject(MessageService);
  private queueCleanerStore = inject(QueueCleanerConfigStore);
  
  // Signals from the store
  readonly queueCleanerConfig = this.queueCleanerStore.config;
  readonly queueCleanerLoading = this.queueCleanerStore.loading;
  readonly queueCleanerSaving = this.queueCleanerStore.saving;
  readonly queueCleanerError = this.queueCleanerStore.error;
  
  constructor() {
    // Initialize the queue cleaner form
    this.queueCleanerForm = this.formBuilder.group({
      enabled: [false],
      jobSchedule: this.formBuilder.group({
        every: [5, [Validators.required, Validators.min(1)]],
        type: [ScheduleUnit.Minutes]
      }),
      runSequentially: [false],
      ignoredDownloadsPath: [''],
      
      // Failed Import settings
      failedImportMaxStrikes: [0, [Validators.min(0)]],
      failedImportIgnorePrivate: [false],
      failedImportDeletePrivate: [false],
      failedImportIgnorePatterns: [[]],
      
      // Stalled settings
      stalledMaxStrikes: [0, [Validators.min(0)]],
      stalledResetStrikesOnProgress: [false],
      stalledIgnorePrivate: [false],
      stalledDeletePrivate: [false],
      
      // Downloading Metadata settings
      downloadingMetadataMaxStrikes: [0, [Validators.min(0)]],
      
      // Slow Download settings
      slowMaxStrikes: [0, [Validators.min(0)]],
      slowResetStrikesOnProgress: [false],
      slowIgnorePrivate: [false],
      slowDeletePrivate: [false],
      slowMinSpeed: [''],
      slowMaxTime: [0, [Validators.min(0)]],
      slowIgnoreAboveSize: ['']
    });
  }
  
  ngOnInit() {
    // Load saved settings if available
    this.loadSettings();
    
    // The QueueCleanerConfigStore automatically loads the configuration when initialized
    // Create an effect to update the form when the configuration changes
    effect(() => {
      const config = this.queueCleanerConfig();
      if (config) {
        // Update the form with the current configuration
        this.queueCleanerForm.patchValue({
          enabled: config.enabled,
          runSequentially: config.runSequentially,
          ignoredDownloadsPath: config.ignoredDownloadsPath,
          
          // Failed Import settings
          failedImportMaxStrikes: config.failedImportMaxStrikes,
          failedImportIgnorePrivate: config.failedImportIgnorePrivate,
          failedImportDeletePrivate: config.failedImportDeletePrivate,
          failedImportIgnorePatterns: config.failedImportIgnorePatterns,
          
          // Stalled settings
          stalledMaxStrikes: config.stalledMaxStrikes,
          stalledResetStrikesOnProgress: config.stalledResetStrikesOnProgress,
          stalledIgnorePrivate: config.stalledIgnorePrivate,
          stalledDeletePrivate: config.stalledDeletePrivate,
          
          // Downloading Metadata settings
          downloadingMetadataMaxStrikes: config.downloadingMetadataMaxStrikes,
          
          // Slow Download settings
          slowMaxStrikes: config.slowMaxStrikes,
          slowResetStrikesOnProgress: config.slowResetStrikesOnProgress,
          slowIgnorePrivate: config.slowIgnorePrivate,
          slowDeletePrivate: config.slowDeletePrivate,
          slowMinSpeed: config.slowMinSpeed,
          slowMaxTime: config.slowMaxTime,
          slowIgnoreAboveSize: config.slowIgnoreAboveSize
        });
        
        // Update job schedule if it exists
        if (config.jobSchedule) {
          this.queueCleanerForm.get('jobSchedule')?.patchValue({
            every: config.jobSchedule.every,
            type: config.jobSchedule.type
          });
        }
      }
    });
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
  
  /**
   * Save the queue cleaner configuration
   */
  saveQueueCleanerConfig(): void {
    if (this.queueCleanerForm.invalid) {
      // Mark all fields as touched to show validation errors
      this.markFormGroupTouched(this.queueCleanerForm);
      this.messageService.add({
        severity: 'error',
        summary: 'Validation Error',
        detail: 'Please correct the errors in the form before saving.',
        life: 5000
      });
      return;
    }
    
    // Get the form values
    const formValues = this.queueCleanerForm.value;
    
    // Build the configuration object
    const config: QueueCleanerConfig = {
      enabled: formValues.enabled,
      // The cronExpression will be generated from the jobSchedule when saving
      cronExpression: '',
      jobSchedule: formValues.jobSchedule,
      runSequentially: formValues.runSequentially,
      ignoredDownloadsPath: formValues.ignoredDownloadsPath || '',
      
      // Failed Import settings
      failedImportMaxStrikes: formValues.failedImportMaxStrikes,
      failedImportIgnorePrivate: formValues.failedImportIgnorePrivate,
      failedImportDeletePrivate: formValues.failedImportDeletePrivate,
      failedImportIgnorePatterns: formValues.failedImportIgnorePatterns || [],
      
      // Stalled settings
      stalledMaxStrikes: formValues.stalledMaxStrikes,
      stalledResetStrikesOnProgress: formValues.stalledResetStrikesOnProgress,
      stalledIgnorePrivate: formValues.stalledIgnorePrivate,
      stalledDeletePrivate: formValues.stalledDeletePrivate,
      
      // Downloading Metadata settings
      downloadingMetadataMaxStrikes: formValues.downloadingMetadataMaxStrikes,
      
      // Slow Download settings
      slowMaxStrikes: formValues.slowMaxStrikes,
      slowResetStrikesOnProgress: formValues.slowResetStrikesOnProgress,
      slowIgnorePrivate: formValues.slowIgnorePrivate,
      slowDeletePrivate: formValues.slowDeletePrivate,
      slowMinSpeed: formValues.slowMinSpeed || '',
      slowMaxTime: formValues.slowMaxTime,
      slowIgnoreAboveSize: formValues.slowIgnoreAboveSize || ''
    };
    
    // Save the configuration
    this.queueCleanerStore.saveConfig(config);
  }
  
  /**
   * Reset the queue cleaner configuration form to default values
   */
  resetQueueCleanerConfig(): void {
    this.queueCleanerForm.reset({
      enabled: false,
      jobSchedule: {
        every: 5,
        type: ScheduleUnit.Minutes
      },
      runSequentially: false,
      ignoredDownloadsPath: '',
      
      // Failed Import settings
      failedImportMaxStrikes: 0,
      failedImportIgnorePrivate: false,
      failedImportDeletePrivate: false,
      failedImportIgnorePatterns: [],
      
      // Stalled settings
      stalledMaxStrikes: 0,
      stalledResetStrikesOnProgress: false,
      stalledIgnorePrivate: false,
      stalledDeletePrivate: false,
      
      // Downloading Metadata settings
      downloadingMetadataMaxStrikes: 0,
      
      // Slow Download settings
      slowMaxStrikes: 0,
      slowResetStrikesOnProgress: false,
      slowIgnorePrivate: false,
      slowDeletePrivate: false,
      slowMinSpeed: '',
      slowMaxTime: 0,
      slowIgnoreAboveSize: ''
    });
  }
  
  /**
   * Mark all controls in a form group as touched
   */
  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.values(formGroup.controls).forEach(control => {
      control.markAsTouched();
      
      if ((control as any).controls) {
        this.markFormGroupTouched(control as FormGroup);
      }
    });
  }
  
  /**
   * Check if a form control has an error after it's been touched
   */
  hasError(controlName: string, errorName: string): boolean {
    const control = this.queueCleanerForm.get(controlName);
    return control ? control.touched && control.hasError(errorName) : false;
  }
  
  /**
   * Get nested form control errors
   */
  hasNestedError(parentName: string, controlName: string, errorName: string): boolean {
    const parentControl = this.queueCleanerForm.get(parentName);
    if (!parentControl || !(parentControl instanceof FormGroup)) {
      return false;
    }
    
    const control = parentControl.get(controlName);
    return control ? control.touched && control.hasError(errorName) : false;
  }
}

import { Component, EventEmitter, OnInit, Output, effect, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { QueueCleanerConfigStore } from './queue-cleaner-config.store';
import { QueueCleanerConfig, ScheduleUnit } from '../../shared/models/queue-cleaner-config.model';
import { SettingsCardComponent } from '../components/settings-card/settings-card.component';
import { ByteSizeInputComponent } from '../../shared/components/byte-size-input/byte-size-input.component';

// PrimeNG Components
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { CheckboxModule } from 'primeng/checkbox';
import { ButtonModule } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { AccordionModule } from 'primeng/accordion';
import { SelectButtonModule } from 'primeng/selectbutton';
import { ChipsModule } from 'primeng/chips';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-queue-cleaner-settings',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    SettingsCardComponent,
    CardModule,
    InputTextModule,
    CheckboxModule,
    ButtonModule,
    InputNumberModule,
    AccordionModule,
    SelectButtonModule,
    ChipsModule,
    ToastModule,
    ByteSizeInputComponent
  ],
  providers: [QueueCleanerConfigStore, MessageService],
  templateUrl: './queue-cleaner-settings.component.html',
  styleUrls: ['./queue-cleaner-settings.component.scss']
})
export class QueueCleanerSettingsComponent implements OnInit {
  @Output() saved = new EventEmitter<void>();
  @Output() error = new EventEmitter<string>();

  // Queue Cleaner Configuration Form
  queueCleanerForm: FormGroup;
  
  // Schedule unit options for job schedules
  scheduleUnitOptions = [
    { label: 'Seconds', value: ScheduleUnit.Seconds },
    { label: 'Minutes', value: ScheduleUnit.Minutes },
    { label: 'Hours', value: ScheduleUnit.Hours }
  ];
  
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

    // Effect to emit events when save operation completes or errors
    effect(() => {
      const error = this.queueCleanerError();
      if (error) {
        this.error.emit(error);
      }
    });

    effect(() => {
      const saving = this.queueCleanerSaving();
      if (saving === false) {
        // This will run after a save operation is completed (whether successful or not)
        // We check if there's no error to determine if it was successful
        if (!this.queueCleanerError()) {
          this.saved.emit();
        }
      }
    });
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

import { Component, EventEmitter, OnDestroy, Output, inject, effect } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { Subject, takeUntil } from "rxjs";
import { DownloadCleanerConfigStore } from "./download-cleaner-config.store";
import { CanComponentDeactivate } from "../../core/guards";
import {
  CleanCategory,
  DownloadCleanerConfig,
  JobSchedule,
  createDefaultCategory
} from "../../shared/models/download-cleaner-config.model";
import { ScheduleUnit, ScheduleOptions } from "../../shared/models/queue-cleaner-config.model";

// PrimeNG Components
import { CardModule } from "primeng/card";
import { InputTextModule } from "primeng/inputtext";
import { CheckboxModule } from "primeng/checkbox";
import { ButtonModule } from "primeng/button";
import { InputNumberModule } from "primeng/inputnumber";
import { AccordionModule } from "primeng/accordion";
import { SelectButtonModule } from "primeng/selectbutton";
import { ChipsModule } from "primeng/chips";
import { ToastModule } from "primeng/toast";
import { NotificationService } from "../../core/services/notification.service";
import { SelectModule } from "primeng/select";
import { AutoCompleteModule } from "primeng/autocomplete";
import { DropdownModule } from "primeng/dropdown";
import { TableModule } from "primeng/table";
import { LoadingErrorStateComponent } from "../../shared/components/loading-error-state/loading-error-state.component";

@Component({
  selector: "app-download-cleaner-settings",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CardModule,
    InputTextModule,
    CheckboxModule,
    ButtonModule,
    InputNumberModule,
    AccordionModule,
    SelectButtonModule,
    ChipsModule,
    ToastModule,
    SelectModule,
    AutoCompleteModule,
    DropdownModule,
    TableModule,
    LoadingErrorStateComponent,
  ],
  providers: [DownloadCleanerConfigStore],
  templateUrl: "./download-cleaner-settings.component.html",
  styleUrls: ["./download-cleaner-settings.component.scss"],
})
export class DownloadCleanerSettingsComponent implements OnDestroy, CanComponentDeactivate {
  @Output() error = new EventEmitter<string>();
  @Output() saved = new EventEmitter<void>();

  // Services
  private formBuilder = inject(FormBuilder);
  private notificationService = inject(NotificationService);
  private downloadCleanerStore = inject(DownloadCleanerConfigStore);
  
  // Configuration signals
  readonly downloadCleanerConfig = this.downloadCleanerStore.config;
  readonly downloadCleanerLoading = this.downloadCleanerStore.loading;
  readonly downloadCleanerSaving = this.downloadCleanerStore.saving;
  readonly downloadCleanerError = this.downloadCleanerStore.error;
  
  // Form and state
  downloadCleanerForm!: FormGroup;
  originalFormValues: any;
  private destroy$ = new Subject<void>();
  hasActualChanges = false; // Flag to track actual form changes
  
  // Get the categories form array for easier access in the template
  get categoriesFormArray(): FormArray {
    return this.downloadCleanerForm.get('categories') as FormArray;
  }
  
  // Schedule options
  scheduleUnitOptions = [
    { label: 'Seconds', value: ScheduleUnit.Seconds },
    { label: 'Minutes', value: ScheduleUnit.Minutes },
    { label: 'Hours', value: ScheduleUnit.Hours },
  ];

  scheduleValueOptions: Record<ScheduleUnit, {label: string, value: number}[]> = {
    [ScheduleUnit.Seconds]: [
      { label: '15s', value: 15 },
      { label: '30s', value: 30 },
      { label: '45s', value: 45 }
    ],
    [ScheduleUnit.Minutes]: [
      { label: '1m', value: 1 },
      { label: '5m', value: 5 },
      { label: '15m', value: 15 },
      { label: '30m', value: 30 },
      { label: '45m', value: 45 }
    ],
    [ScheduleUnit.Hours]: [
      { label: '1h', value: 1 },
      { label: '3h', value: 3 },
      { label: '6h', value: 6 },
      { label: '12h', value: 12 }
    ]
  };

  // Display modes for schedule
  scheduleModeOptions = [
    { label: 'Basic', value: false },
    { label: 'Advanced', value: true }
  ];

  /**
   * Check if component can be deactivated (navigation guard)
   */
  canDeactivate(): boolean {
    // Allow navigation if form is not dirty or has been saved
    return !this.downloadCleanerForm?.dirty || !this.formValuesChanged();
  }

  constructor() {
    // Initialize the form with proper disabled states for dependent controls
    this.downloadCleanerForm = this.formBuilder.group({
      enabled: [false],
      useAdvancedScheduling: [{ value: false, disabled: true }],
      cronExpression: [{ value: "0 0 * * * ?", disabled: true }, [Validators.required]],
      jobSchedule: this.formBuilder.group({
        every: [{ value: 5, disabled: true }, [Validators.required, Validators.min(1)]],
        type: [{ value: ScheduleUnit.Minutes, disabled: true }, [Validators.required]]
      }),
      categories: this.formBuilder.array([]),
      deletePrivate: [{ value: false, disabled: true }],
      unlinkedEnabled: [{ value: false, disabled: true }],
      unlinkedTargetCategory: [{ value: 'cleanuparr-unlinked', disabled: true }, [Validators.required]],
      unlinkedUseTag: [{ value: false, disabled: true }],
      unlinkedIgnoredRootDir: [{ value: '', disabled: true }],
      unlinkedCategories: [{ value: [], disabled: true }]
    });

    // Set up form value change listeners
    this.setupFormValueChangeListeners();

    // Load the current configuration
    effect(() => {
      const config = this.downloadCleanerConfig();
      if (config) {
        this.updateForm(config);
      }
    });
    
    // Effect to handle errors
    effect(() => {
      const errorMessage = this.downloadCleanerError();
      if (errorMessage) {
        // Only emit the error for parent components
        this.error.emit(errorMessage);
      }
    });
  }
  
  /**
   * Add a new category to the form array
   */
  addCategory(): void {
    const defaultCategory = createDefaultCategory();
    const categoryGroup = this.formBuilder.group({
      name: [defaultCategory.name, [Validators.required]],
      maxRatio: [defaultCategory.maxRatio, [Validators.min(-1)]],
      minSeedTime: [defaultCategory.minSeedTime, [Validators.min(0)]],
      maxSeedTime: [defaultCategory.maxSeedTime, [Validators.min(-1)]]
    });
    
    this.categoriesFormArray.push(categoryGroup);
    this.downloadCleanerForm.markAsDirty();
  }
  
  /**
   * Helper method to get a category control as FormGroup for the template
   */
  getCategoryAsFormGroup(index: number): FormGroup {
    return this.categoriesFormArray.at(index) as FormGroup;
  }
  
  /**
   * Remove a category from the form array
   */
  removeCategory(index: number): void {
    this.categoriesFormArray.removeAt(index);
    this.downloadCleanerForm.markAsDirty();
  }

  /**
   * Update the form with values from the configuration
   */
  private updateForm(config: DownloadCleanerConfig): void {
    // Clear existing categories
    while (this.categoriesFormArray.length > 0) {
      this.categoriesFormArray.removeAt(0);
    }

    // Add categories from config
    if (config.categories && config.categories.length > 0) {
      config.categories.forEach(category => {
        const categoryGroup = this.formBuilder.group({
          name: [category.name, [Validators.required]],
          maxRatio: [category.maxRatio, [Validators.min(-1)]],
          minSeedTime: [category.minSeedTime, [Validators.min(0)]],
          maxSeedTime: [category.maxSeedTime, [Validators.min(-1)]]
        });
        
        this.categoriesFormArray.push(categoryGroup);
      });
    }

    // Update form values
    this.downloadCleanerForm.patchValue({
      enabled: config.enabled,
      useAdvancedScheduling: config.useAdvancedScheduling,
      cronExpression: config.cronExpression,
      deletePrivate: config.deletePrivate,
      unlinkedEnabled: config.unlinkedEnabled,
      unlinkedTargetCategory: config.unlinkedTargetCategory,
      unlinkedUseTag: config.unlinkedUseTag,
      unlinkedIgnoredRootDir: config.unlinkedIgnoredRootDir,
      unlinkedCategories: config.unlinkedCategories || []
    });

    // Update job schedule if present
    if (config.jobSchedule) {
      this.downloadCleanerForm.get('jobSchedule')?.patchValue({
        every: config.jobSchedule.every,
        type: config.jobSchedule.type
      });
    }
    
    // Update form control states based on the configuration
    this.updateFormControlDisabledStates(config);
    
    // Store original values for change detection
    this.storeOriginalValues();
    
    // Mark form as pristine after loading
    this.downloadCleanerForm.markAsPristine();
  }

  /**
   * Clean up subscriptions when component is destroyed
   */
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Set up listeners for form control value changes to manage dependent control states
   */
  private setupFormValueChangeListeners(): void {
    // Listen for changes to the 'enabled' control
    const enabledControl = this.downloadCleanerForm.get('enabled');
    if (enabledControl) {
      enabledControl.valueChanges
        .pipe(takeUntil(this.destroy$))
        .subscribe(enabled => {
          this.updateMainControlsState(enabled);
        });
    }

    // Listen for changes to the 'useAdvancedScheduling' control
    const advancedControl = this.downloadCleanerForm.get('useAdvancedScheduling');
    if (advancedControl) {
      advancedControl.valueChanges
        .pipe(takeUntil(this.destroy$))
        .subscribe(useAdvanced => {
          const cronControl = this.downloadCleanerForm.get('cronExpression');
          const jobScheduleControl = this.downloadCleanerForm.get('jobSchedule');
          const options = { onlySelf: true };

          if (useAdvanced) {
            jobScheduleControl?.disable(options);
            cronControl?.enable(options);
          } else {
            cronControl?.disable(options);
            jobScheduleControl?.enable(options);
          }
        });
    }

    // Listen for changes to the 'unlinkedEnabled' control
    const unlinkedEnabledControl = this.downloadCleanerForm.get('unlinkedEnabled');
    if (unlinkedEnabledControl) {
      unlinkedEnabledControl.valueChanges
        .pipe(takeUntil(this.destroy$))
        .subscribe(enabled => {
          this.updateUnlinkedControlsState(enabled);
        });
    }

    // Listen for changes to the schedule type to update available values
    const scheduleTypeControl = this.downloadCleanerForm.get('jobSchedule.type');
    if (scheduleTypeControl) {
      scheduleTypeControl.valueChanges
        .pipe(takeUntil(this.destroy$))
        .subscribe(() => {
          // Ensure the selected value is valid for the new type
          const everyControl = this.downloadCleanerForm.get('jobSchedule.every');
          const currentValue = everyControl?.value;
          const scheduleType = this.downloadCleanerForm.get('jobSchedule.type')?.value;
          
          const validValues = ScheduleOptions[scheduleType as keyof typeof ScheduleOptions];
          if (currentValue && !validValues.includes(currentValue)) {
            everyControl?.setValue(validValues[0]);
          }
        });
    }
    
    // Listen to all form changes to check for actual differences from original values
    this.downloadCleanerForm.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.hasActualChanges = this.formValuesChanged();
      });
  }

  /**
   * Store original form values for dirty checking
   */
  private storeOriginalValues(): void {
    // Create a deep copy of the form values to ensure proper comparison
    // Using getRawValue() instead of just value to include disabled controls
    this.originalFormValues = JSON.parse(JSON.stringify(this.downloadCleanerForm.getRawValue()));
    this.hasActualChanges = false;
  }

  /**
   * Check if the current form values are different from the original values
   */
  formValuesChanged(): boolean {
    if (!this.originalFormValues) return false;
    
    // Use getRawValue() to include disabled controls in the comparison
    const currentValues = this.downloadCleanerForm.getRawValue();
    return !this.isEqual(currentValues, this.originalFormValues);
  }

  /**
   * Deep compare two objects for equality
   */
  private isEqual(obj1: any, obj2: any): boolean {
    if (obj1 === obj2) return true;
    if (obj1 === null || obj2 === null) return false;
    if (typeof obj1 !== 'object' || typeof obj2 !== 'object') return obj1 === obj2;

    const keys1 = Object.keys(obj1);
    const keys2 = Object.keys(obj2);

    if (keys1.length !== keys2.length) return false;

    for (const key of keys1) {
      if (!keys2.includes(key)) return false;
      if (!this.isEqual(obj1[key], obj2[key])) return false;
    }

    return true;
  }

  /**
   * Update form control disabled states based on the configuration
   */
  private updateFormControlDisabledStates(config: DownloadCleanerConfig): void {
    // Update main controls based on enabled state
    this.updateMainControlsState(config.enabled);
    
    // Update schedule controls based on advanced scheduling
    const cronControl = this.downloadCleanerForm.get('cronExpression');
    const jobScheduleControl = this.downloadCleanerForm.get('jobSchedule');

    if (config.useAdvancedScheduling) {
      jobScheduleControl?.disable();
      cronControl?.enable();
    } else {
      cronControl?.disable();
      jobScheduleControl?.enable();
    }
  }
  
  /**
   * Update the state of main controls based on whether the feature is enabled
   */
  private updateMainControlsState(enabled: boolean): void {
    const useAdvancedControl = this.downloadCleanerForm.get('useAdvancedScheduling');
    const cronControl = this.downloadCleanerForm.get('cronExpression');
    const jobScheduleControl = this.downloadCleanerForm.get('jobSchedule');
    const categoriesControl = this.categoriesFormArray;
    const deletePrivateControl = this.downloadCleanerForm.get('deletePrivate');
    const unlinkedEnabledControl = this.downloadCleanerForm.get('unlinkedEnabled');

    // Disable emitting events during bulk changes
    const options = { emitEvent: false };

    if (enabled) {
      // Enable main controls
      useAdvancedControl?.enable(options);
      deletePrivateControl?.enable(options);
      categoriesControl?.enable(options);
      unlinkedEnabledControl?.enable(options);

      // Enable the appropriate scheduling controls based on advanced mode
      const useAdvanced = useAdvancedControl?.value;
      if (useAdvanced) {
        cronControl?.enable(options);
      } else {
        jobScheduleControl?.enable(options);
      }
      
      // Update unlinked controls based on unlinkedEnabled value
      const unlinkedEnabled = unlinkedEnabledControl?.value;
      this.updateUnlinkedControlsState(unlinkedEnabled);
    } else {
      // Disable all controls when the feature is disabled
      useAdvancedControl?.disable(options);
      cronControl?.disable(options);
      jobScheduleControl?.disable(options);
      categoriesControl?.disable(options);
      deletePrivateControl?.disable(options);
      unlinkedEnabledControl?.disable(options);
      
      // Always disable unlinked controls when main feature is disabled
      this.updateUnlinkedControlsState(false);
    }
  }

  /**
   * Save the download cleaner configuration
   */
  saveDownloadCleanerConfig(): void {
    // Mark all form controls as touched to trigger validation
    this.markFormGroupTouched(this.downloadCleanerForm);

    if (this.downloadCleanerForm.valid) {
      // Get form values including disabled controls
      const formValues = this.downloadCleanerForm.getRawValue();

      // Create config object from form values
      const config: DownloadCleanerConfig = {
        enabled: formValues.enabled,
        useAdvancedScheduling: formValues.useAdvancedScheduling,
        cronExpression: formValues.cronExpression,
        jobSchedule: formValues.jobSchedule,
        categories: formValues.categories,
        deletePrivate: formValues.deletePrivate,
        unlinkedEnabled: formValues.unlinkedEnabled,
        unlinkedTargetCategory: formValues.unlinkedTargetCategory,
        unlinkedUseTag: formValues.unlinkedUseTag,
        unlinkedIgnoredRootDir: formValues.unlinkedIgnoredRootDir,
        unlinkedCategories: formValues.unlinkedCategories || []
      };

      // Save the configuration
      this.downloadCleanerStore.saveDownloadCleanerConfig(config)
        .then(success => {
          if (success) {
            // Show success message
            this.notificationService.showSuccess('Download cleaner configuration saved successfully.');
            
            // Emit saved event for parent components
            this.saved.emit();
            
            // Setup a one-time check to mark form as pristine after successful save
            const checkSaveCompletion = () => {
              const saving = this.downloadCleanerSaving();
              const error = this.downloadCleanerError();
              
              if (!saving && !error) {
                // Reset form state after successful save
                this.downloadCleanerForm.markAsPristine();
                this.storeOriginalValues();
              } else if (!saving && error) {
                // If there's an error, we can stop checking
                // No need to show error toast here, it's handled by the LoadingErrorStateComponent
              } else {
                // If still saving, check again in a moment
                setTimeout(checkSaveCompletion, 100);
              }
            };
            
            // Start checking for save completion
            checkSaveCompletion();
          }
        });
    } else {
      // Form is invalid, show error message
      this.notificationService.showValidationError();
      
      // Emit error for parent components
      this.error.emit("Please fix validation errors before saving.");
    }
  }

  /**
   * Reset the download cleaner configuration form to default values
   */
  resetDownloadCleanerConfig(): void {
    // Clear categories
    this.categoriesFormArray.clear();
    
    // Reset form to default values
    this.downloadCleanerForm.reset({
      enabled: false,
      useAdvancedScheduling: false,
      cronExpression: '0 0 * * * ?',
      jobSchedule: {
        type: ScheduleUnit.Minutes,
        every: 5
      },
      categories: [],
      deletePrivate: false,
      unlinkedEnabled: false,
      unlinkedTargetCategory: 'cleanuparr-unlinked',
      unlinkedUseTag: false,
      unlinkedIgnoredRootDir: '',
      unlinkedCategories: []
    });

    // Manually update control states after reset
    this.updateMainControlsState(false);
    
    // Mark form as dirty so the save button is enabled after reset
    this.downloadCleanerForm.markAsDirty();
  }

  /**
   * Mark all controls in a form group as touched
   */
  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.values(formGroup.controls).forEach((control) => {
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
    const control = this.downloadCleanerForm.get(controlName);
    return control ? control.touched && control.hasError(errorName) : false;
  }
  
  /**
   * Get schedule value options based on the current schedule unit type
   */
  getScheduleValueOptions(): {label: string, value: number}[] {
    const scheduleType = this.downloadCleanerForm.get('jobSchedule.type')?.value as ScheduleUnit;
    if (scheduleType === ScheduleUnit.Seconds) {
      return this.scheduleValueOptions[ScheduleUnit.Seconds];
    } else if (scheduleType === ScheduleUnit.Minutes) {
      return this.scheduleValueOptions[ScheduleUnit.Minutes];
    } else if (scheduleType === ScheduleUnit.Hours) {
      return this.scheduleValueOptions[ScheduleUnit.Hours];
    }
    return this.scheduleValueOptions[ScheduleUnit.Minutes]; // Default to minutes
  }

  /**
   * Get nested form control errors
   */
  hasNestedError(parentName: string, controlName: string, errorName: string): boolean {
    const parentControl = this.downloadCleanerForm.get(parentName);
    if (!parentControl || !(parentControl instanceof FormGroup)) {
      return false;
    }

    const control = parentControl.get(controlName);
    return control ? control.touched && control.hasError(errorName) : false;
  }

  /**
   * Check if a control in a form array has an error
   */
  hasCategoryError(index: number, controlName: string, errorName: string): boolean {
    const categoryGroup = this.categoriesFormArray.at(index) as FormGroup;
    if (!categoryGroup) return false;
    
    const control = categoryGroup.get(controlName);
    return control ? control.touched && control.hasError(errorName) : false;
  }

  /**
   * Check if a category form control has an error
   */
  hasCategoryControlError(categoryIndex: number, controlName: string, errorName: string): boolean {
    const categoryGroup = this.categoriesFormArray.at(categoryIndex);
    const control = categoryGroup.get(controlName);
    return control ? control.touched && control.hasError(errorName) : false;
  }

  /**
   * Update the state of unlinked controls based on whether unlinked handling is enabled
   */
  private updateUnlinkedControlsState(enabled: boolean): void {
    const targetCategoryControl = this.downloadCleanerForm.get('unlinkedTargetCategory');
    const useTagControl = this.downloadCleanerForm.get('unlinkedUseTag');
    const ignoredRootDirControl = this.downloadCleanerForm.get('unlinkedIgnoredRootDir');
    const categoriesControl = this.downloadCleanerForm.get('unlinkedCategories');
    
    // Disable emitting events during bulk changes
    const options = { emitEvent: false };
    
    if (enabled) {
      // Enable all unlinked controls
      targetCategoryControl?.enable(options);
      useTagControl?.enable(options);
      ignoredRootDirControl?.enable(options);
      categoriesControl?.enable(options);
    } else {
      // Disable all unlinked controls
      targetCategoryControl?.disable(options);
      useTagControl?.disable(options);
      ignoredRootDirControl?.disable(options);
      categoriesControl?.disable(options);
    }
  }
}

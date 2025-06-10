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
  @Output() saved = new EventEmitter<void>();
  @Output() error = new EventEmitter<string>();

  // Download Cleaner Configuration Form
  downloadCleanerForm: FormGroup;
  
  // Original form values for tracking changes
  private originalFormValues: any;
  
  // Track whether the form has actual changes compared to original values
  hasActualChanges = false;

  // Schedule unit options for job schedules
  scheduleUnitOptions = [
    { label: "Seconds", value: ScheduleUnit.Seconds },
    { label: "Minutes", value: ScheduleUnit.Minutes },
    { label: "Hours", value: ScheduleUnit.Hours },
  ];
  
  // Options for each schedule unit
  scheduleValueOptions = {
    [ScheduleUnit.Seconds]: ScheduleOptions[ScheduleUnit.Seconds].map(v => ({ label: v.toString(), value: v })),
    [ScheduleUnit.Minutes]: ScheduleOptions[ScheduleUnit.Minutes].map(v => ({ label: v.toString(), value: v })),
    [ScheduleUnit.Hours]: ScheduleOptions[ScheduleUnit.Hours].map(v => ({ label: v.toString(), value: v }))
  };
  
  // Display modes for schedule
  scheduleModeOptions = [
    { label: 'Basic', value: false },
    { label: 'Advanced', value: true }
  ];

  // Inject the necessary services
  private formBuilder = inject(FormBuilder);
  private notificationService = inject(NotificationService);
  private downloadCleanerStore = inject(DownloadCleanerConfigStore);

  // Signals from the store
  readonly downloadCleanerConfig = this.downloadCleanerStore.config;
  readonly downloadCleanerLoading = this.downloadCleanerStore.loading;
  readonly downloadCleanerSaving = this.downloadCleanerStore.saving;
  readonly downloadCleanerError = this.downloadCleanerStore.error;

  // Subject for unsubscribing from observables when component is destroyed
  private destroy$ = new Subject<void>();

  /**
   * Check if component can be deactivated (navigation guard)
   */
  canDeactivate(): boolean {
    // Allow navigation if form is not dirty or has been saved
    return !this.downloadCleanerForm?.dirty || !this.formValuesChanged();
  }

  constructor() {
    // Initialize form
    this.downloadCleanerForm = this.formBuilder.group({
      enabled: [false],
      useAdvancedScheduling: [false],
      cronExpression: ['0 0 * * * ?', [Validators.required]],
      jobSchedule: this.formBuilder.group({
        every: [5, [Validators.required]],
        type: [ScheduleUnit.Minutes, [Validators.required]]
      }),
      categories: this.formBuilder.array([]),
      deletePrivate: [false],
      unlinkedTargetCategory: ['cleanuparr-unlinked', [Validators.required]],
      unlinkedUseTag: [false],
      unlinkedIgnoredRootDir: [''],
      unlinkedCategories: [[]]
    });

    // Set up form value change listeners
    this.setupFormValueChangeListeners();

    // Listen for config changes from the store
    effect(() => {
      const config = this.downloadCleanerConfig();
      if (config && !this.downloadCleanerLoading()) {
        this.updateForm(config);
      }
    });
  }

  /**
   * Update the form with values from the configuration
   */
  private updateForm(config: DownloadCleanerConfig): void {
    // Clear existing categories
    this.categoriesFormArray.clear();
    
    // Add each category from the config
    if (config.categories && config.categories.length > 0) {
      config.categories.forEach(category => {
        this.addCategory(category);
      });
    }

    // Update the form values
    this.downloadCleanerForm.patchValue({
      enabled: config.enabled,
      useAdvancedScheduling: config.useAdvancedScheduling,
      cronExpression: config.cronExpression,
      deletePrivate: config.deletePrivate,
      unlinkedTargetCategory: config.unlinkedTargetCategory,
      unlinkedUseTag: config.unlinkedUseTag,
      unlinkedIgnoredRootDir: config.unlinkedIgnoredRootDir,
      unlinkedCategories: config.unlinkedCategories
    });

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
    this.downloadCleanerForm.get('enabled')?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(enabled => {
        this.updateMainControlsState(enabled);
      });

    // Listen for changes to the 'useAdvancedScheduling' control
    this.downloadCleanerForm.get('useAdvancedScheduling')?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(useAdvanced => {
        const cronControl = this.downloadCleanerForm.get('cronExpression');
        const jobScheduleControl = this.downloadCleanerForm.get('jobSchedule');

        if (useAdvanced) {
          jobScheduleControl?.disable();
          cronControl?.enable();
        } else {
          cronControl?.disable();
          jobScheduleControl?.enable();
        }
      });

    // Listen for changes to the schedule type to update available values
    this.downloadCleanerForm.get('jobSchedule.type')?.valueChanges
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

  /**
   * Store original form values for dirty checking
   */
  private storeOriginalValues(): void {
    this.originalFormValues = JSON.parse(JSON.stringify(this.downloadCleanerForm.value));
  }

  /**
   * Check if the current form values are different from the original values
   */
  formValuesChanged(): boolean {
    const currentValues = this.downloadCleanerForm.value;
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
   * Update the state of main controls based on the 'enabled' control value
   */
  private updateMainControlsState(enabled: boolean): void {
    const controls = [
      'useAdvancedScheduling',
      'deletePrivate',
      'unlinkedTargetCategory',
      'unlinkedUseTag',
      'unlinkedIgnoredRootDir',
      'unlinkedCategories'
    ];

    if (enabled) {
      // Enable all controls when the cleaner is enabled
      controls.forEach(controlName => {
        const control = this.downloadCleanerForm.get(controlName);
        if (control) {
          control.enable();
        }
      });

      // Enable or disable schedule controls based on advanced scheduling
      const useAdvanced = this.downloadCleanerForm.get('useAdvancedScheduling')?.value;
      if (useAdvanced) {
        this.downloadCleanerForm.get('cronExpression')?.enable();
        this.downloadCleanerForm.get('jobSchedule')?.disable();
      } else {
        this.downloadCleanerForm.get('cronExpression')?.disable();
        this.downloadCleanerForm.get('jobSchedule')?.enable();
      }

      // Enable categories form array
      this.categoriesFormArray.enable();
    } else {
      // Disable all controls when the cleaner is disabled
      controls.forEach(controlName => {
        const control = this.downloadCleanerForm.get(controlName);
        if (control) {
          control.disable();
        }
      });

      // Disable schedule controls
      this.downloadCleanerForm.get('cronExpression')?.disable();
      this.downloadCleanerForm.get('jobSchedule')?.disable();

      // Disable categories form array
      this.categoriesFormArray.disable();
    }
  }

  /**
   * Get the categories form array
   */
  get categoriesFormArray(): FormArray {
    return this.downloadCleanerForm.get('categories') as FormArray;
  }

  /**
   * Add a new category to the form array
   */
  addCategory(category?: CleanCategory): void {
    // Use the provided category or create a default one
    const defaultCategory = category || createDefaultCategory();
    
    const categoryGroup = this.formBuilder.group({
      name: [defaultCategory.name, [Validators.required]],
      maxRatio: [defaultCategory.maxRatio, [Validators.min(-1), Validators.pattern(/^\d+(\.\d+)?$/)]],
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
   * Save the download cleaner configuration
   */
  saveDownloadCleanerConfig(): void {
    // Mark all form controls as touched to trigger validation
    this.markFormGroupTouched(this.downloadCleanerForm);

    if (this.downloadCleanerForm.valid) {
      // Get form values
      const formValues = this.downloadCleanerForm.getRawValue();

      // Create config object from form values
      const config: DownloadCleanerConfig = {
        enabled: formValues.enabled,
        useAdvancedScheduling: formValues.useAdvancedScheduling,
        cronExpression: formValues.cronExpression,
        categories: formValues.categories,
        deletePrivate: formValues.deletePrivate,
        unlinkedTargetCategory: formValues.unlinkedTargetCategory,
        unlinkedUseTag: formValues.unlinkedUseTag,
        unlinkedIgnoredRootDir: formValues.unlinkedIgnoredRootDir,
        unlinkedCategories: formValues.unlinkedCategories
      };

      // Save the configuration
      this.downloadCleanerStore.saveDownloadCleanerConfig(config)
        .then(success => {
          if (success) {
            // Emit saved event for parent components
            this.saved.emit();
            
            // Setup a one-time check to mark form as pristine after successful save
            const checkSaveCompletion = () => {
              if (!this.downloadCleanerSaving()) {
                // Reset form state after successful save
                this.downloadCleanerForm.markAsPristine();
                this.storeOriginalValues();
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
      cronExpression: "0 0 * * * ?",
      jobSchedule: {
        every: 5,
        type: ScheduleUnit.Minutes,
      },
      categories: [],
      deletePrivate: false,
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
}

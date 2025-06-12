import { Component, EventEmitter, OnDestroy, Output, effect, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { Subject, takeUntil } from "rxjs";
import { RadarrConfigStore } from "./radarr-config.store";
import { CanComponentDeactivate } from "../../core/guards";
import { RadarrConfig } from "../../shared/models/radarr-config.model";

// PrimeNG Components
import { CardModule } from "primeng/card";
import { InputTextModule } from "primeng/inputtext";
import { CheckboxModule } from "primeng/checkbox";
import { ButtonModule } from "primeng/button";
import { InputNumberModule } from "primeng/inputnumber";
import { SelectButtonModule } from "primeng/selectbutton";
import { ToastModule } from "primeng/toast";
import { NotificationService } from "../../core/services/notification.service";
import { DropdownModule } from "primeng/dropdown";
import { LoadingErrorStateComponent } from "../../shared/components/loading-error-state/loading-error-state.component";

@Component({
  selector: "app-radarr-settings",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CardModule,
    InputTextModule,
    CheckboxModule,
    ButtonModule,
    InputNumberModule,
    SelectButtonModule,
    ToastModule,
    DropdownModule,
    LoadingErrorStateComponent,
  ],
  providers: [RadarrConfigStore],
  templateUrl: "./radarr-settings.component.html",
  styleUrls: ["./radarr-settings.component.scss"],
})
export class RadarrSettingsComponent implements OnDestroy, CanComponentDeactivate {
  @Output() saved = new EventEmitter<void>();
  @Output() error = new EventEmitter<string>();

  // Radarr Configuration Form
  radarrForm: FormGroup;

  // Original form values for tracking changes
  private originalFormValues: any;

  // Track whether the form has actual changes compared to original values
  hasActualChanges = false;

  // Clean up subscriptions
  private destroy$ = new Subject<void>();

  // Inject the necessary services
  private formBuilder = inject(FormBuilder);
  // Using the notification service for all toast messages
  private notificationService = inject(NotificationService);
  private radarrStore = inject(RadarrConfigStore);

  // Signals from store
  radarrConfig = this.radarrStore.config;
  radarrLoading = this.radarrStore.loading;
  radarrError = this.radarrStore.error;
  radarrSaving = this.radarrStore.saving;

  /**
   * Check if component can be deactivated (navigation guard)
   */
  canDeactivate(): boolean {
    return !this.radarrForm?.dirty || !this.hasActualChanges;
  }

  constructor() {
    // Initialize the main form
    this.radarrForm = this.formBuilder.group({
      enabled: [false],
      failedImportMaxStrikes: [-1],
    });

    // Add instances FormArray to main form
    this.radarrForm.addControl('instances', this.formBuilder.array([]));

    // Load Radarr config data
    this.radarrStore.loadConfig();

    // Setup effect to update form when config changes
    effect(() => {
      const config = this.radarrConfig();
      if (config) {
        this.updateFormFromConfig(config);
      }
    });
  }

  /**
   * Clean up subscriptions when component is destroyed
   */
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Update form with values from the configuration
   */
  private updateFormFromConfig(config: RadarrConfig): void {
    // Update main form controls
    this.radarrForm.patchValue({
      enabled: config.enabled,
      failedImportMaxStrikes: config.failedImportMaxStrikes
    });

    // Clear and rebuild the instances form array
    const instancesArray = this.radarrForm.get('instances') as FormArray;
    instancesArray.clear();

    // Add all instances to the form array
    if (config.instances && config.instances.length > 0) {
      config.instances.forEach(instance => {
        instancesArray.push(
          this.formBuilder.group({
            id: [instance.id || ''],
            name: [instance.name, Validators.required],
            url: [instance.url, Validators.required],
            apiKey: [instance.apiKey, Validators.required],
          })
        );
      });
    }

    // Store original form values for dirty checking
    this.storeOriginalValues();
  }

  /**
   * Store original form values for dirty checking
   */
  private storeOriginalValues(): void {
    this.originalFormValues = JSON.parse(JSON.stringify(this.radarrForm.value));
    this.radarrForm.markAsPristine();
    this.hasActualChanges = false;
  }

  /**
   * Check if the current form values are different from the original values
   */
  private formValuesChanged(): boolean {
    return !this.isEqual(this.radarrForm.value, this.originalFormValues);
  }

  /**
   * Deep compare two objects for equality
   */
  private isEqual(obj1: any, obj2: any): boolean {
    if (obj1 === obj2) return true;

    if (typeof obj1 !== "object" || typeof obj2 !== "object" || obj1 == null || obj2 == null) {
      return false;
    }

    const keys1 = Object.keys(obj1);
    const keys2 = Object.keys(obj2);

    if (keys1.length !== keys2.length) return false;

    for (const key of keys1) {
      const val1 = obj1[key];
      const val2 = obj2[key];
      const areObjects = typeof val1 === "object" && typeof val2 === "object";

      if ((areObjects && !this.isEqual(val1, val2)) || (!areObjects && val1 !== val2)) {
        return false;
      }
    }

    return true;
  }

  /**
   * Update form control disabled states based on the configuration
   */
  private updateFormControlDisabledStates(config: RadarrConfig): void {
    const enabled = config.enabled;
    this.updateMainControlsState(enabled);
  }

  /**
   * Update the state of main controls based on the 'enabled' control value
   */
  private updateMainControlsState(enabled: boolean): void {
    const failedImportMaxStrikesControl = this.radarrForm.get('failedImportMaxStrikes');
    const searchTypeControl = this.radarrForm.get('searchType');

    if (enabled) {
      failedImportMaxStrikesControl?.enable();
      searchTypeControl?.enable();
    } else {
      failedImportMaxStrikesControl?.disable();
      searchTypeControl?.disable();
    }
  }

  /**
   * Save the Radarr configuration
   */
  saveRadarrConfig(): void {
    if (this.radarrForm.valid) {
      // Mark form as saving
      this.radarrForm.disable();

      // Get data from form
      const formValue = this.radarrForm.getRawValue();

      // Create config object
      const radarrConfig: RadarrConfig = {
        enabled: formValue.enabled,
        failedImportMaxStrikes: formValue.failedImportMaxStrikes,
        instances: formValue.instances || []
      };

      // Save the configuration
      this.radarrStore.saveConfig(radarrConfig);

      // Setup a one-time check for save completion
      const checkSaveCompletion = () => {
        // Check if we're done saving
        if (!this.radarrSaving()) {
          // Re-enable the form
          this.radarrForm.enable();

          // If still disabled, update control states based on enabled state
          if (!this.radarrForm.get('enabled')?.value) {
            this.updateMainControlsState(false);
          }

          // Update original values to match current form state
          this.storeOriginalValues();

          // Notify listeners that we've completed the save
          this.saved.emit();

          // Show success message
          this.notificationService.showSuccess("Radarr configuration saved successfully");
        } else {
          // If still saving, check again in a moment
          setTimeout(checkSaveCompletion, 100);
        }
      };

      // Start checking for save completion
      checkSaveCompletion();
    } else {
      // Form is invalid, show error message
      this.notificationService.showValidationError();

      // Emit error for parent components
      this.error.emit("Please fix validation errors before saving.");

      // Mark all controls as touched to show validation errors
      this.markFormGroupTouched(this.radarrForm);
    }
  }

  /**
   * Reset the Radarr configuration form to default values
   */
  resetRadarrConfig(): void {
    this.radarrForm.reset({
      enabled: false,
      failedImportMaxStrikes: -1,
    });

    // Clear all instances
    const instancesArray = this.radarrForm.get('instances') as FormArray;
    instancesArray.clear();

    // Update control states after reset
    this.updateMainControlsState(false);

    // Mark form as dirty so the save button is enabled after reset
    this.radarrForm.markAsDirty();
    this.hasActualChanges = true;
  }

  /**
   * Add a new instance to the instances form array
   */
  addInstance(): void {
    const instancesArray = this.radarrForm.get('instances') as FormArray;

    instancesArray.push(
      this.formBuilder.group({
        id: [''],
        name: ['', Validators.required],
        url: ['', Validators.required],
        apiKey: ['', Validators.required],
      })
    );

    this.radarrForm.markAsDirty();
    this.hasActualChanges = true;
  }

  /**
   * Remove an instance from the list
   */
  removeInstance(index: number): void {
    const instancesArray = this.radarrForm.get('instances') as FormArray;
    instancesArray.removeAt(index);
    this.radarrForm.markAsDirty();
    this.hasActualChanges = true;
  }

  /**
   * Get the instances form array
   */
  get instances(): FormArray {
    return this.radarrForm.get('instances') as FormArray;
  }

  /**
   * Get an instance at the specified index as a FormGroup
   */
  getInstanceAsFormGroup(index: number): FormGroup {
    return this.instances.at(index) as FormGroup;
  }

  // hasInstanceFieldError is implemented below

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
   * Check if the form control has an error
   * @param controlName The name of the control to check
   * @param errorName The name of the error to check for
   * @returns True if the control has the specified error
   */
  hasError(controlName: string, errorName: string): boolean {
    const control = this.radarrForm.get(controlName);
    return control !== null && control.hasError(errorName) && control.touched;
  }

  /**
   * Check if an instance field has an error
   * @param instanceIndex The index of the instance in the array
   * @param fieldName The name of the field to check
   * @param errorName The name of the error to check for
   * @returns True if the field has the specified error
   */
  hasInstanceFieldError(instanceIndex: number, fieldName: string, errorName: string): boolean {
    const instancesArray = this.radarrForm.get('instances') as FormArray;
    if (!instancesArray || !instancesArray.controls[instanceIndex]) return false;
    
    const control = (instancesArray.controls[instanceIndex] as FormGroup).get(fieldName);
    return control !== null && control.hasError(errorName) && control.touched;
  }
}

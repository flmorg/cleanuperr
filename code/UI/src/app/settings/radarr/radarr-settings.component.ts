import { Component, EventEmitter, OnDestroy, Output, effect, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { Subject, takeUntil } from "rxjs";
import { RadarrConfigStore } from "./radarr-config.store";
import { CanComponentDeactivate } from "../../core/guards";
import { RadarrConfig } from "../../shared/models/radarr-config.model";
import { CreateArrInstanceDto, ArrInstance } from "../../shared/models/arr-config.model";

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
  instanceOperations = this.radarrStore.instanceOperations;

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
   * Add a new instance to the instances form array
   * @param instance Optional instance configuration to initialize the form with
   */
  addInstance(instance: ArrInstance | null = null): void {
    const instanceForm = this.formBuilder.group({
      id: [instance?.id || ''],
      name: [instance?.name || '', Validators.required],
      url: [instance?.url?.toString() || '', Validators.required],
      apiKey: [instance?.apiKey || '', Validators.required]
    });
    
    this.instances.push(instanceForm);
  }

  /**
   * Remove an instance at the specified index
   */
  removeInstance(index: number): void {
    const instanceForm = this.getInstanceAsFormGroup(index);
    const instanceId = instanceForm.get('id')?.value;
    
    // Just remove from the form array - deletion will be handled on save
    this.instances.removeAt(index);
    
    // Mark form as dirty to enable save button
    this.radarrForm.markAsDirty();
    this.hasActualChanges = this.formValuesChanged();
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

  /**
   * Save the Radarr configuration
   */
  saveRadarrConfig(): void {
    // Mark all form controls as touched to trigger validation
    this.markFormGroupTouched(this.radarrForm);

    if (this.radarrForm.invalid) {
      this.notificationService.showError('Please fix the validation errors before saving');
      return;
    }

    if (!this.hasActualChanges) {
      this.notificationService.showSuccess('No changes detected');
      return;
    }

    // Get the current config to preserve existing instances
    const currentConfig = this.radarrConfig();
    if (!currentConfig) return;

    // Create the updated main config
    const updatedConfig: RadarrConfig = {
      ...currentConfig,
      enabled: this.radarrForm.get('enabled')?.value,
      failedImportMaxStrikes: this.radarrForm.get('failedImportMaxStrikes')?.value
    };

    // Get the instances from the form
    const formInstances = this.instances.getRawValue();
    
    // Separate creates and updates
    const creates: CreateArrInstanceDto[] = [];
    const updates: Array<{ id: string, instance: ArrInstance }> = [];
    
    formInstances.forEach((instance: any) => {
      if (instance.id) {
        // This is an existing instance, prepare for update
        const updateInstance: ArrInstance = {
          id: instance.id,
          name: instance.name,
          url: instance.url,
          apiKey: instance.apiKey
        };
        updates.push({ id: instance.id, instance: updateInstance });
      } else {
        // This is a new instance, prepare for creation (don't send ID)
        const createInstance: CreateArrInstanceDto = {
          name: instance.name,
          url: instance.url,
          apiKey: instance.apiKey
        };
        creates.push(createInstance);
      }
    });
    
    // Save main config first, then handle instances
    this.radarrStore.saveConfig(updatedConfig);
    
    // Handle instance operations if there are any
    if (creates.length > 0) {
      creates.forEach(instance => this.radarrStore.createInstance(instance));
    }
    if (updates.length > 0) {
      updates.forEach(({ id, instance }) => this.radarrStore.updateInstance({ id, instance }));
    }
    
    // Monitor the saving state to show completion feedback
    this.monitorSavingCompletion();
  }

  /**
   * Monitor saving completion and show appropriate feedback
   */
  private monitorSavingCompletion(): void {
    // Use a timeout to check the saving state periodically
    const checkSavingStatus = () => {
      const saving = this.radarrSaving();
      const error = this.radarrError();
      const pendingOps = this.instanceOperations();
      
      if (!saving && Object.keys(pendingOps).length === 0) {
        // Operations are complete
        if (error) {
          this.notificationService.showError(`Save completed with issues: ${error}`);
          this.error.emit(error);
          // Don't mark as pristine if there were errors
        } else {
          // Complete success
          this.notificationService.showSuccess('Radarr configuration saved successfully');
          this.saved.emit();
          
          // Reload config from backend to ensure UI is in sync
          this.radarrStore.loadConfig();
          
          // Reset form state after successful save
          setTimeout(() => {
            this.radarrForm.markAsPristine();
            this.hasActualChanges = false;
            this.storeOriginalValues();
          }, 100);
        }
      } else {
        // Still saving, check again in a short while
        setTimeout(checkSavingStatus, 100);
      }
    };
    
    // Start monitoring
    setTimeout(checkSavingStatus, 100);
  }

  /**
   * Reset the Radarr configuration form to default values
   */
  resetRadarrConfig(): void {
    // Clear all instances
    const instancesArray = this.radarrForm.get('instances') as FormArray;
    instancesArray.clear();
    
    // Reset main config to defaults
    this.radarrForm.patchValue({
      enabled: false,
      failedImportMaxStrikes: -1
    });
    
    // Check if this reset actually changes anything compared to the original state
    const hasChangesAfterReset = this.formValuesChanged();
    
    if (hasChangesAfterReset) {
      // Only mark as dirty if the reset actually changes something
      this.radarrForm.markAsDirty();
      this.hasActualChanges = true;
    } else {
      // If reset brings us back to original state, mark as pristine
      this.radarrForm.markAsPristine();
      this.hasActualChanges = false;
    }
  }
}

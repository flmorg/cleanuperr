import { Component, EventEmitter, OnDestroy, Output, effect, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { Subject, takeUntil } from "rxjs";
import { SonarrConfigStore } from "./sonarr-config.store";
import { CanComponentDeactivate } from "../../core/guards";
import { SonarrConfig, SonarrSearchType, ArrInstance } from "../../shared/models/sonarr-config.model";

// PrimeNG Components
import { CardModule } from "primeng/card";
import { InputTextModule } from "primeng/inputtext";
import { CheckboxModule } from "primeng/checkbox";
import { ButtonModule } from "primeng/button";
import { InputNumberModule } from "primeng/inputnumber";
import { AccordionModule } from "primeng/accordion";
import { SelectButtonModule } from "primeng/selectbutton";
import { DialogModule } from "primeng/dialog";
import { TableModule } from "primeng/table";
import { ToastModule } from "primeng/toast";
import { NotificationService } from "../../core/services/notification.service";
import { DropdownModule } from "primeng/dropdown";
import { LoadingErrorStateComponent } from "../../shared/components/loading-error-state/loading-error-state.component";

@Component({
  selector: "app-sonarr-settings",
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
    DialogModule,
    TableModule,
    ToastModule,
    DropdownModule,
    LoadingErrorStateComponent,
  ],
  providers: [SonarrConfigStore],
  templateUrl: "./sonarr-settings.component.html",
  styleUrls: ["./sonarr-settings.component.scss"],
})
export class SonarrSettingsComponent implements OnDestroy, CanComponentDeactivate {
  @Output() saved = new EventEmitter<void>();
  @Output() error = new EventEmitter<string>();

  // Sonarr Configuration Form
  sonarrForm: FormGroup;
  
  // Original form values for tracking changes
  private originalFormValues: any;
  
  // Track whether the form has actual changes compared to original values
  hasActualChanges = false;

  // Dialog state
  showInstanceDialog = false;
  editingInstanceIndex: number | null = null;
  instanceForm: FormGroup;

  // SonarrSearchType options
  searchTypeOptions = [
    { label: "Episode", value: SonarrSearchType.Episode },
    { label: "Season", value: SonarrSearchType.Season },
    { label: "Series", value: SonarrSearchType.Series },
  ];

  // Clean up subscriptions
  private destroy$ = new Subject<void>();

  // Inject the necessary services
  private formBuilder = inject(FormBuilder);
  // Using the notification service for all toast messages
  private notificationService = inject(NotificationService);
  private sonarrStore = inject(SonarrConfigStore);

  // Signals from the store
  readonly sonarrConfig = this.sonarrStore.config;
  readonly sonarrLoading = this.sonarrStore.loading;
  readonly sonarrSaving = this.sonarrStore.saving;
  readonly sonarrError = this.sonarrStore.error;

  /**
   * Check if component can be deactivated (navigation guard)
   */
  canDeactivate(): boolean {
    return !this.sonarrForm?.dirty || !this.hasActualChanges;
  }

  constructor() {
    // Initialize the main form
    this.sonarrForm = this.formBuilder.group({
      enabled: [false],
      failedImportMaxStrikes: [0],
      searchType: [SonarrSearchType.Episode, Validators.required],
    });

    // Initialize the instance form
    this.instanceForm = this.formBuilder.group({
      id: [''],
      name: ['', Validators.required],
      url: ['', [Validators.required]],
      apiKey: ['', [Validators.required]],
    });

    // Add instances FormArray to main form
    this.sonarrForm.addControl('instances', this.formBuilder.array([]));

    // Setup value change listeners
    this.setupFormValueChangeListeners();

    // Create an effect to respond to config changes
    effect(() => {
      const config = this.sonarrConfig();
      if (config) {
        this.updateForm(config);
        this.storeOriginalValues();
        this.updateFormControlDisabledStates(config);
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
   * Set up listeners for form control value changes to manage dependent control states
   */
  private setupFormValueChangeListeners(): void {
    // Listen for changes on the enabled control
    this.sonarrForm
      .get("enabled")
      ?.valueChanges.pipe(takeUntil(this.destroy$))
      .subscribe((enabled) => {
        this.updateMainControlsState(enabled);
      });

    // Listen for form changes to update the hasActualChanges flag
    this.sonarrForm.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.hasActualChanges = this.formValuesChanged();
      });
  }

  /**
   * Store original form values for dirty checking
   */
  private storeOriginalValues(): void {
    this.originalFormValues = JSON.parse(JSON.stringify(this.sonarrForm.value));
    this.sonarrForm.markAsPristine();
    this.hasActualChanges = false;
  }

  /**
   * Check if the current form values are different from the original values
   */
  private formValuesChanged(): boolean {
    return !this.isEqual(this.sonarrForm.value, this.originalFormValues);
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
  private updateFormControlDisabledStates(config: SonarrConfig): void {
    const enabled = config.enabled;
    this.updateMainControlsState(enabled);
  }

  /**
   * Update the state of main controls based on the 'enabled' control value
   */
  private updateMainControlsState(enabled: boolean): void {
    const failedImportMaxStrikesControl = this.sonarrForm.get('failedImportMaxStrikes');
    const searchTypeControl = this.sonarrForm.get('searchType');
    
    if (enabled) {
      failedImportMaxStrikesControl?.enable();
      searchTypeControl?.enable();
    } else {
      failedImportMaxStrikesControl?.disable();
      searchTypeControl?.disable();
    }
  }

  /**
   * Update the form with values from the configuration
   */
  private updateForm(config: SonarrConfig): void {
    // Update main form controls
    this.sonarrForm.patchValue({
      enabled: config.enabled,
      failedImportMaxStrikes: config.failedImportMaxStrikes,
      searchType: config.searchType,
    });

    // Clear and rebuild the instances form array
    const instancesArray = this.sonarrForm.get('instances') as FormArray;
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
  }

  /**
   * Get the instances form array
   */
  get instances(): FormArray {
    return this.sonarrForm.get('instances') as FormArray;
  }

  /**
   * Save the Sonarr configuration
   */
  saveSonarrConfig(): void {
    if (this.sonarrForm.valid) {
      // Mark form as saving
      this.sonarrForm.disable();

      // Get data from form
      const formValue = this.sonarrForm.getRawValue();
      
      // Create config object
      const sonarrConfig: SonarrConfig = {
        enabled: formValue.enabled,
        failedImportMaxStrikes: formValue.failedImportMaxStrikes,
        searchType: formValue.searchType,
        instances: formValue.instances || []
      };
      
      // Save the configuration
      this.sonarrStore.saveConfig(sonarrConfig);

      // Setup a one-time check for save completion
      const checkSaveCompletion = () => {
        // Check if we're done saving
        if (!this.sonarrSaving()) {
          // Re-enable the form
          this.sonarrForm.enable();
          
          // If still disabled, update control states based on enabled state
          if (!this.sonarrForm.get('enabled')?.value) {
            this.updateMainControlsState(false);
          }
          
          // Update original values to match current form state
          this.storeOriginalValues();
          
          // Notify listeners that we've completed the save
          this.saved.emit();
          
          // Show success message
          this.notificationService.showSuccess("Sonarr configuration saved successfully");
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
      this.markFormGroupTouched(this.sonarrForm);
    }
  }

  /**
   * Reset the Sonarr configuration form to default values
   */
  resetSonarrConfig(): void {
    this.sonarrForm.reset({
      enabled: false,
      failedImportMaxStrikes: 0,
      searchType: SonarrSearchType.Episode,
    });

    // Clear all instances
    const instancesArray = this.sonarrForm.get('instances') as FormArray;
    instancesArray.clear();

    // Update control states after reset
    this.updateMainControlsState(false);
    
    // Mark form as dirty so the save button is enabled after reset
    this.sonarrForm.markAsDirty();
    this.hasActualChanges = true;
  }

  /**
   * Open the instance dialog for adding a new instance
   */
  openAddInstanceDialog(): void {
    this.editingInstanceIndex = null;
    this.instanceForm.reset({
      id: '',
      name: '',
      url: '',
      apiKey: '',
    });
    this.showInstanceDialog = true;
  }

  /**
   * Open the instance dialog for editing an existing instance
   */
  openEditInstanceDialog(index: number): void {
    const instanceToEdit = this.instances.at(index).value;
    this.editingInstanceIndex = index;
    
    this.instanceForm.reset({
      id: instanceToEdit.id || '',
      name: instanceToEdit.name,
      url: instanceToEdit.url,
      apiKey: instanceToEdit.apiKey,
    });
    
    this.showInstanceDialog = true;
  }

  /**
   * Save the instance from the dialog
   */
  saveInstance(): void {
    if (this.instanceForm.invalid) {
      this.markFormGroupTouched(this.instanceForm);
      return;
    }

    const instanceData = this.instanceForm.value;
    const instancesArray = this.sonarrForm.get('instances') as FormArray;

    if (this.editingInstanceIndex !== null) {
      // Update existing instance
      instancesArray.at(this.editingInstanceIndex).patchValue(instanceData);
    } else {
      // Add new instance
      instancesArray.push(
        this.formBuilder.group({
          id: [instanceData.id || ''],
          name: [instanceData.name, Validators.required],
          url: [instanceData.url, Validators.required],
          apiKey: [instanceData.apiKey, Validators.required],
        })
      );
    }

    this.showInstanceDialog = false;
    this.sonarrForm.markAsDirty();
    this.hasActualChanges = true;
  }

  /**
   * Remove an instance from the list
   */
  removeInstance(index: number): void {
    const instancesArray = this.sonarrForm.get('instances') as FormArray;
    instancesArray.removeAt(index);
    this.sonarrForm.markAsDirty();
    this.hasActualChanges = true;
  }

  /**
   * Cancel the instance dialog
   */
  cancelInstanceDialog(): void {
    this.showInstanceDialog = false;
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
    const control = this.sonarrForm.get(controlName);
    return control ? control.touched && control.hasError(errorName) : false;
  }

  /**
   * Get nested form control errors
   */
  hasInstanceError(controlName: string, errorName: string): boolean {
    const control = this.instanceForm.get(controlName);
    return control ? control.touched && control.hasError(errorName) : false;
  }
}

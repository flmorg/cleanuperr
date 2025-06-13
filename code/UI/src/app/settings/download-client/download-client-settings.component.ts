import { Component, EventEmitter, OnDestroy, Output, effect, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { Subject, takeUntil } from "rxjs";
import { DownloadClientConfigStore } from "./download-client-config.store";
import { CanComponentDeactivate } from "../../core/guards";
import { ClientConfig, DownloadClientConfig } from "../../shared/models/download-client-config.model";
import { DownloadClientType } from "../../shared/models/enums";

// PrimeNG Components
import { CardModule } from "primeng/card";
import { InputTextModule } from "primeng/inputtext";
import { CheckboxModule } from "primeng/checkbox";
import { ButtonModule } from "primeng/button";
import { InputNumberModule } from "primeng/inputnumber";
import { SelectModule } from 'primeng/select';
import { ToastModule } from "primeng/toast";
import { NotificationService } from "../../core/services/notification.service";
import { LoadingErrorStateComponent } from "../../shared/components/loading-error-state/loading-error-state.component";

@Component({
  selector: "app-download-client-settings",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CardModule,
    InputTextModule,
    CheckboxModule,
    ButtonModule,
    InputNumberModule,
    SelectModule,
    ToastModule,
    LoadingErrorStateComponent
  ],
  providers: [DownloadClientConfigStore],
  templateUrl: "./download-client-settings.component.html",
  styleUrls: ["./download-client-settings.component.scss"],
})
export class DownloadClientSettingsComponent implements OnDestroy, CanComponentDeactivate {
  @Output() saved = new EventEmitter<void>();
  @Output() error = new EventEmitter<string>();

  // Download Client Configuration Form
  downloadClientForm: FormGroup;

  // Original form values for tracking changes
  private originalFormValues: any;

  // Track whether the form has actual changes compared to original values
  hasActualChanges = false;

  // Download client type options
  clientTypeOptions = [
    { label: "QBittorrent", value: DownloadClientType.QBittorrent },
    { label: "Deluge", value: DownloadClientType.Deluge },
    { label: "Transmission", value: DownloadClientType.Transmission }
  ];

  // Clean up subscriptions
  private destroy$ = new Subject<void>();

  // Inject the necessary services
  private formBuilder = inject(FormBuilder);
  private notificationService = inject(NotificationService);
  private downloadClientStore = inject(DownloadClientConfigStore);

  // Signals from store
  downloadClientConfig = this.downloadClientStore.config;
  downloadClientLoading = this.downloadClientStore.loading;
  downloadClientError = this.downloadClientStore.error;
  downloadClientSaving = this.downloadClientStore.saving;

  /**
   * Check if component can be deactivated (navigation guard)
   */
  canDeactivate(): boolean {
    return !this.downloadClientForm?.dirty || !this.hasActualChanges;
  }

  constructor() {
    // Initialize the main form
    this.downloadClientForm = this.formBuilder.group({});

    // Add clients FormArray to main form
    this.downloadClientForm.addControl('clients', this.formBuilder.array([]));

    // Load Download Client config data
    this.downloadClientStore.loadConfig();

    // Setup effect to update form when config changes
    effect(() => {
      const config = this.downloadClientConfig();
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
  private updateFormFromConfig(config: DownloadClientConfig): void {
    // Clear existing clients
    const clientsArray = this.downloadClientForm.get('clients') as FormArray;
    clientsArray.clear();

    // Add each client to the form array
    if (config.clients && config.clients.length > 0) {
      config.clients.forEach(client => {
        this.addClient(client);
      });
    }

    // Store the original values for change detection
    this.storeOriginalValues();

    // Mark the form as pristine after loading data
    this.downloadClientForm.markAsPristine();
    this.hasActualChanges = false;
  }

  /**
   * Store original form values for dirty checking
   */
  private storeOriginalValues(): void {
    this.originalFormValues = JSON.parse(JSON.stringify(this.downloadClientForm.value));
  }

  /**
   * Check if the current form values are different from the original values
   */
  private formValuesChanged(): boolean {
    return !this.isEqual(this.downloadClientForm.value, this.originalFormValues);
  }

  /**
   * Deep compare two objects for equality
   */
  private isEqual(obj1: any, obj2: any): boolean {
    if (obj1 === obj2) return true;
    if (obj1 === null || obj2 === null) return false;
    if (obj1 === undefined || obj2 === undefined) return false;
    
    if (typeof obj1 !== 'object' && typeof obj2 !== 'object') {
      return obj1 === obj2;
    }
    
    if (Array.isArray(obj1) && Array.isArray(obj2)) {
      if (obj1.length !== obj2.length) return false;
      for (let i = 0; i < obj1.length; i++) {
        if (!this.isEqual(obj1[i], obj2[i])) return false;
      }
      return true;
    }
    
    const keys1 = Object.keys(obj1);
    const keys2 = Object.keys(obj2);
    
    if (keys1.length !== keys2.length) return false;
    
    for (const key of keys1) {
      if (!this.isEqual(obj1[key], obj2[key])) return false;
    }
    
    return true;
  }

  /**
   * Save the Download Client configuration
   */
  saveDownloadClientConfig(): void {
    if (this.downloadClientForm.valid) {
      // Get the form values
      const formValues = this.downloadClientForm.value as DownloadClientConfig;
      
      // Flag to track actual changes
      this.hasActualChanges = this.formValuesChanged();
      
      // Save the configuration
      this.downloadClientStore.saveConfig(formValues);
      
      // Setup a one-time check for save completion
      const checkSaveCompletion = () => {
        // Check if saving is complete
        if (!this.downloadClientSaving()) {
          // Check if there's an error
          const error = this.downloadClientError();
          if (error) {
            // Show error notification
            this.notificationService.showError('Failed to save configuration');
            
            // Emit error for parent components
            this.error.emit(error);
            return;
          }
          
          // Save successful
          
          // Store new original values
          this.storeOriginalValues();
          
          // Mark form as pristine after save
          this.downloadClientForm.markAsPristine();
          this.hasActualChanges = false;
          
          // Notify listeners that we've completed the save
          this.saved.emit();
          
          // Show success message
          this.notificationService.showSuccess("Download Client configuration saved successfully");
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
      this.markFormGroupTouched(this.downloadClientForm);
    }
  }

  /**
   * Reset the Download Client configuration form to default values
   */
  resetDownloadClientConfig(): void {
    // Clear all clients
    const clientsArray = this.downloadClientForm.get('clients') as FormArray;
    clientsArray.clear();
    
    // Mark form as dirty so the save button is enabled after reset
    this.downloadClientForm.markAsDirty();
    this.hasActualChanges = true;
  }

  /**
   * Add a new client to the clients form array
   */
  addClient(client: ClientConfig | null = null): void {
    const clientsArray = this.downloadClientForm.get('clients') as FormArray;
    
    clientsArray.push(
      this.formBuilder.group({
        enabled: [client?.enabled ?? true],
        id: [client?.id ?? ''],
        name: [client?.name ?? '', Validators.required],
        type: [client?.type ?? DownloadClientType.QBittorrent, Validators.required],
        host: [client?.host ?? '', Validators.required],
        username: [client?.username ?? ''],
        password: [client?.password ?? ''],
        urlBase: [client?.urlBase ?? '']
      })
    );
    
    this.downloadClientForm.markAsDirty();
    this.hasActualChanges = true;
  }

  /**
   * Remove a client from the list
   */
  removeClient(index: number): void {
    const clientsArray = this.downloadClientForm.get('clients') as FormArray;
    clientsArray.removeAt(index);
    this.downloadClientForm.markAsDirty();
    this.hasActualChanges = true;
  }

  /**
   * Get the clients form array
   */
  get clients(): FormArray {
    return this.downloadClientForm.get('clients') as FormArray;
  }

  /**
   * Get a client at the specified index as a FormGroup
   */
  getClientAsFormGroup(index: number): FormGroup {
    return this.clients.at(index) as FormGroup;
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
   * Check if the form control has an error
   * @param controlName The name of the control to check
   * @param errorName The name of the error to check for
   * @returns True if the control has the specified error
   */
  hasError(controlName: string, errorName: string): boolean {
    const control = this.downloadClientForm.get(controlName);
    return control !== null && control.hasError(errorName) && control.touched;
  }

  /**
   * Check if a client field has an error
   * @param clientIndex The index of the client in the array
   * @param fieldName The name of the field to check
   * @param errorName The name of the error to check for
   * @returns True if the field has the specified error
   */
  hasClientFieldError(clientIndex: number, fieldName: string, errorName: string): boolean {
    const clientsArray = this.downloadClientForm.get('clients') as FormArray;
    if (!clientsArray || !clientsArray.controls[clientIndex]) return false;
    
    const control = (clientsArray.controls[clientIndex] as FormGroup).get(fieldName);
    return control !== null && control.hasError(errorName) && control.touched;
  }
}

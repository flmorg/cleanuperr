import { Component, EventEmitter, OnDestroy, Output, effect, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors, FormControl } from "@angular/forms";
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
    { label: "Transmission", value: DownloadClientType.Transmission },
    { label: "Usenet", value: DownloadClientType.Usenet }
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
   * Get the clients form array
   */
  public get clients(): FormArray {
    return this.downloadClientForm.get('clients') as FormArray;
  }

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

    // Track form changes for dirty state
    this.downloadClientForm.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.hasActualChanges = this.formValuesChanged();
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
    // Mark all form controls as touched to trigger validation
    this.markFormGroupTouched(this.downloadClientForm);

    if (this.downloadClientForm.invalid) {
      this.notificationService.showError('Please fix the validation errors before saving');
      return;
    }

    if (!this.hasActualChanges) {
      this.notificationService.showSuccess('No changes detected');
      return;
    }

    // Get the clients from the form
    const formClients = this.clients.getRawValue();
    
    // Keep track of operations
    let operationsCount = 0;
    
    // Process each client
    formClients.forEach((client: any) => {
      // Map the client type for backend compatibility
      const mappedType = this.mapClientTypeForBackend(client.type);
      const backendClient = {
        ...client,
        typeName: mappedType.typeName,
        type: mappedType.type
      };
      
      if (client.id) {
        // This is an existing client, use the individual update endpoint
        operationsCount++;
        this.downloadClientStore.updateClient({ id: client.id, client: backendClient });
      } else {
        // This is a new client, create it (don't send ID)
        operationsCount++;
        const { id, ...clientWithoutId } = backendClient;
        this.downloadClientStore.createClient(clientWithoutId);
      }
    });
    
    // If we don't have any clients to process, show a message
    if (operationsCount === 0) {
      this.notificationService.showSuccess('No clients to save');
      return;
    }
    
    // Monitor the saving state to show completion feedback
    const savingSubscription = this.downloadClientSaving().valueOf() !== false ? 
      this.monitorSavingCompletion() : null;
  }

  /**
   * Monitor saving completion and show appropriate feedback
   */
  private monitorSavingCompletion(): void {
    // Use a timeout to check the saving state periodically
    const checkSavingStatus = () => {
      const saving = this.downloadClientSaving();
      const error = this.downloadClientError();
      
      if (!saving) {
        // Saving is complete
        if (error) {
          this.notificationService.showError(`Failed to save: ${error}`);
          this.error.emit(error);
        } else {
          // Success
          this.notificationService.showSuccess('Download Client configuration saved successfully');
          this.saved.emit();
          this.downloadClientForm.markAsPristine();
          this.hasActualChanges = false;
          this.storeOriginalValues();
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
   * @param client Optional client configuration to initialize the form with
   */
  addClient(client: ClientConfig | null = null): void {
    // If client has typeName from backend, map it to frontend type
    const frontendType = client?.typeName 
      ? this.mapClientTypeFromBackend(client.typeName)
      : client?.type || null;
    
    const clientForm = this.formBuilder.group({
      id: [client?.id || ''],
      name: [client?.name || '', Validators.required],
      type: [frontendType, Validators.required],
      host: [client?.host || '', Validators.required],
      username: [client?.username || ''],
      password: [client?.password || ''],
      urlBase: [client?.urlBase || ''],
      enabled: [client?.enabled ?? true]
    });
    
    // Set up client type change handler
    clientForm.get('type')?.valueChanges.subscribe(() => {
      this.onClientTypeChange(clientForm);
    });
    
    this.clients.push(clientForm);
  }
  
  /**
   * Map frontend client type to backend TypeName and Type
   */
  private mapClientTypeForBackend(frontendType: DownloadClientType): { typeName: string, type: string } {
    switch (frontendType) {
      case DownloadClientType.QBittorrent:
        return { typeName: 'QBittorrent', type: 'Torrent' };
      case DownloadClientType.Deluge:
        return { typeName: 'Deluge', type: 'Torrent' };
      case DownloadClientType.Transmission:
        return { typeName: 'Transmission', type: 'Torrent' };
      case DownloadClientType.Usenet:
        return { typeName: 'Usenet', type: 'Usenet' };
      default:
        return { typeName: 'QBittorrent', type: 'Torrent' };
    }
  }
  
  /**
   * Map backend TypeName to frontend client type
   */
  private mapClientTypeFromBackend(backendTypeName: string): DownloadClientType {
    switch (backendTypeName) {
      case 'QBittorrent':
        return DownloadClientType.QBittorrent;
      case 'Deluge':
        return DownloadClientType.Deluge;
      case 'Transmission':
        return DownloadClientType.Transmission;
      case 'Usenet':
        return DownloadClientType.Usenet;
      default:
        return DownloadClientType.QBittorrent;
    }
  }
  
  /**
   * Remove a client at the specified index
   */
  removeClient(index: number): void {
    const clientForm = this.getClientAsFormGroup(index);
    const clientId = clientForm.get('id')?.value;
    
    // If this is an existing client (has ID), delete it from the backend
    if (clientId) {
      this.downloadClientStore.deleteClient(clientId);
    }
    
    // Remove from the form array
    this.clients.removeAt(index);
    
    // If no clients remain, add an empty one
    if (this.clients.length === 0) {
      this.addClient();
    }
  }

  /**
   * Mark all controls in a form group as touched to trigger validation
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
   * Get a client at the specified index as a FormGroup
   */
  public getClientAsFormGroup(index: number): FormGroup {
    return this.clients.at(index) as FormGroup;
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
    if (!this.clients || !this.clients.controls[clientIndex]) return false;
    
    const control = this.getClientAsFormGroup(clientIndex).get(fieldName);
    return control !== null && control.hasError(errorName) && control.touched;
  }

  /**
   * Custom validator to check if the input is a valid URI
   */
  private uriValidator(control: AbstractControl): ValidationErrors | null {
    if (!control.value) {
      return null; // Let required validator handle empty values
    }
    
    try {
      const url = new URL(control.value);
      
      // Check that we have a valid protocol (http or https)
      if (url.protocol !== 'http:' && url.protocol !== 'https:') {
        return { invalidProtocol: true };
      }
      
      return null; // Valid URI
    } catch (e) {
      return { invalidUri: true }; // Invalid URI
    }
  }

  /**
   * Checks if a client type is Usenet
   */
  public isUsenetClient(clientType: DownloadClientType | null | undefined): boolean {
    return clientType === DownloadClientType.Usenet;
  }

  /**
   * Handle client type changes to update validation
   * @param clientFormGroup The form group containing the client type and host controls
   */
  onClientTypeChange(clientFormGroup: FormGroup): void {
    const clientType = clientFormGroup.get('type')?.value;
    const hostControl = clientFormGroup.get('host');
    
    if (!hostControl) return;
    
    if (this.isUsenetClient(clientType)) {
      // For Usenet, remove all validators
      hostControl.clearValidators();
    } else {
      // For other client types, add required and URI validators
      hostControl.setValidators([
        Validators.required, 
        this.uriValidator.bind(this)
      ]);
    }
    
    // Update validation state
    hostControl.updateValueAndValidity();
  }

}

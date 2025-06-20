import { Component, EventEmitter, OnDestroy, Output, effect, inject, signal } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { Subject, takeUntil } from "rxjs";
import { ConfigurationService } from "../../core/services/configuration.service";
import { CanComponentDeactivate } from "../../core/guards";
import { GeneralConfig } from "../../shared/models/general-config.model";
import { LogEventLevel } from "../../shared/models/log-event-level.enum";
import { CertificateValidationType } from "../../shared/models/certificate-validation-type.enum";

// PrimeNG Components
import { CardModule } from "primeng/card";
import { InputTextModule } from "primeng/inputtext";
import { CheckboxModule } from "primeng/checkbox";
import { ButtonModule } from "primeng/button";
import { InputNumberModule } from "primeng/inputnumber";
import { ToastModule } from "primeng/toast";
import { NotificationService } from '../../core/services/notification.service';
import { SelectModule } from "primeng/select";
import { ChipsModule } from "primeng/chips";
import { AutoCompleteModule } from "primeng/autocomplete";
import { LoadingErrorStateComponent } from "../../shared/components/loading-error-state/loading-error-state.component";

@Component({
  selector: "app-general-settings",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CardModule,
    InputTextModule,
    CheckboxModule,
    ButtonModule,
    InputNumberModule,
    ChipsModule,
    ToastModule,
    SelectModule,
    AutoCompleteModule,
    LoadingErrorStateComponent,
  ],
  templateUrl: "./general-settings.component.html",
  styleUrls: ["./general-settings.component.scss"],
})
export class GeneralSettingsComponent implements OnDestroy, CanComponentDeactivate {
  @Output() saved = new EventEmitter<void>();
  @Output() error = new EventEmitter<string>();

  // General Configuration Form
  generalForm: FormGroup;
  
  // Original form values for tracking changes
  private originalFormValues: any;
  
  // Track whether the form has actual changes compared to original values
  hasActualChanges = false;
  
  // Signals for reactive state
  generalConfig = signal<GeneralConfig | null>(null);
  generalLoading = signal<boolean>(false);
  generalSaving = signal<boolean>(false);
  generalError = signal<string | null>(null);
  
  // Log level options for dropdown
  logLevelOptions = [
    { label: "Verbose", value: LogEventLevel.Verbose },
    { label: "Debug", value: LogEventLevel.Debug },
    { label: "Information", value: LogEventLevel.Information },
    { label: "Warning", value: LogEventLevel.Warning },
    { label: "Error", value: LogEventLevel.Error },
    { label: "Fatal", value: LogEventLevel.Fatal },
  ];
  
  // Certificate validation options for dropdown
  certificateValidationOptions = [
    { label: "Enabled", value: CertificateValidationType.Enabled },
    { label: "Disabled for Local Addresses", value: CertificateValidationType.DisabledForLocalAddresses },
    { label: "Disabled", value: CertificateValidationType.Disabled },
  ];

  // Inject the necessary services
  private formBuilder = inject(FormBuilder);
  private notificationService = inject(NotificationService);
  private configurationService = inject(ConfigurationService);

  // Subject for unsubscribing from observables when component is destroyed
  private destroy$ = new Subject<void>();

  /**
   * Check if component can be deactivated (navigation guard)
   */
  canDeactivate(): boolean {
    return !this.generalForm.dirty;
  }

  constructor() {
    // Initialize the general settings form
    this.generalForm = this.formBuilder.group({
      displaySupportBanner: [true],
      dryRun: [false],
      httpMaxRetries: [0, [Validators.required,Validators.min(0), Validators.max(5)]],
      httpTimeout: [100, [Validators.required, Validators.min(1), Validators.max(100)]],
      httpCertificateValidation: [CertificateValidationType.Enabled],
      searchEnabled: [true],
      searchDelay: [30, [Validators.required, Validators.min(1), Validators.max(300)]],
      logLevel: [LogEventLevel.Information],
      ignoredDownloads: [[]],
    });

    // Load initial configuration
    this.loadGeneralConfig();

    // Setup effect to react to config changes
    effect(() => {
      const config = this.generalConfig();
      if (config) {
        this.generalForm.patchValue(config);
        this.storeOriginalValues();
        this.generalForm.markAsPristine();
        this.hasActualChanges = false;
      }
    });

    // Track form changes for dirty state
    this.generalForm.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.hasActualChanges = this.formValuesChanged();
      });

    // Setup effect to react to error changes
    effect(() => {
      const errorMessage = this.generalError();
      if (errorMessage) {
        this.error.emit(errorMessage);
      }
    });
  }

  /**
   * Load general configuration from the API
   */
  private loadGeneralConfig(): void {
    this.generalLoading.set(true);
    this.generalError.set(null);

    this.configurationService.getGeneralConfig()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (config) => {
          this.generalConfig.set(config);
          this.generalError.set(null);
          this.generalLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading general configuration:', error);
          this.generalError.set(error.message || 'Failed to load general configuration');
          this.generalLoading.set(false);
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
   * Check if the current form values are different from the original values
   */
  private formValuesChanged(): boolean {
    return !this.isEqual(this.generalForm.value, this.originalFormValues);
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
   * Store original form values for dirty checking
   */
  private storeOriginalValues(): void {
    this.originalFormValues = JSON.parse(JSON.stringify(this.generalForm.value));
  }

  /**
   * Save the general configuration
   */
  saveGeneralConfig(): void {
    if (this.generalForm.invalid) {
      this.markFormGroupTouched(this.generalForm);
      this.notificationService.showValidationError();
      return;
    }

    if (!this.hasActualChanges) {
      this.notificationService.showSuccess('No changes detected');
      return;
    }

    const formValues = this.generalForm.value;

    const config: GeneralConfig = {
      displaySupportBanner: formValues.displaySupportBanner,
      dryRun: formValues.dryRun,
      httpMaxRetries: formValues.httpMaxRetries,
      httpTimeout: formValues.httpTimeout,
      httpCertificateValidation: formValues.httpCertificateValidation,
      searchEnabled: formValues.searchEnabled,
      searchDelay: formValues.searchDelay,
      logLevel: formValues.logLevel,
      ignoredDownloads: formValues.ignoredDownloads || [],
    };

    // Set saving state
    this.generalSaving.set(true);
    this.generalError.set(null);

    // Save the configuration
    this.configurationService.updateGeneralConfig(config)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          // Update the stored config
          this.generalConfig.set(config);
          this.generalError.set(null);
          this.generalSaving.set(false);
          
          // Mark form as pristine after successful save
          this.generalForm.markAsPristine();
          this.hasActualChanges = false;
          this.storeOriginalValues();
          
          // Emit saved event and show success message
          this.saved.emit();
          this.notificationService.showSuccess('General configuration saved successfully!');
        },
        error: (error) => {
          console.error('Error saving general configuration:', error);
          this.generalError.set(error.message || 'Failed to save general configuration');
          this.generalSaving.set(false);
        }
      });
  }

  /**
   * Reset the general configuration form to default values
   */
  resetGeneralConfig(): void {  
    this.generalForm.reset({
      displaySupportBanner: true,
      dryRun: false,
      httpMaxRetries: 0,
      httpTimeout: 100,
      httpCertificateValidation: CertificateValidationType.Enabled,
      searchEnabled: true,
      searchDelay: 30,
      logLevel: LogEventLevel.Information,
      ignoredDownloads: [],
    });
    
    // Check if this reset actually changes anything compared to the original state
    const hasChangesAfterReset = this.formValuesChanged();
    
    if (hasChangesAfterReset) {
      // Only mark as dirty if the reset actually changes something
      this.generalForm.markAsDirty();
      this.hasActualChanges = true;
    } else {
      // If reset brings us back to original state, mark as pristine
      this.generalForm.markAsPristine();
      this.hasActualChanges = false;
    }
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
    const control = this.generalForm.get(controlName);
    return control ? control.touched && control.hasError(errorName) : false;
  }
}

import { Component, EventEmitter, OnDestroy, Output, effect, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { Subject, takeUntil } from "rxjs";
import { GeneralConfigStore } from "./general-config.store";
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
import { MessageService } from "primeng/api";
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
  providers: [GeneralConfigStore, MessageService],
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
  private messageService = inject(MessageService);
  private generalConfigStore = inject(GeneralConfigStore);

  // Signals from the store
  readonly generalConfig = this.generalConfigStore.config;
  readonly generalLoading = this.generalConfigStore.loading;
  readonly generalSaving = this.generalConfigStore.saving;
  readonly generalError = this.generalConfigStore.error;

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
      dryRun: [false],
      httpMaxRetries: [0, [Validators.required,Validators.min(0), Validators.max(5)]],
      httpTimeout: [30, [Validators.required, Validators.min(1), Validators.max(100)]],
      httpCertificateValidation: [CertificateValidationType.Enabled],
      searchEnabled: [true],
      searchDelay: [30, [Validators.required, Validators.min(1), Validators.max(300)]],
      logLevel: [LogEventLevel.Information],
      ignoredDownloads: [[]],
    });

    // Setup effect to react to config changes
    effect(() => {
      const config = this.generalConfig();
      if (config) {
        this.generalForm.patchValue(config);
        this.storeOriginalValues();
      }
    });

    // Setup effect to react to error changes
    effect(() => {
      const errorMessage = this.generalError();
      if (errorMessage) {
        this.messageService.add({
          severity: "error",
          summary: "Error",
          detail: errorMessage,
          life: 5000,
        });
        this.error.emit(errorMessage);
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
   * Store original form values for dirty checking
   */
  private storeOriginalValues(): void {
    this.originalFormValues = { ...this.generalForm.value };
  }

  /**
   * Save the general configuration
   */
  saveGeneralConfig(): void {
    if (this.generalForm.invalid) {
      this.markFormGroupTouched(this.generalForm);
      this.messageService.add({
        severity: "error",
        summary: "Validation Error",
        detail: "Please correct the form errors before saving.",
        life: 5000,
      });
      return;
    }

    const formValues = this.generalForm.value;

    const config: GeneralConfig = {
      dryRun: formValues.dryRun,
      httpMaxRetries: formValues.httpMaxRetries,
      httpTimeout: formValues.httpTimeout,
      httpCertificateValidation: formValues.httpCertificateValidation,
      searchEnabled: formValues.searchEnabled,
      searchDelay: formValues.searchDelay,
      logLevel: formValues.logLevel,
      ignoredDownloads: formValues.ignoredDownloads || [],
    };

    // Save the configuration
    this.generalConfigStore.saveConfig(config);
    
    // Setup a one-time check to mark form as pristine after successful save
    const checkSaveCompletion = () => {
      const loading = this.generalSaving();
      const error = this.generalError();
      
      if (!loading && !error) {
        // Mark form as pristine after successful save
        this.generalForm.markAsPristine();
        // Update original values reference
        this.storeOriginalValues();
        // Emit saved event
        this.saved.emit();
        // Show success message
        this.messageService.add({
          severity: "success",
          summary: "Success",
          detail: "General configuration saved successfully.",
          life: 3000,
        });
      } else if (!loading && error) {
        // If there's an error, we can stop checking
      } else {
        // If still loading, check again in a moment
        setTimeout(checkSaveCompletion, 100);
      }
    };
    
    // Start checking for save completion
    checkSaveCompletion();
  }

  /**
   * Reset the general configuration form to default values
   */
  resetGeneralConfig(): void {  
    this.generalForm.reset({
      dryRun: false,
      httpMaxRetries: 0,
      httpTimeout: 100,
      httpCertificateValidation: CertificateValidationType.Enabled,
      searchEnabled: true,
      searchDelay: 30,
      logLevel: LogEventLevel.Information,
      ignoredDownloads: [],
    });
    
    // Mark form as dirty so the save button is enabled after reset
    this.generalForm.markAsDirty();
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

import { Component, EventEmitter, OnDestroy, Output, effect, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { Subject, takeUntil } from "rxjs";
import { QueueCleanerConfigStore } from "./queue-cleaner-config.store";
import {
  QueueCleanerConfig,
  ScheduleUnit,
  BlocklistType,
  FailedImportConfig,
  StalledConfig,
  SlowConfig,
  ContentBlockerConfig,
} from "../../shared/models/queue-cleaner-config.model";
import { SettingsCardComponent } from "../components/settings-card/settings-card.component";
import { ByteSizeInputComponent } from "../../shared/components/byte-size-input/byte-size-input.component";

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
import { MessageService } from "primeng/api";
import { SelectModule } from "primeng/select";
import { AutoCompleteModule } from "primeng/autocomplete";

@Component({
  selector: "app-queue-cleaner-settings",
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
    ByteSizeInputComponent,
    SelectModule,
    AutoCompleteModule,
  ],
  providers: [QueueCleanerConfigStore, MessageService],
  templateUrl: "./queue-cleaner-settings.component.html",
  styleUrls: ["./queue-cleaner-settings.component.scss"],
})
export class QueueCleanerSettingsComponent implements OnDestroy {
  @Output() saved = new EventEmitter<void>();
  @Output() error = new EventEmitter<string>();

  // Queue Cleaner Configuration Form
  queueCleanerForm: FormGroup;

  // Schedule unit options for job schedules
  scheduleUnitOptions = [
    { label: "Seconds", value: ScheduleUnit.Seconds },
    { label: "Minutes", value: ScheduleUnit.Minutes },
    { label: "Hours", value: ScheduleUnit.Hours },
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

  // Track active accordion tabs
  activeAccordionIndices: number[] = [];

  // Subject for unsubscribing from observables when component is destroyed
  private destroy$ = new Subject<void>();

  constructor() {
    // Initialize the queue cleaner form with proper disabled states
    this.queueCleanerForm = this.formBuilder.group({
      enabled: [false],
      jobSchedule: this.formBuilder.group({
        every: [{ value: 5, disabled: true }, [Validators.required, Validators.min(1)]],
        type: [{ value: ScheduleUnit.Minutes, disabled: true }],
      }),
      runSequentially: [{ value: false, disabled: true }],
      ignoredDownloadsPath: [{ value: "", disabled: true }],

      // Failed Import settings - nested group
      failedImport: this.formBuilder.group({
        maxStrikes: [0, [Validators.min(0)]],
        ignorePrivate: [{ value: false, disabled: true }],
        deletePrivate: [{ value: false, disabled: true }],
        ignoredPatterns: [{ value: [], disabled: true }],
      }),

      // Stalled settings - nested group
      stalled: this.formBuilder.group({
        maxStrikes: [0, [Validators.min(0)]],
        resetStrikesOnProgress: [{ value: false, disabled: true }],
        ignorePrivate: [{ value: false, disabled: true }],
        deletePrivate: [{ value: false, disabled: true }],
        downloadingMetadataMaxStrikes: [0, [Validators.min(0)]],
      }),

      // Slow Download settings - nested group
      slow: this.formBuilder.group({
        maxStrikes: [0, [Validators.min(0)]],
        resetStrikesOnProgress: [{ value: false, disabled: true }],
        ignorePrivate: [{ value: false, disabled: true }],
        deletePrivate: [{ value: false, disabled: true }],
        minSpeed: [{ value: "", disabled: true }],
        maxTime: [{ value: 0, disabled: true }],
        ignoreAboveSize: [{ value: "", disabled: true }],
      }),

      // Content Blocker settings - nested group
      contentBlocker: this.formBuilder.group({
        enabled: [{ value: false, disabled: true }],
        ignorePrivate: [{ value: false, disabled: true }],
        deletePrivate: [{ value: false, disabled: true }],
        sonarrBlocklist: this.formBuilder.group({
          path: [{ value: "", disabled: true }],
          type: [{ value: BlocklistType.Blacklist, disabled: true }],
        }),
        radarrBlocklist: this.formBuilder.group({
          path: [{ value: "", disabled: true }],
          type: [{ value: BlocklistType.Blacklist, disabled: true }],
        }),
        lidarrBlocklist: this.formBuilder.group({
          path: [{ value: "", disabled: true }],
          type: [{ value: BlocklistType.Blacklist, disabled: true }],
        }),
      }),
    });

    // Set up form control value change subscriptions to manage dependent control states
    this.setupFormValueChangeListeners();

    // Create an effect to update the form when the configuration changes
    effect(() => {
      const config = this.queueCleanerConfig();
      if (config) {
        // Build form values for the nested configuration structure
        const formValues: any = {
          enabled: config.enabled,
          runSequentially: config.runSequentially,
          ignoredDownloadsPath: config.ignoredDownloadsPath,
        };

        // Add jobSchedule if it exists
        if (config.jobSchedule) {
          formValues.jobSchedule = {
            every: config.jobSchedule.every,
            type: config.jobSchedule.type,
          };
        }

        // Add Failed Import settings
        if (config.failedImport) {
          formValues.failedImport = {
            maxStrikes: config.failedImport.maxStrikes,
            ignorePrivate: config.failedImport.ignorePrivate,
            deletePrivate: config.failedImport.deletePrivate,
            ignoredPatterns: config.failedImport.ignoredPatterns,
          };
        }

        // Add Stalled settings
        if (config.stalled) {
          formValues.stalled = {
            maxStrikes: config.stalled.maxStrikes,
            resetStrikesOnProgress: config.stalled.resetStrikesOnProgress,
            ignorePrivate: config.stalled.ignorePrivate,
            deletePrivate: config.stalled.deletePrivate,
            downloadingMetadataMaxStrikes: config.stalled.downloadingMetadataMaxStrikes,
          };
        }

        // Add Slow Download settings
        if (config.slow) {
          formValues.slow = {
            maxStrikes: config.slow.maxStrikes,
            resetStrikesOnProgress: config.slow.resetStrikesOnProgress,
            ignorePrivate: config.slow.ignorePrivate,
            deletePrivate: config.slow.deletePrivate,
            minSpeed: config.slow.minSpeed,
            maxTime: config.slow.maxTime,
            ignoreAboveSize: config.slow.ignoreAboveSize,
          };
        }

        // Add Content Blocker settings
        if (config.contentBlocker) {
          formValues.contentBlocker = {
            enabled: config.contentBlocker.enabled,
            ignorePrivate: config.contentBlocker.ignorePrivate,
            deletePrivate: config.contentBlocker.deletePrivate,
            sonarrBlocklist: config.contentBlocker.sonarrBlocklist,
            radarrBlocklist: config.contentBlocker.radarrBlocklist,
            lidarrBlocklist: config.contentBlocker.lidarrBlocklist,
          };
        }

        // Update the form with the current configuration
        this.queueCleanerForm.patchValue(formValues);

        // Update form control disabled states based on the configuration
        this.updateFormControlDisabledStates(config);
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
    // Listen to the 'enabled' control changes
    this.queueCleanerForm
      .get("enabled")
      ?.valueChanges.pipe(takeUntil(this.destroy$))
      .subscribe((enabled) => {
        this.updateMainControlsState(enabled);

        // When disabled, close all accordions
        if (!enabled) {
          this.activeAccordionIndices = [];
        }
      });

    // Failed import settings
    this.queueCleanerForm
      .get("failedImport.maxStrikes")
      ?.valueChanges.pipe(takeUntil(this.destroy$))
      .subscribe((strikes) => {
        this.updateFailedImportDependentControls(strikes);
      });

    // Stalled settings
    this.queueCleanerForm
      .get("stalled.maxStrikes")
      ?.valueChanges.pipe(takeUntil(this.destroy$))
      .subscribe((strikes) => {
        this.updateStalledDependentControls(strikes);
      });

    // Slow downloads settings
    this.queueCleanerForm
      .get("slow.maxStrikes")
      ?.valueChanges.pipe(takeUntil(this.destroy$))
      .subscribe((strikes) => {
        this.updateSlowDependentControls(strikes);
      });

    // Content blocker settings
    this.queueCleanerForm
      .get("enabled")
      ?.valueChanges.pipe(takeUntil(this.destroy$))
      .subscribe((enabled) => {
        if (enabled) {
          this.queueCleanerForm.get("contentBlocker.enabled")?.enable();
        } else {
          this.queueCleanerForm.get("contentBlocker.enabled")?.disable();
        }
      });

    // Update content blocker dependent controls when enabled changes
    this.queueCleanerForm
      .get("contentBlocker.enabled")
      ?.valueChanges.pipe(takeUntil(this.destroy$))
      .subscribe((enabled) => {
        this.updateContentBlockerDependentControls(enabled);
      });
  }

  /**
   * Update form control disabled states based on the configuration
   */
  private updateFormControlDisabledStates(config: QueueCleanerConfig): void {
    // Update main form controls based on the 'enabled' state
    this.updateMainControlsState(config.enabled);

    // Check if failed import strikes are set and update dependent controls
    if (config.failedImport?.maxStrikes !== undefined) {
      this.updateFailedImportDependentControls(config.failedImport.maxStrikes);
    }

    // Check if stalled strikes are set and update dependent controls
    if (config.stalled?.maxStrikes !== undefined) {
      this.updateStalledDependentControls(config.stalled.maxStrikes);
    }

    // Check if slow download strikes are set and update dependent controls
    if (config.slow?.maxStrikes !== undefined) {
      this.updateSlowDependentControls(config.slow.maxStrikes);
    }

    // Check if content blocker is enabled and update dependent controls
    if (config.contentBlocker?.enabled !== undefined) {
      this.updateContentBlockerDependentControls(config.contentBlocker.enabled);
    }
  }

  /**
   * Update the state of main controls based on the 'enabled' control value
   */
  private updateMainControlsState(enabled: boolean): void {
    // Common controls
    const jobScheduleGroup = this.queueCleanerForm.get("jobSchedule") as FormGroup;

    if (enabled) {
      jobScheduleGroup.get("every")?.enable({ emitEvent: false });
      jobScheduleGroup.get("type")?.enable({ emitEvent: false });
      this.queueCleanerForm.get("runSequentially")?.enable({ emitEvent: false });
      this.queueCleanerForm.get("ignoredDownloadsPath")?.enable({ emitEvent: false });

      // Update individual config sections only if they are enabled
      const failedImportMaxStrikes = this.queueCleanerForm.get("failedImport.maxStrikes")?.value;
      const stalledMaxStrikes = this.queueCleanerForm.get("stalled.maxStrikes")?.value;
      const slowMaxStrikes = this.queueCleanerForm.get("slow.maxStrikes")?.value;
      const contentBlockerEnabled = this.queueCleanerForm.get("contentBlocker.enabled")?.value;

      this.updateFailedImportDependentControls(failedImportMaxStrikes);
      this.updateStalledDependentControls(stalledMaxStrikes);
      this.updateSlowDependentControls(slowMaxStrikes);
      this.updateContentBlockerDependentControls(contentBlockerEnabled);
    } else {
      jobScheduleGroup.get("every")?.disable({ emitEvent: false });
      jobScheduleGroup.get("type")?.disable({ emitEvent: false });
      this.queueCleanerForm.get("runSequentially")?.disable({ emitEvent: false });
      this.queueCleanerForm.get("ignoredDownloadsPath")?.disable({ emitEvent: false });

      // Save current active accordion state before clearing it
      // This will be empty when we collapse all accordions
      this.activeAccordionIndices = [];
    }
  }

  /**
   * Update the state of Failed Import dependent controls based on the 'maxStrikes' value
   */
  private updateFailedImportDependentControls(strikes: number): void {
    const enable = strikes >= 3;
    const options = { onlySelf: true };

    if (enable) {
      this.queueCleanerForm.get("failedImport")?.get("ignorePrivate")?.enable(options);
      this.queueCleanerForm.get("failedImport")?.get("deletePrivate")?.enable(options);
      this.queueCleanerForm.get("failedImport")?.get("ignoredPatterns")?.enable(options);
    } else {
      this.queueCleanerForm.get("failedImport")?.get("ignorePrivate")?.disable(options);
      this.queueCleanerForm.get("failedImport")?.get("deletePrivate")?.disable(options);
      this.queueCleanerForm.get("failedImport")?.get("ignoredPatterns")?.disable(options);
    }
  }

  /**
   * Update the state of Stalled dependent controls based on the 'maxStrikes' value
   */
  private updateStalledDependentControls(strikes: number): void {
    const enable = strikes >= 3;
    const options = { onlySelf: true };

    if (enable) {
      this.queueCleanerForm.get("stalled")?.get("resetStrikesOnProgress")?.enable(options);
      this.queueCleanerForm.get("stalled")?.get("ignorePrivate")?.enable(options);
      this.queueCleanerForm.get("stalled")?.get("deletePrivate")?.enable(options);
    } else {
      this.queueCleanerForm.get("stalled")?.get("resetStrikesOnProgress")?.disable(options);
      this.queueCleanerForm.get("stalled")?.get("ignorePrivate")?.disable(options);
      this.queueCleanerForm.get("stalled")?.get("deletePrivate")?.disable(options);
    }
  }

  /**
   * Update the state of Slow Download dependent controls based on the 'maxStrikes' value
   */
  private updateSlowDependentControls(strikes: number): void {
    const enable = strikes >= 3;
    const options = { onlySelf: true };

    if (enable) {
      this.queueCleanerForm.get("slow")?.get("resetStrikesOnProgress")?.enable(options);
      this.queueCleanerForm.get("slow")?.get("ignorePrivate")?.enable(options);
      this.queueCleanerForm.get("slow")?.get("deletePrivate")?.enable(options);
      this.queueCleanerForm.get("slow")?.get("minSpeed")?.enable(options);
      this.queueCleanerForm.get("slow")?.get("maxTime")?.enable(options);
      this.queueCleanerForm.get("slow")?.get("ignoreAboveSize")?.enable(options);
    } else {
      this.queueCleanerForm.get("slow")?.get("resetStrikesOnProgress")?.disable(options);
      this.queueCleanerForm.get("slow")?.get("ignorePrivate")?.disable(options);
      this.queueCleanerForm.get("slow")?.get("deletePrivate")?.disable(options);
      this.queueCleanerForm.get("slow")?.get("minSpeed")?.disable(options);
      this.queueCleanerForm.get("slow")?.get("maxTime")?.disable(options);
      this.queueCleanerForm.get("slow")?.get("ignoreAboveSize")?.disable(options);
    }
  }

  /**
   * Update the state of Content Blocker dependent controls based on the 'enabled' value
   */
  private updateContentBlockerDependentControls(enabled: boolean): void {
    const options = { onlySelf: true };

    if (enabled) {
      // Enable blocklist settings
      this.queueCleanerForm.get("contentBlocker")?.get("ignorePrivate")?.enable(options);
      this.queueCleanerForm.get("contentBlocker")?.get("deletePrivate")?.enable(options);

      // Enable Sonarr blocklist settings
      this.queueCleanerForm.get("contentBlocker")?.get("sonarrBlocklist")?.get("path")?.enable(options);
      this.queueCleanerForm.get("contentBlocker")?.get("sonarrBlocklist")?.get("type")?.enable(options);

      // Enable Radarr blocklist settings
      this.queueCleanerForm.get("contentBlocker")?.get("radarrBlocklist")?.get("path")?.enable(options);
      this.queueCleanerForm.get("contentBlocker")?.get("radarrBlocklist")?.get("type")?.enable(options);

      // Enable Lidarr blocklist settings
      this.queueCleanerForm.get("contentBlocker")?.get("lidarrBlocklist")?.get("path")?.enable(options);
      this.queueCleanerForm.get("contentBlocker")?.get("lidarrBlocklist")?.get("type")?.enable(options);
    } else {
      // Disable blocklist settings
      this.queueCleanerForm.get("contentBlocker")?.get("ignorePrivate")?.disable(options);
      this.queueCleanerForm.get("contentBlocker")?.get("deletePrivate")?.disable(options);

      // Disable Sonarr blocklist settings
      this.queueCleanerForm.get("contentBlocker")?.get("sonarrBlocklist")?.get("path")?.disable(options);
      this.queueCleanerForm.get("contentBlocker")?.get("sonarrBlocklist")?.get("type")?.disable(options);

      // Disable Radarr blocklist settings
      this.queueCleanerForm.get("contentBlocker")?.get("radarrBlocklist")?.get("path")?.disable(options);
      this.queueCleanerForm.get("contentBlocker")?.get("radarrBlocklist")?.get("type")?.disable(options);

      // Disable Lidarr blocklist settings
      this.queueCleanerForm.get("contentBlocker")?.get("lidarrBlocklist")?.get("path")?.disable(options);
      this.queueCleanerForm.get("contentBlocker")?.get("lidarrBlocklist")?.get("type")?.disable(options);
    }
  }

  /**
   * Save the queue cleaner configuration
   */
  saveQueueCleanerConfig(): void {
    if (this.queueCleanerForm.invalid) {
      // Mark all fields as touched to show validation errors
      this.markFormGroupTouched(this.queueCleanerForm);
      this.messageService.add({
        severity: "error",
        summary: "Validation Error",
        detail: "Please correct the errors in the form before saving.",
        life: 5000,
      });
      return;
    }

    // Get the form values
    const formValues = this.queueCleanerForm.getRawValue(); // Get values including disabled fields

    // Build the configuration object with nested structure
    const config: QueueCleanerConfig = {
      enabled: formValues.enabled,
      // The cronExpression will be generated from the jobSchedule when saving
      cronExpression: "",
      jobSchedule: formValues.jobSchedule,
      runSequentially: formValues.runSequentially,
      ignoredDownloadsPath: formValues.ignoredDownloadsPath || "",

      failedImport: {
        maxStrikes: formValues.failedImport?.maxStrikes || 0,
        ignorePrivate: formValues.failedImport?.ignorePrivate || false,
        deletePrivate: formValues.failedImport?.deletePrivate || false,
        ignoredPatterns: formValues.failedImport?.ignoredPatterns || [],
      },

      stalled: {
        maxStrikes: formValues.stalled?.maxStrikes || 0,
        resetStrikesOnProgress: formValues.stalled?.resetStrikesOnProgress || false,
        ignorePrivate: formValues.stalled?.ignorePrivate || false,
        deletePrivate: formValues.stalled?.deletePrivate || false,
        downloadingMetadataMaxStrikes: formValues.stalled?.downloadingMetadataMaxStrikes || 0,
      },

      slow: {
        maxStrikes: formValues.slow?.maxStrikes || 0,
        resetStrikesOnProgress: formValues.slow?.resetStrikesOnProgress || false,
        ignorePrivate: formValues.slow?.ignorePrivate || false,
        deletePrivate: formValues.slow?.deletePrivate || false,
        minSpeed: formValues.slow?.minSpeed || "",
        maxTime: formValues.slow?.maxTime || 0,
        ignoreAboveSize: formValues.slow?.ignoreAboveSize || "",
      },

      contentBlocker: {
        enabled: formValues.contentBlocker?.enabled || false,
        ignorePrivate: formValues.contentBlocker?.ignorePrivate || false,
        deletePrivate: formValues.contentBlocker?.deletePrivate || false,
        sonarrBlocklist: formValues.contentBlocker?.sonarrBlocklist || {
          path: "",
          type: BlocklistType.Blacklist,
        },
        radarrBlocklist: formValues.contentBlocker?.radarrBlocklist || {
          path: "",
          type: BlocklistType.Blacklist,
        },
        lidarrBlocklist: formValues.contentBlocker?.lidarrBlocklist || {
          path: "",
          type: BlocklistType.Blacklist,
        },
      },
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
        type: ScheduleUnit.Minutes,
      },
      runSequentially: false,
      ignoredDownloadsPath: "",

      // Failed Import settings (nested)
      failedImport: {
        maxStrikes: 0,
        ignorePrivate: false,
        deletePrivate: false,
        ignoredPatterns: [],
      },

      // Stalled settings (nested)
      stalled: {
        maxStrikes: 0,
        resetStrikesOnProgress: false,
        ignorePrivate: false,
        deletePrivate: false,
        downloadingMetadataMaxStrikes: 0,
      },

      // Slow Download settings (nested)
      slow: {
        maxStrikes: 0,
        resetStrikesOnProgress: false,
        ignorePrivate: false,
        deletePrivate: false,
        minSpeed: "",
        maxTime: 0,
        ignoreAboveSize: "",
      },

      // Content Blocker settings (nested)
      contentBlocker: {
        enabled: false,
        ignorePrivate: false,
        deletePrivate: false,
        sonarrBlocklist: {
          path: "",
          type: BlocklistType.Blacklist,
        },
        radarrBlocklist: {
          path: "",
          type: BlocklistType.Blacklist,
        },
        lidarrBlocklist: {
          path: "",
          type: BlocklistType.Blacklist,
        },
      },
    });

    // Manually update control states after reset
    this.updateMainControlsState(false);
    this.updateFailedImportDependentControls(0);
    this.updateStalledDependentControls(0);
    this.updateSlowDependentControls(0);
    this.updateContentBlockerDependentControls(false);
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

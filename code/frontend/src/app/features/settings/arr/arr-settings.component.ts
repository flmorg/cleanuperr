import { Component, ChangeDetectionStrategy, inject, signal, input, computed, effect, untracked } from '@angular/core';
import { form, required, validate, FormField } from '@angular/forms/signals';
import { PageHeaderComponent } from '@layout/page-header/page-header.component';
import {
  CardComponent, ButtonComponent, InputComponent, ToggleComponent, NumberInputComponent,
  SelectComponent, ModalComponent, EmptyStateComponent, BadgeComponent, LoadingStateComponent,
  type SelectOption,
} from '@ui';
import { ArrApi } from '@core/api/arr.api';
import { ApiError } from '@core/interceptors/error.interceptor';
import { ToastService } from '@core/services/toast.service';
import { ConfirmService } from '@core/services/confirm.service';
import { ArrInstance, CreateArrInstanceDto, TestArrInstanceRequest } from '@shared/models/arr-config.model';
import { ArrType } from '@shared/models/enums';
import { HasPendingChanges } from '@core/guards/pending-changes.guard';
import { createSettingsResource, saveSettings } from '@shared/utils/settings-resource.util';
import { createDirtyTracker } from '@shared/utils/dirty-tracker.util';

const ARR_VERSION_OPTIONS: Record<string, SelectOption[]> = {
  sonarr:  [{ label: 'v4', value: 4 }],
  radarr:  [{ label: 'v6', value: 6 }],
  lidarr:  [{ label: 'v3', value: 3 }],
  readarr: [{ label: 'v0.4', value: 0.4 }],
  whisparr: [{ label: 'v2', value: 2 }, { label: 'v3', value: 3 }],
  sportarr: [{ label: 'v3', value: 3 }],
  lazylibrarian: [{ label: 'latest', value: 1 }],
};

const EXPERIMENTAL_TYPES = new Set(['lazylibrarian']);

const ARR_DEFAULT_PORT: Record<string, number> = {
  sonarr: 8989,
  radarr: 7878,
  lidarr: 8686,
  readarr: 8787,
  whisparr: 6969,
  sportarr: 1867,
  lazylibrarian: 5299,
};

interface ArrInstanceFormModel {
  name: string;
  url: string;
  externalUrl: string;
  apiKey: string;
  version: number;
  enabled: boolean;
}

interface FailedImportFormModel {
  override: boolean;
  maxStrikes: number | null;
}

@Component({
  selector: 'app-arr-settings',
  standalone: true,
  imports: [
    PageHeaderComponent, CardComponent, ButtonComponent, InputComponent,
    ToggleComponent, NumberInputComponent, SelectComponent, ModalComponent, EmptyStateComponent,
    BadgeComponent, LoadingStateComponent, FormField,
  ],
  templateUrl: './arr-settings.component.html',
  styleUrl: './arr-settings.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ArrSettingsComponent implements HasPendingChanges {
  private readonly api = inject(ArrApi);
  private readonly toast = inject(ToastService);
  private readonly confirmService = inject(ConfirmService);

  readonly type = input.required<string>();
  readonly displayName = computed(() => {
    const t = this.type();
    if (t === 'lazylibrarian') {
      return 'LazyLibrarian';
    }
    return t.charAt(0).toUpperCase() + t.slice(1);
  });
  readonly badge = computed(() => (EXPERIMENTAL_TYPES.has(this.type()) ? 'Experimental' : ''));
  readonly versionOptions = computed(() => ARR_VERSION_OPTIONS[this.type()] ?? []);
  readonly urlPlaceholder = computed(() => `http://localhost:${ARR_DEFAULT_PORT[this.type()] ?? 8989}`);
  readonly externalUrlPlaceholder = computed(() => `https://${this.type()}.example.com`);

  private readonly settings = createSettingsResource({
    params: () => this.type(),
    load: (type) => this.api.getConfig(type as ArrType),
    errorMessage: () => `Failed to load ${this.displayName()} settings`,
  });
  private readonly configResource = this.settings.resource;

  readonly loader = this.settings.loader;
  readonly loadError = this.settings.loadError;
  readonly saving = signal(false);
  readonly instances = computed(() =>
    this.configResource.hasValue() ? (this.configResource.value().instances ?? []) : [],
  );

  // Failed import override
  readonly failedImportSaving = signal(false);
  readonly failedImportSaved = signal(false);
  readonly failedImportModel = signal<FailedImportFormModel>({ override: false, maxStrikes: 3 });
  readonly failedImportForm = form(this.failedImportModel, (p) => {
    validate(p.maxStrikes, ({ value, valueOf }) => {
      if (!valueOf(p.override)) {
        return undefined;
      }
      if (value() === null) {
        return { kind: 'required', message: 'This field is required' };
      }
      if (value()! < 0) {
        return { kind: 'min', message: 'Value cannot be negative' };
      }
      if (value()! > 0 && value()! < 3) {
        return { kind: 'min', message: 'Minimum value is 3' };
      }
      if (value()! > 5000) {
        return { kind: 'max', message: 'Value cannot exceed 5000' };
      }
      return undefined;
    });
  });
  readonly failedImportHasErrors = computed(() => this.failedImportForm().invalid());

  private readonly failedImportDirtyTracker = createDirtyTracker(
    this.failedImportModel,
    (m) => (m.override ? m.maxStrikes : -1),
  );
  readonly failedImportDirty = this.failedImportDirtyTracker.dirty;

  // Modal state
  readonly modalVisible = signal(false);
  readonly editingInstance = signal<ArrInstance | null>(null);
  readonly testing = signal(false);

  readonly instanceModel = signal<ArrInstanceFormModel>({
    name: '', url: '', externalUrl: '', apiKey: '', version: 3, enabled: true,
  });
  readonly instanceForm = form(this.instanceModel, (p) => {
    required(p.name, { message: 'Name is required' });
    required(p.url, { message: 'URL is required' });
    required(p.apiKey, { message: 'API key is required' });
  });

  readonly hasModalErrors = computed(() => this.instanceForm().invalid());

  /** JSON snapshot of the model as loaded when the modal opened, for dirty tracking. */
  private readonly openSnapshot = signal('');
  private readonly modalDirty = computed(() =>
    this.modalVisible() && JSON.stringify(this.instanceModel()) !== this.openSnapshot());

  constructor() {
    effect(() => {
      const options = this.versionOptions();
      if (options.length > 0) {
        untracked(() => this.instanceModel.update(m => ({ ...m, version: options[0].value as number })));
      }
    });

    effect(() => {
      if (!this.configResource.hasValue()) {
        return;
      }
      const config = this.configResource.value();
      untracked(() => {
        if (this.failedImportDirty()) {
          return;
        }
        this.failedImportModel.set(
          config.failedImportMaxStrikes >= 0
            ? { override: true, maxStrikes: config.failedImportMaxStrikes }
            : { override: false, maxStrikes: 3 },
        );
        this.failedImportDirtyTracker.markSaved();
      });
    });
  }

  retry(): void {
    this.settings.retry();
  }

  openAddModal(): void {
    this.editingInstance.set(null);
    const options = this.versionOptions();
    this.instanceModel.set({
      name: '', url: '', externalUrl: '', apiKey: '',
      version: options.length > 0 ? (options[0].value as number) : 3,
      enabled: true,
    });
    this.openSnapshot.set(JSON.stringify(this.instanceModel()));
    this.modalVisible.set(true);
  }

  openEditModal(instance: ArrInstance): void {
    this.editingInstance.set(instance);
    this.instanceModel.set({
      name: instance.name,
      url: instance.url,
      externalUrl: instance.externalUrl ?? '',
      apiKey: instance.apiKey,
      version: instance.version,
      enabled: instance.enabled,
    });
    this.openSnapshot.set(JSON.stringify(this.instanceModel()));
    this.modalVisible.set(true);
  }

  testConnection(): void {
    const m = this.instanceModel();
    const request: TestArrInstanceRequest = {
      url: m.url,
      apiKey: m.apiKey,
      version: m.version ?? 3,
      instanceId: this.editingInstance()?.id,
    };
    this.testing.set(true);
    this.api.testInstance(this.type() as ArrType, request).subscribe({
      next: (result) => {
        this.toast.success(result.message || 'Connection successful');
        this.testing.set(false);
      },
      error: (err: ApiError) => {
        this.toast.error(err.message);
        this.testing.set(false);
      },
    });
  }

  saveInstance(): void {
    if (this.instanceForm().invalid()) {
      return;
    }
    const m = this.instanceModel();
    const dto: CreateArrInstanceDto = {
      name: m.name,
      url: m.url,
      externalUrl: m.externalUrl || undefined,
      apiKey: m.apiKey,
      version: m.version ?? 3,
      enabled: m.enabled,
    };

    this.saving.set(true);
    const editing = this.editingInstance();
    const obs = editing?.id
      ? this.api.updateInstance(this.type() as ArrType, editing.id, dto)
      : this.api.createInstance(this.type() as ArrType, dto);

    obs.subscribe({
      next: () => {
        this.toast.success(editing ? 'Instance updated' : 'Instance added');
        this.modalVisible.set(false);
        this.saving.set(false);
        this.configResource.reload();
      },
      error: (err: ApiError) => {
        this.toast.error(err.message);
        this.saving.set(false);
      },
    });
  }

  async deleteInstance(instance: ArrInstance): Promise<void> {
    if (!instance.id) return;
    const confirmed = await this.confirmService.confirm({
      title: 'Delete Instance',
      message: `Are you sure you want to delete "${instance.name}"? This action cannot be undone.`,
      confirmLabel: 'Delete',
      destructive: true,
    });
    if (!confirmed) return;

    this.api.deleteInstance(this.type() as ArrType, instance.id).subscribe({
      next: () => {
        this.toast.success('Instance deleted');
        this.configResource.reload();
      },
      error: (err: ApiError) => this.toast.error(err.message),
    });
  }

  saveFailedImport(): void {
    if (this.failedImportForm().invalid()) {
      return;
    }
    const m = this.failedImportModel();
    const maxStrikes = m.override ? (m.maxStrikes ?? 3) : -1;

    saveSettings({
      request: this.api.updateConfig(this.type() as ArrType, { failedImportMaxStrikes: maxStrikes }),
      toast: this.toast,
      saving: this.failedImportSaving,
      saved: this.failedImportSaved,
      successMessage: 'Failed import settings saved',
      onSaved: () => this.failedImportDirtyTracker.markSaved(m),
    });
  }

  hasPendingChanges(): boolean {
    return this.modalDirty() || this.failedImportDirty();
  }
}

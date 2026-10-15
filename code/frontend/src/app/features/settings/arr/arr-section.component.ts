import {
  Component, ChangeDetectionStrategy, inject, signal, input, output, computed, effect, untracked,
} from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { form, validate, FormField } from '@angular/forms/signals';
import {
  CardComponent, ButtonComponent, NumberInputComponent, ToggleComponent, BadgeComponent,
} from '@ui';
import { ArrApi } from '@core/api/arr.api';
import { ToastService } from '@core/services/toast.service';
import { ThemeService } from '@core/services/theme.service';
import { ArrInstance, ArrConfig } from '@shared/models/arr-config.model';
import type { ArrType } from '@shared/models/enums';
import { createDirtyTracker } from '@shared/utils/dirty-tracker.util';
import { saveSettings } from '@shared/utils/settings-resource.util';
import { ARR_APP_META } from './arr-app-meta';

interface FailedImportFormModel {
  override: boolean;
  maxStrikes: number | null;
}

@Component({
  selector: 'app-arr-section',
  standalone: true,
  imports: [
    CardComponent, ButtonComponent, NumberInputComponent, ToggleComponent, BadgeComponent,
    NgIcon, FormField,
  ],
  host: {
    '[attr.id]': 'type()',
  },
  templateUrl: './arr-section.component.html',
  styleUrl: './arr-section.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ArrSectionComponent {
  private readonly api = inject(ArrApi);
  private readonly toast = inject(ToastService);
  private readonly themeService = inject(ThemeService);

  readonly type = input.required<ArrType>();
  readonly config = input.required<ArrConfig>();

  readonly add = output<void>();
  readonly edit = output<ArrInstance>();
  readonly delete = output<ArrInstance>();

  readonly meta = computed(() => ARR_APP_META[this.type()]);
  readonly instances = computed(() => this.config().instances ?? []);

  readonly strikesOpen = signal(false);

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
      if (!Number.isInteger(value())) {
        return { kind: 'integer', message: 'Value must be a whole number' };
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
  private seededType: ArrType | null = null;

  readonly panelVisible = computed(() =>
    this.strikesOpen() || this.failedImportDirty() || this.failedImportHasErrors(),
  );

  constructor() {
    effect(() => {
      const type = this.type();
      const config = this.config();
      untracked(() => {
        if (this.seededType === type && this.failedImportDirty()) {
          return;
        }
        this.seededType = type;
        this.failedImportModel.set(
          config.failedImportMaxStrikes >= 0
            ? { override: true, maxStrikes: config.failedImportMaxStrikes }
            : { override: false, maxStrikes: 3 },
        );
        this.failedImportDirtyTracker.markSaved();
      });
    });
  }

  themedIconSrc(src: string): string {
    return this.themeService.themedIconSrc(src);
  }

  toggleStrikesPanel(): void {
    this.strikesOpen.update((v) => !v);
  }

  saveFailedImport(): void {
    if (this.failedImportForm().invalid()) {
      return;
    }
    const m = this.failedImportModel();
    const maxStrikes = m.override ? (m.maxStrikes ?? 3) : -1;

    saveSettings({
      request: this.api.updateConfig(this.type(), { failedImportMaxStrikes: maxStrikes }),
      toast: this.toast,
      saving: this.failedImportSaving,
      saved: this.failedImportSaved,
      successMessage: 'Failed import settings saved',
      onSaved: () => this.failedImportDirtyTracker.markSaved(m),
    });
  }

  onAddClick(): void {
    this.add.emit();
  }

  onEditClick(instance: ArrInstance): void {
    this.edit.emit(instance);
  }

  onDeleteClick(instance: ArrInstance): void {
    this.delete.emit(instance);
  }

  hasPendingChanges(): boolean {
    return this.failedImportDirty();
  }
}

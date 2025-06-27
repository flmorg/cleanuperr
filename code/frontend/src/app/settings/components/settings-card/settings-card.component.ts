import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardModule } from 'primeng/card';

@Component({
  selector: 'app-settings-card',
  standalone: true,
  imports: [CommonModule, CardModule],
  template: `
    <p-card [header]="title" [styleClass]="styleClass">
      <ng-content></ng-content>
      <ng-template pTemplate="footer" *ngIf="showActions">
        <div class="settings-card-actions">
          <ng-content select="[actions]"></ng-content>
        </div>
      </ng-template>
    </p-card>
  `,
  styles: [`
    :host {
      display: block;
      margin-bottom: 1.5rem;
    }

    .settings-card-actions {
      display: flex;
      justify-content: flex-start;
      gap: 0.5rem;
      margin-top: 1rem;
    }
  `]
})
export class SettingsCardComponent {
  @Input() title: string = '';
  @Input() styleClass: string = 'settings-card';
  @Input() showActions: boolean = true;
}

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

// PrimeNG Components
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { InputSwitchModule } from 'primeng/inputswitch';
import { ButtonModule } from 'primeng/button';
import { AccordionModule } from 'primeng/accordion';

@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CardModule,
    InputTextModule,
    InputSwitchModule,
    ButtonModule,
    AccordionModule
  ],
  templateUrl: './settings-page.component.html',
  styleUrl: './settings-page.component.scss'
})
export class SettingsPageComponent {
  // Sample settings
  apiUrl = 'http://localhost:5000';
  enableLogs = true;
  enableNotifications = true;
  autoRefresh = false;
  refreshInterval = 30;
}

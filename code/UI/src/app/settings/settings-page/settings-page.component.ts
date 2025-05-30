import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { environment } from '../../../environments/environment';

// PrimeNG Components
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { ButtonModule } from 'primeng/button';
import { MessageService } from 'primeng/api';

// Custom Components and Services
import { QueueCleanerSettingsComponent } from '../queue-cleaner/queue-cleaner-settings.component';
import { SettingsCardComponent } from '../components/settings-card/settings-card.component';

// Define interfaces for settings page
interface LogLevel {
  name: string;
  value: string;
}

interface Category {
  name: string;
  code: string;
}

@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    CardModule,
    ButtonModule,
    ToastModule,
    SettingsCardComponent,
    QueueCleanerSettingsComponent
  ],
  providers: [MessageService],
  templateUrl: './settings-page.component.html',
  styleUrl: './settings-page.component.scss'
})
export class SettingsPageComponent implements OnInit {
  logLevels: LogLevel[] = [
    { name: 'Trace', value: 'trace' },
    { name: 'Debug', value: 'debug' },
    { name: 'Information', value: 'information' },
    { name: 'Warning', value: 'warning' },
    { name: 'Error', value: 'error' },
    { name: 'Critical', value: 'critical' },
    { name: 'None', value: 'none' }
  ];

  categories: Category[] = [
    { name: 'All', code: '*' },
    { name: 'System', code: 'SYS' },
    { name: 'Commands', code: 'CMD' },
    { name: 'Database', code: 'DB' },
    { name: 'Network', code: 'NET' },
    { name: 'Jobs', code: 'JOBS' },
    { name: 'Imports', code: 'IMPORTS' },
    { name: 'Media', code: 'MEDIA' }
  ];
  
  // API URLs from environment
  private apiUrl = environment.apiUrl;
  
  // Services
  private messageService = inject(MessageService);
  
  ngOnInit(): void {
    // Future implementation for other settings sections
  }
}

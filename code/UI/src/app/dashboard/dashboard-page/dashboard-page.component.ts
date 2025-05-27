import { Component, OnInit, OnDestroy, signal, computed, inject } from '@angular/core';
import { CommonModule, NgClass, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';

// PrimeNG Components
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { ProgressSpinnerModule } from 'primeng/progressspinner';

// Services & Models
import { LogHubService } from '../../core/services/log-hub.service';
import { EventHubService } from '../../core/services/event-hub.service';
import { LogEntry } from '../../core/models/signalr.models';
import { Event as EventModel } from '../../core/models/event.models';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [
    CommonModule,
    NgClass,
    RouterLink,
    DatePipe,
    CardModule,
    ButtonModule,
    TagModule,
    TooltipModule,
    ProgressSpinnerModule
  ],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss'
})
export class DashboardPageComponent implements OnInit, OnDestroy {
  private logHubService = inject(LogHubService);
  private eventHubService = inject(EventHubService);
  private destroy$ = new Subject<void>();

  // Signals for reactive state
  recentLogs = signal<LogEntry[]>([]);
  recentEvents = signal<EventModel[]>([]);
  logsConnected = signal<boolean>(false);
  eventsConnected = signal<boolean>(false);

  // Computed values for display
  displayLogs = computed(() => {
    return this.recentLogs()
      .sort((a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime()) // Sort chronologically (oldest first)
      .slice(-5); // Take the last 5 (most recent)
  });
  
  displayEvents = computed(() => {
    return this.recentEvents()
      .sort((a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime()) // Sort chronologically (oldest first)
      .slice(-5); // Take the last 5 (most recent)
  });

  ngOnInit() {
    this.initializeLogHub();
    this.initializeEventHub();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeLogHub(): void {
    // Connect to logs hub
    this.logHubService.startConnection()
      .catch((error: Error) => console.error('Failed to connect to log hub:', error));

    // Subscribe to logs
    this.logHubService.getLogs()
      .pipe(takeUntil(this.destroy$))
      .subscribe((logs: LogEntry[]) => {
        this.recentLogs.set(logs);
      });

    // Subscribe to connection status
    this.logHubService.getConnectionStatus()
      .pipe(takeUntil(this.destroy$))
      .subscribe((status: boolean) => {
        this.logsConnected.set(status);
      });
  }

  private initializeEventHub(): void {
    // Connect to events hub
    this.eventHubService.startConnection()
      .catch((error: Error) => console.error('Failed to connect to event hub:', error));

    // Subscribe to events
    this.eventHubService.getEvents()
      .pipe(takeUntil(this.destroy$))
      .subscribe((events: EventModel[]) => {
        this.recentEvents.set(events);
      });

    // Subscribe to connection status
    this.eventHubService.getConnectionStatus()
      .pipe(takeUntil(this.destroy$))
      .subscribe((status: boolean) => {
        this.eventsConnected.set(status);
      });
  }



  // Log-related methods
  getLogIcon(level: string): string {
    const normalizedLevel = level?.toLowerCase() || '';
    
    switch (normalizedLevel) {
      case 'error':
      case 'fatal':
      case 'critical':
        return 'pi pi-times-circle';
      case 'warning':
        return 'pi pi-exclamation-triangle';
      case 'information':
      case 'info':
        return 'pi pi-info-circle';
      case 'debug':
      case 'trace':
      case 'verbose':
        return 'pi pi-bug';
      default:
        return 'pi pi-circle';
    }
  }

  getLogIconClass(level: string): string {
    const normalizedLevel = level?.toLowerCase() || '';
    
    switch (normalizedLevel) {
      case 'error':
      case 'fatal':
      case 'critical':
        return 'log-icon-error';
      case 'warning':
        return 'log-icon-warning';
      case 'information':
      case 'info':
        return 'log-icon-info';
      case 'debug':
      case 'trace':
      case 'verbose':
        return 'log-icon-debug';
      default:
        return 'log-icon-default';
    }
  }

  getLogSeverity(level: string): string {
    const normalizedLevel = level?.toLowerCase() || '';
    
    switch (normalizedLevel) {
      case 'error':
      case 'fatal':
      case 'critical':
        return 'danger';
      case 'warning':
        return 'warn';
      case 'information':
      case 'info':
        return 'info';
      case 'debug':
      case 'trace':
      case 'verbose':
        return 'success';
      default:
        return 'secondary';
    }
  }

  // Event-related methods
  getEventIcon(eventType: string): string {
    const normalizedType = eventType?.toLowerCase() || '';
    
    if (normalizedType.includes('strike')) {
      return 'pi pi-bolt';
    }
    
    switch (normalizedType) {
      case 'downloadingmetadatastrike':
      case 'failedimportstrike':
      case 'stalledstrike':
      case 'slowspeedstrike':
      case 'slowtimestrike':
        return 'pi pi-bolt';
      case 'downloadcleaned':
        return 'pi pi-download';
      case 'queueitemdeleted':
        return 'pi pi-trash';
      case 'categorychanged':
        return 'pi pi-tag';
      default:
        return 'pi pi-circle';
    }
  }

  getEventIconClass(eventType: string, severity: string): string {
    const normalizedSeverity = severity?.toLowerCase() || '';
    const normalizedType = eventType?.toLowerCase() || '';
    
    // Strike events get special coloring based on severity
    if (normalizedType.includes('strike')) {
      switch (normalizedSeverity) {
        case 'error':
          return 'event-icon-strike-error';
        case 'warning':
          return 'event-icon-strike-warning';
        default:
          return 'event-icon-strike';
      }
    }
    
    // Other events get standard severity coloring
    switch (normalizedSeverity) {
      case 'error':
        return 'event-icon-error';
      case 'warning':
        return 'event-icon-warning';
      case 'information':
        return 'event-icon-info';
      default:
        return 'event-icon-default';
    }
  }

  getEventSeverity(severity: string): string {
    const normalizedSeverity = severity?.toLowerCase() || '';
    
    switch (normalizedSeverity) {
      case 'error':
        return 'danger';
      case 'warning':
        return 'warn';
      case 'information':
        return 'info';
      case 'important':
        return 'warn';
      default:
        return 'secondary';
    }
  }

  // Utility methods
  truncateMessage(message: string, maxLength = 80): string {
    if (message.length <= maxLength) {
      return message;
    }
    return message.substring(0, maxLength) + '...';
  }

  formatEventType(eventType: string): string {
    // Convert PascalCase to readable format
    return eventType.replace(/([A-Z])/g, ' $1').trim();
  }
}

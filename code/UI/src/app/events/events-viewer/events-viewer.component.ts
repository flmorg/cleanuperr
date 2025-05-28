import { Component, OnInit, OnDestroy, signal, computed, inject, ViewChild, ElementRef } from '@angular/core';
import { DatePipe, NgFor, NgIf, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil, debounceTime, distinctUntilChanged } from 'rxjs';
import { Clipboard } from '@angular/cdk/clipboard';

// PrimeNG Imports
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { CardModule } from 'primeng/card';
import { ToolbarModule } from 'primeng/toolbar';
import { TooltipModule } from 'primeng/tooltip';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { InputSwitchModule } from 'primeng/inputswitch';
import { MenuModule } from 'primeng/menu';
import { MenuItem } from 'primeng/api';

// Services & Models
import { AppHubService } from '../../core/services/app-hub.service';
import { AppEvent } from '../../core/models/event.models';

@Component({
  selector: 'app-events-viewer',
  standalone: true,
  imports: [
    NgFor,
    NgIf,
    DatePipe,
    FormsModule,
    TableModule,
    InputTextModule,
    ButtonModule,
    SelectModule,
    TagModule,
    CardModule,
    ToolbarModule,
    TooltipModule,
    ProgressSpinnerModule,
    MenuModule,
    InputSwitchModule
  ],
  providers: [AppHubService],
  templateUrl: './events-viewer.component.html',
  styleUrl: './events-viewer.component.scss'
})
export class EventsViewerComponent implements OnInit, OnDestroy {
  private appHubService = inject(AppHubService);
  private destroy$ = new Subject<void>();
  private clipboard = inject(Clipboard);
  private search$ = new Subject<string>();

  @ViewChild('eventsConsole') eventsConsole!: ElementRef;
  @ViewChild('exportMenu') exportMenu: any;
  
  // Signals for reactive state
  events = signal<AppEvent[]>([]);
  isConnected = signal<boolean>(false);
  autoScroll = signal<boolean>(true);
  expandedEvents: { [key: number]: boolean } = {};
  
  // Filter state
  severityFilter = signal<string | null>(null);
  eventTypeFilter = signal<string | null>(null);
  searchFilter = signal<string>('');

  // Export menu items
  exportMenuItems: MenuItem[] = [
    { label: 'Export as JSON', icon: 'pi pi-file', command: () => this.exportAsJson() },
    { label: 'Export as CSV', icon: 'pi pi-file-excel', command: () => this.exportAsCsv() },
    { label: 'Export as Text', icon: 'pi pi-file-o', command: () => this.exportAsText() }
  ];

  // Computed values
  filteredEvents = computed(() => {
    let filtered = this.events();
    
    if (this.severityFilter()) {
      filtered = filtered.filter(event => event.severity === this.severityFilter());
    }
    
    if (this.eventTypeFilter()) {
      filtered = filtered.filter(event => event.eventType === this.eventTypeFilter());
    }
    
    if (this.searchFilter()) {
      const search = this.searchFilter().toLowerCase();
      filtered = filtered.filter(event => 
        event.message.toLowerCase().includes(search) ||
        event.eventType.toLowerCase().includes(search) ||
        (event.data && event.data.toLowerCase().includes(search)) ||
        (event.trackingId && event.trackingId.toLowerCase().includes(search)));
    }
    
    return filtered;
  });
  
  severities = computed(() => {
    const uniqueSeverities = [...new Set(this.events().map(event => event.severity))];
    return uniqueSeverities.map(severity => ({ label: severity, value: severity }));
  });
  
  eventTypes = computed(() => {
    const uniqueTypes = [...new Set(this.events().map(event => event.eventType))];
    return uniqueTypes.map(type => ({ label: type, value: type }));
  });
  
  constructor() {}
  
  ngOnInit(): void {
    // Connect to SignalR hub
    this.appHubService.startConnection()
      .catch((error: Error) => console.error('Failed to connect to app hub:', error));
    
    // Subscribe to events
    this.appHubService.getEvents()
      .pipe(takeUntil(this.destroy$))
      .subscribe((events: AppEvent[]) => {
        this.events.set(events);
        if (this.autoScroll()) {
          this.scrollToBottom();
        }
      });
    
    // Subscribe to connection status
    this.appHubService.getEventsConnectionStatus()
      .pipe(takeUntil(this.destroy$))
      .subscribe((status: boolean) => {
        this.isConnected.set(status);
      });
      
    // Setup search debounce (300ms)
    this.search$
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntil(this.destroy$)
      )
      .subscribe(searchText => {
        this.searchFilter.set(searchText);
      });
  }

  ngAfterViewChecked(): void {
    if (this.autoScroll() && this.eventsConsole) {
      this.scrollToBottom();
    }
  }
  
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
  
  onSeverityFilterChange(severity: string): void {
    this.severityFilter.set(severity);
  }
  
  onEventTypeFilterChange(eventType: string): void {
    this.eventTypeFilter.set(eventType);
  }
  
  onSearchChange(event: Event): void {
    const searchText = (event.target as HTMLInputElement).value;
    this.search$.next(searchText);
  }
  
  clearFilters(): void {
    this.severityFilter.set(null);
    this.eventTypeFilter.set(null);
    this.searchFilter.set('');
  }
  
  getSeverity(severity: string): string {
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
      case 'test':
        return 'secondary';
      default:
        return 'secondary';
    }
  }
  
  refresh(): void {
    this.appHubService.requestRecentEvents();
  }
  
  hasDataInfo(): boolean {
    return this.events().some(event => event.data);
  }
  
  hasTrackingInfo(): boolean {
    return this.events().some(event => event.trackingId);
  }

  /**
   * Toggle expansion of an event entry
   */
  toggleEventExpansion(index: number, domEvent?: MouseEvent): void {
    if (domEvent) {
      domEvent.stopPropagation();
    }
    this.expandedEvents[index] = !this.expandedEvents[index];
  }
  
  /**
   * Copy a specific event entry to clipboard
   */
  copyEventEntry(event: AppEvent, domEvent: MouseEvent): void {
    domEvent.stopPropagation();
    
    const timestamp = new Date(event.timestamp).toISOString();
    let content = `[${timestamp}] [${event.severity}] [${event.eventType}] ${event.message}`;
    
    if (event.trackingId) {
      content += `\nTracking ID: ${event.trackingId}`;
    }
    
    if (event.data) {
      content += `\nData: ${event.data}`;
    }
    
    this.clipboard.copy(content);
  }
  
  /**
   * Copy all filtered events to clipboard
   */
  copyEvents(): void {
    const events = this.filteredEvents();
    if (events.length === 0) return;
    
    const content = events.map(event => {
      const timestamp = new Date(event.timestamp).toISOString();
      let entry = `[${timestamp}] [${event.severity}] [${event.eventType}] ${event.message}`;
      
      if (event.trackingId) {
        entry += `\nTracking ID: ${event.trackingId}`;
      }
      
      if (event.data) {
        entry += `\nData: ${event.data}`;
      }
      
      return entry;
    }).join('\n\n');
    
    this.clipboard.copy(content);
  }
  
  /**
   * Export events menu trigger
   */
  exportEvents(event?: MouseEvent): void {
    if (event && this.exportMenuItems.length > 0 && this.exportMenu) {
      this.exportMenu.toggle(event);
    }
  }
  
  /**
   * Export events as JSON
   */
  exportAsJson(): void {
    const events = this.filteredEvents();
    if (events.length === 0) return;
    
    const content = JSON.stringify(events, null, 2);
    this.downloadFile(content, 'application/json', 'events.json');
  }
  
  /**
   * Export events as CSV
   */
  exportAsCsv(): void {
    const events = this.filteredEvents();
    if (events.length === 0) return;
    
    // CSV header
    let csv = 'Timestamp,Severity,EventType,Message,Data,TrackingId\n';
    
    // CSV rows
    events.forEach(event => {
      const timestamp = new Date(event.timestamp).toISOString();
      const severity = event.severity || '';
      const eventType = event.eventType ? `"${event.eventType.replace(/"/g, '""')}"` : '';
      const message = event.message ? `"${event.message.replace(/"/g, '""')}"` : '';
      const data = event.data ? `"${event.data.replace(/"/g, '""').replace(/\n/g, ' ')}"` : '';
      const trackingId = event.trackingId ? `"${event.trackingId.replace(/"/g, '""')}"` : '';
      
      csv += `${timestamp},${severity},${eventType},${message},${data},${trackingId}\n`;
    });
    
    this.downloadFile(csv, 'text/csv', 'events.csv');
  }
  
  /**
   * Export events as plain text
   */
  exportAsText(): void {
    const events = this.filteredEvents();
    if (events.length === 0) return;
    
    const content = events.map(event => {
      const timestamp = new Date(event.timestamp).toISOString();
      let entry = `[${timestamp}] [${event.severity}] [${event.eventType}] ${event.message}`;
      
      if (event.trackingId) {
        entry += `\nTracking ID: ${event.trackingId}`;
      }
      
      if (event.data) {
        entry += `\nData: ${event.data}`;
      }
      
      return entry;
    }).join('\n\n');
    
    this.downloadFile(content, 'text/plain', 'events.txt');
  }
  
  /**
   * Helper method to download a file
   */
  private downloadFile(content: string, contentType: string, filename: string): void {
    const blob = new Blob([content], { type: contentType });
    const url = URL.createObjectURL(blob);
    
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link); // Required for Firefox
    link.click();
    document.body.removeChild(link); // Clean up
    
    setTimeout(() => {
      URL.revokeObjectURL(url);
    }, 100);
  }
  
  /**
   * Scroll to the bottom of the events container
   */
  private scrollToBottom(): void {
    if (this.eventsConsole && this.eventsConsole.nativeElement) {
      const element = this.eventsConsole.nativeElement;
      element.scrollTop = element.scrollHeight;
    }
  }

  /**
   * Sets the auto-scroll state
   */
  setAutoScroll(value: boolean): void {
    this.autoScroll.set(value);
    if (value) {
      this.scrollToBottom();
    }
  }

  /**
   * Format JSON data for display
   */
  formatJsonData(data: string): string {
    try {
      const parsed = JSON.parse(data);
      return JSON.stringify(parsed, null, 2);
    } catch {
      return data;
    }
  }

  /**
   * Check if data is valid JSON
   */
  isValidJson(data: string): boolean {
    try {
      JSON.parse(data);
      return true;
    } catch {
      return false;
    }
  }
} 
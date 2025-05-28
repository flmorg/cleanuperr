import { Component, OnInit, OnDestroy, signal, computed, inject, ViewChild, ElementRef } from '@angular/core';
import { DatePipe, NgFor, NgIf, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil, debounceTime, distinctUntilChanged, Subscription } from 'rxjs';
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
import { DatePickerModule } from 'primeng/datepicker';
import { PaginatorModule } from 'primeng/paginator';

// Services & Models
import { AppHubService } from '../../core/services/app-hub.service';
import { EventsService, EventsFilter, PaginatedResult } from '../../core/services/events.service';
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
    InputSwitchModule,
    DatePickerModule,
    PaginatorModule
  ],
  providers: [AppHubService, EventsService],
  templateUrl: './events-viewer.component.html',
  styleUrl: './events-viewer.component.scss'
})
export class EventsViewerComponent implements OnInit, OnDestroy {
  private appHubService = inject(AppHubService); // Keep for dashboard
  private eventsService = inject(EventsService);
  private destroy$ = new Subject<void>();
  private clipboard = inject(Clipboard);
  private search$ = new Subject<string>();
  private pollingSubscription?: Subscription;

  @ViewChild('eventsConsole') eventsConsole!: ElementRef;
  @ViewChild('exportMenu') exportMenu: any;

  // Signals for reactive state
  events = signal<AppEvent[]>([]);
  isConnected = signal<boolean>(true); // Always connected with HTTP
  autoScroll = signal<boolean>(true);
  expandedEvents: { [key: number]: boolean } = {};
  loading = signal<boolean>(false);

  // Pagination
  currentPage = signal<number>(1);
  pageSize = signal<number>(50);
  totalRecords = signal<number>(0);
  totalPages = signal<number>(0);

  // Filter state
  severityFilter = signal<string | null>(null);
  eventTypeFilter = signal<string | null>(null);
  searchFilter = signal<string>('');
  fromDate = signal<Date | null>(null);
  toDate = signal<Date | null>(null);

  // Export menu items
  exportMenuItems: MenuItem[] = [
    { label: 'Export as JSON', icon: 'pi pi-file', command: () => this.exportAsJson() },
    { label: 'Export as CSV', icon: 'pi pi-file-excel', command: () => this.exportAsCsv() },
    { label: 'Export as Text', icon: 'pi pi-file-o', command: () => this.exportAsText() }
  ];

  // Computed values
  filteredEvents = computed(() => {
    return this.events();
    // Note: We no longer need client-side filtering as filtering is done on the server
  });

  severities = signal<any[]>([]);
  eventTypes = signal<any[]>([]);

  constructor() { }

  ngOnInit(): void {
    // Setup search debounce
    this.search$
      .pipe(
        takeUntil(this.destroy$),
        debounceTime(300),
        distinctUntilChanged()
      )
      .subscribe(value => {
        this.searchFilter.set(value);
        this.loadEvents();
      });

    // Load event types and severities
    this.loadEventTypes();
    this.loadSeverities();

    // Initial events load
    this.loadEvents();

    // Start polling for new events
    this.startPolling();
  }

  ngOnDestroy(): void {
    this.stopPolling();
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSeverityFilterChange(severity: string): void {
    this.severityFilter.set(severity);
    this.currentPage.set(1); // Reset to first page when filter changes
    this.loadEvents();
  }

  onEventTypeFilterChange(eventType: string): void {
    this.eventTypeFilter.set(eventType);
    this.currentPage.set(1); // Reset to first page when filter changes
    this.loadEvents();
  }

  onSearchChange(event: Event): void {
    const searchText = (event.target as HTMLInputElement).value;
    this.search$.next(searchText);
  }

  clearFilters(): void {
    this.severityFilter.set(null);
    this.eventTypeFilter.set(null);
    this.searchFilter.set('');
    this.fromDate.set(null);
    this.toDate.set(null);
    this.currentPage.set(1);
    this.loadEvents();
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

  ngAfterViewChecked(): void {
    if (this.autoScroll() && this.eventsConsole) {
      this.scrollToBottom();
    }
  }

  setAutoScroll(value: boolean): void {
    this.autoScroll.set(value);
    if (value) {
      this.scrollToBottom();
    }
  }

  private scrollToBottom(): void {
    if (this.eventsConsole && this.eventsConsole.nativeElement) {
      const element = this.eventsConsole.nativeElement;
      element.scrollTop = element.scrollHeight;
    }
  }

  loadEvents(): void {
    this.loading.set(true);

    const filter: EventsFilter = {
      page: this.currentPage(),
      pageSize: this.pageSize(),
      severity: this.severityFilter() || undefined,
      eventType: this.eventTypeFilter() || undefined,
      fromDate: this.fromDate(),
      toDate: this.toDate()
    };

    this.eventsService.loadEvents(filter)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result: PaginatedResult<AppEvent>) => {
          this.events.set(result.items);
          this.totalRecords.set(result.totalCount);
          this.totalPages.set(result.totalPages);
          this.loading.set(false);

          // Auto-scroll to bottom if enabled
          if (this.autoScroll()) {
            setTimeout(() => this.scrollToBottom(), 0);
          }
        },
        error: (error) => {
          console.error('Error loading events:', error);
          this.loading.set(false);
        }
      });
  }

  loadEventTypes(): void {
    this.eventsService.getEventTypes()
      .pipe(takeUntil(this.destroy$))
      .subscribe(types => {
        this.eventTypes.set(types.map(type => ({ label: type, value: type })));
      });
  }

  loadSeverities(): void {
    this.eventsService.getSeverities()
      .pipe(takeUntil(this.destroy$))
      .subscribe(severities => {
        this.severities.set(severities.map(severity => ({ label: severity, value: severity })));
      });
  }

  onPageChange(event: any): void {
    this.currentPage.set(event.page + 1); // PrimeNG paginator is 0-based
    this.pageSize.set(event.rows);
    this.loadEvents();
  }

  onDateFilterChange(): void {
    this.currentPage.set(1); // Reset to first page
    this.loadEvents();
  }

  startPolling(): void {
    // Stop any existing polling
    this.stopPolling();

    // Create filter for polling (only apply type and severity filters, not pagination)
    const pollingFilter = {
      severity: this.severityFilter() || undefined,
      eventType: this.eventTypeFilter() || undefined,
      fromDate: this.fromDate(),
      toDate: this.toDate()
    };

    this.eventsService.startPolling(pollingFilter);

    // Subscribe to events stream to update UI
    this.pollingSubscription = this.eventsService.events$
      .pipe(takeUntil(this.destroy$))
      .subscribe(result => {
        if (result) {
          this.events.set(result.items);
          this.totalRecords.set(result.totalCount);
          this.totalPages.set(result.totalPages);

          // Auto-scroll to bottom if enabled
          if (this.autoScroll()) {
            setTimeout(() => this.scrollToBottom(), 0);
          }
        }
      });
  }

  stopPolling(): void {
    this.eventsService.stopPolling();
    if (this.pollingSubscription) {
      this.pollingSubscription.unsubscribe();
      this.pollingSubscription = undefined;
    }
  }

  formatJsonData(data: string): string {
    try {
      const parsed = JSON.parse(data);
      return JSON.stringify(parsed, null, 2);
    } catch {
      return data;
    }
  }

  isValidJson(data: string): boolean {
    try {
      JSON.parse(data);
      return true;
    } catch {
      return false;
    }
  }

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

  exportEvents(event?: MouseEvent): void {
    if (event && this.exportMenuItems.length > 0 && this.exportMenu) {
      this.exportMenu.toggle(event);
    }
  }

  exportAsJson(): void {
    const events = this.filteredEvents();
    if (events.length === 0) return;

    const content = JSON.stringify(events, null, 2);
    this.downloadFile(content, 'application/json', 'events.json');
  }

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

  toggleEventExpansion(index: number, domEvent?: MouseEvent): void {
    if (domEvent) {
      domEvent.stopPropagation();
    }
    this.expandedEvents[index] = !this.expandedEvents[index];
  }

  refresh(): void {
    // Reload events from the server
    this.loadEvents();
  }

  hasDataInfo(): boolean {
    return this.events().some(event => event.data);
  }
  
  hasTrackingInfo(): boolean {
    return this.events().some(event => event.trackingId);
  }
}
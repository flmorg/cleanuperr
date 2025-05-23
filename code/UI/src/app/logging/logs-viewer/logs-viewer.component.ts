import { Component, OnInit, OnDestroy, signal, computed, inject, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { DatePipe, NgIf, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
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
import { LogHubService } from '../../core/services/log-hub.service';
import { LogEntry } from '../../core/models/signalr.models';

@Component({
  selector: 'app-logs-viewer',
  standalone: true,
  imports: [
    NgIf,
    NgClass,
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
    InputSwitchModule,
    MenuModule
  ],
  providers: [LogHubService],
  templateUrl: './logs-viewer.component.html',
  styleUrl: './logs-viewer.component.scss'
})
export class LogsViewerComponent implements OnInit, OnDestroy, AfterViewChecked {
  private logHubService = inject(LogHubService);
  private clipboard = inject(Clipboard);
  private destroy$ = new Subject<void>();
  
  @ViewChild('logsConsole') logsConsole!: ElementRef;
  
  // Signals for reactive state
  logs = signal<LogEntry[]>([]);
  isConnected = signal<boolean>(false);
  autoScroll = signal<boolean>(true);
  expandedLogs: { [key: number]: boolean } = {};
  
  // Filter state
  levelFilter = signal<string | null>(null);
  categoryFilter = signal<string | null>(null);
  searchFilter = signal<string>('');
  
  // Export menu items
  exportMenuItems: MenuItem[] = [
    { label: 'Export as JSON', icon: 'pi pi-file', command: () => this.exportAsJson() },
    { label: 'Export as CSV', icon: 'pi pi-file-excel', command: () => this.exportAsCsv() },
    { label: 'Export as Text', icon: 'pi pi-file-o', command: () => this.exportAsText() }
  ];
  
  // Computed values
  filteredLogs = computed(() => {
    let filtered = this.logs();
    console.log(`Total logs before filtering: ${filtered.length}`);
    
    if (this.levelFilter()) {
      filtered = filtered.filter(log => log.level === this.levelFilter());
      console.log(`After level filter: ${filtered.length} logs remaining`);
    }
    
    if (this.categoryFilter()) {
      filtered = filtered.filter(log => log.category === this.categoryFilter());
      console.log(`After category filter: ${filtered.length} logs remaining`);
    }
    
    if (this.searchFilter() && this.searchFilter().trim() !== '') {
      const search = this.searchFilter().toLowerCase();
      filtered = filtered.filter(log => 
        log.message.toLowerCase().includes(search) ||
        (log.exception && log.exception.toLowerCase().includes(search)));
      console.log(`After search filter: ${filtered.length} logs remaining`);
    }
    
    return filtered;
  });
  
  levels = computed(() => {
    const uniqueLevels = [...new Set(this.logs().map(log => log.level))];
    return uniqueLevels.map(level => ({ label: level, value: level }));
  });
  
  categories = computed(() => {
    const uniqueCategories = [...new Set(this.logs().map(log => log.category).filter(Boolean))];
    return uniqueCategories.map(category => ({ label: category, value: category }));
  });
  
  constructor() {
    // Initialize expandedLogs object
    this.expandedLogs = {};
    
    // Add some test logs to ensure the display works
    this.addTestLogs();
  }
  
  ngOnInit(): void {
    // Connect to SignalR hub
    this.logHubService.startConnection()
      .then(() => {
        console.log('Connected to log hub, requesting logs...');
        // Request logs immediately once connected
        this.logHubService.requestRecentLogs();
      })
      .catch((error: Error) => console.error('Failed to connect to log hub:', error));
    
    // Subscribe to logs
    this.logHubService.getLogs()
      .pipe(takeUntil(this.destroy$))
      .subscribe((logs: LogEntry[]) => {
        console.log(`Received ${logs.length} logs from service`);
        this.logs.set(logs);
        if (this.autoScroll()) {
          this.scrollToBottom();
        }
      });
    
    // Subscribe to connection status
    this.logHubService.getConnectionStatus()
      .pipe(takeUntil(this.destroy$))
      .subscribe((status: boolean) => {
        console.log(`Log hub connection status: ${status ? 'connected' : 'disconnected'}`);
        this.isConnected.set(status);
        
        // Request logs again when connection is established
        if (status) {
          this.logHubService.requestRecentLogs();
        }
      });
  }
  
  ngAfterViewChecked(): void {
    if (this.autoScroll() && this.logsConsole) {
      this.scrollToBottom();
    }
  }
  
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
  
  onLevelFilterChange(level: string): void {
    this.levelFilter.set(level);
  }
  
  onCategoryFilterChange(category: string): void {
    this.categoryFilter.set(category);
  }
  
  onSearchChange(event: Event): void {
    this.searchFilter.set((event.target as HTMLInputElement).value);
  }
  
  clearFilters(): void {
    this.levelFilter.set(null);
    this.categoryFilter.set(null);
    this.searchFilter.set('');
  }
  
  getSeverity(level: string): string {
    const normalizedLevel = level?.toLowerCase() || '';
    
    switch (normalizedLevel) {
      case 'error':
      case 'fatal':
      case 'critical':
        return 'danger';
      case 'warning':
        return 'warning';
      case 'information':
      case 'info':
        return 'info';
      case 'debug':
      case 'trace':
        return 'success';
      default:
        return 'info';
    }
  }
  
  refresh(): void {
    this.logHubService.requestRecentLogs();
  }
  
  hasJobInfo(): boolean {
    return this.logs().some(log => log.jobName);
  }
  
  hasInstanceInfo(): boolean {
    return this.logs().some(log => log.instanceName);
  }
  
  /**
   * Returns the appropriate CSS class based on log level
   */
  getLevelClass(level: string): string {
    const normalizedLevel = level?.toLowerCase() || '';
    
    switch (normalizedLevel) {
      case 'error':
      case 'fatal':
      case 'critical':
        return 'level-error';
      case 'warning':
        return 'level-warning';
      case 'information':
      case 'info':
        return 'level-info';
      case 'debug':
        return 'level-debug';
      case 'trace':
        return 'level-trace';
      default:
        return 'level-default';
    }
  }
  
  /**
   * Toggle expansion of a log entry
   */
  toggleLogExpansion(index: number, event?: Event): void {
    if (event) {
      event.stopPropagation();
    }
    this.expandedLogs[index] = !this.expandedLogs[index];
  }
  
  /**
   * Copy a specific log entry to clipboard
   */
  copyLogEntry(log: LogEntry, event: Event): void {
    event.stopPropagation();
    
    const timestamp = new Date(log.timestamp).toISOString();
    let content = `[${timestamp}] [${log.level}] ${log.category ? `[${log.category}] ` : ''}${log.message}`;
    
    if (log.exception) {
      content += `\n${log.exception}`;
    }
    
    this.clipboard.copy(content);
  }
  
  /**
   * Copy all filtered logs to clipboard
   */
  copyLogs(): void {
    const logs = this.filteredLogs();
    if (logs.length === 0) return;
    
    const content = logs.map(log => {
      const timestamp = new Date(log.timestamp).toISOString();
      let entry = `[${timestamp}] [${log.level}] ${log.category ? `[${log.category}] ` : ''}${log.message}`;
      
      if (log.exception) {
        entry += `\n${log.exception}`;
      }
      
      return entry;
    }).join('\n');
    
    this.clipboard.copy(content);
  }
  
  /**
   * Export logs menu trigger
   */
  exportLogs(event?: MouseEvent): void {
    if (event && this.exportMenuItems.length > 0) {
      const menuElement = document.querySelector('p-menu');
      if (menuElement) {
        (menuElement as any).toggle(event);
      }
    }
  }
  
  /**
   * Export logs as JSON
   */
  exportAsJson(): void {
    const logs = this.filteredLogs();
    if (logs.length === 0) return;
    
    const content = JSON.stringify(logs, null, 2);
    this.downloadFile(content, 'application/json', 'logs.json');
  }
  
  /**
   * Export logs as CSV
   */
  exportAsCsv(): void {
    const logs = this.filteredLogs();
    if (logs.length === 0) return;
    
    // CSV header
    let csv = 'Timestamp,Level,Category,Message,Exception,JobName,InstanceName\n';
    
    // CSV rows
    logs.forEach(log => {
      const timestamp = new Date(log.timestamp).toISOString();
      const level = log.level || '';
      const category = log.category ? `"${log.category.replace(/"/g, '""')}"` : '';
      const message = log.message ? `"${log.message.replace(/"/g, '""')}"` : '';
      const exception = log.exception ? `"${log.exception.replace(/"/g, '""').replace(/\n/g, ' ')}"` : '';
      const jobName = log.jobName ? `"${log.jobName.replace(/"/g, '""')}"` : '';
      const instanceName = log.instanceName ? `"${log.instanceName.replace(/"/g, '""')}"` : '';
      
      csv += `${timestamp},${level},${category},${message},${exception},${jobName},${instanceName}\n`;
    });
    
    this.downloadFile(csv, 'text/csv', 'logs.csv');
  }
  
  /**
   * Export logs as plain text
   */
  exportAsText(): void {
    const logs = this.filteredLogs();
    if (logs.length === 0) return;
    
    const content = logs.map(log => {
      const timestamp = new Date(log.timestamp).toISOString();
      let entry = `[${timestamp}] [${log.level}] ${log.category ? `[${log.category}] ` : ''}${log.message}`;
      
      if (log.exception) {
        entry += `\n${log.exception}`;
      }
      
      if (log.jobName) {
        entry += `\nJob: ${log.jobName}`;
      }
      
      if (log.instanceName) {
        entry += `\nInstance: ${log.instanceName}`;
      }
      
      return entry;
    }).join('\n\n');
    
    this.downloadFile(content, 'text/plain', 'logs.txt');
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
    link.click();
    
    setTimeout(() => {
      URL.revokeObjectURL(url);
    }, 100);
  }
  
  /**
   * Clear all logs
   */
  clearLogs(): void {
    this.logs.set([]);
  }
  
  /**
   * Scroll to the bottom of the logs container
   */
  private scrollToBottom(): void {
    if (this.logsConsole && this.logsConsole.nativeElement) {
      const element = this.logsConsole.nativeElement;
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
   * Add test logs for debugging purposes
   * This is only used during development to ensure the logs display correctly
   */
  private addTestLogs(): void {
    const testLogs: LogEntry[] = [
      {
        timestamp: new Date(),
        level: 'Information',
        category: 'Application',
        message: 'Application started successfully'
      },
      {
        timestamp: new Date(),
        level: 'Warning',
        category: 'Database',
        message: 'Database connection took longer than expected',
        jobName: 'MaintenanceJob',
        instanceName: 'Instance01'
      },
      {
        timestamp: new Date(),
        level: 'Error',
        category: 'API',
        message: 'Failed to connect to external service',
        exception: 'System.Net.Http.HttpRequestException: Connection refused\n   at ExternalService.Connect() in ExternalService.cs:line 45\n   at API.Controllers.ExternalController.Connect() in ExternalController.cs:line 28'
      },
      {
        timestamp: new Date(),
        level: 'Debug',
        category: 'Authentication',
        message: 'User authentication attempt'
      },
      {
        timestamp: new Date(),
        level: 'Trace',
        category: 'FileSystem',
        message: 'File system operations completed',
        jobName: 'FileProcessingJob',
        instanceName: 'Worker02'
      }
    ];
    
    // Set the test logs to display
    this.logs.set(testLogs);
    
    // Mark as connected so UI shows properly
    this.isConnected.set(true);
  }
}

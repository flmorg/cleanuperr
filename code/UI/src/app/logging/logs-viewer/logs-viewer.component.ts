import { Component, OnInit, OnDestroy, signal, computed, inject } from '@angular/core';
import { DatePipe, NgIf, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';

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
    ProgressSpinnerModule
  ],
  providers: [LogHubService],
  templateUrl: './logs-viewer.component.html',
  styleUrl: './logs-viewer.component.scss'
})
export class LogsViewerComponent implements OnInit, OnDestroy {
  private logHubService = inject(LogHubService);
  private destroy$ = new Subject<void>();
  
  // Signals for reactive state
  logs = signal<LogEntry[]>([]);
  isConnected = signal<boolean>(false);
  
  // Filter state
  levelFilter = signal<string | null>(null);
  categoryFilter = signal<string | null>(null);
  searchFilter = signal<string>('');
  
  // Computed values
  filteredLogs = computed(() => {
    let filtered = this.logs();
    
    if (this.levelFilter()) {
      filtered = filtered.filter(log => log.level === this.levelFilter());
    }
    
    if (this.categoryFilter()) {
      filtered = filtered.filter(log => log.category === this.categoryFilter());
    }
    
    if (this.searchFilter) {
      const search = this.searchFilter().toLowerCase();
      filtered = filtered.filter(log => 
        log.message.toLowerCase().includes(search) ||
        (log.exception && log.exception.toLowerCase().includes(search)));
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
  
  constructor() {}
  
  ngOnInit(): void {
    // Connect to SignalR hub
    this.logHubService.startConnection()
      .catch((error: Error) => console.error('Failed to connect to log hub:', error));
    
    // Subscribe to logs
    this.logHubService.getLogs()
      .pipe(takeUntil(this.destroy$))
      .subscribe((logs: LogEntry[]) => {
        this.logs.set(logs);
      });
    
    // Subscribe to connection status
    this.logHubService.getConnectionStatus()
      .pipe(takeUntil(this.destroy$))
      .subscribe((status: boolean) => {
        this.isConnected.set(status);
      });
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
}

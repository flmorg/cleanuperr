import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject, Subscription } from 'rxjs';
import { tap } from 'rxjs/operators';
import { ApplicationPathService } from './base-path.service';
import { AppEvent } from '../models/event.models';

export interface PaginatedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface EventsFilter {
  page?: number;
  pageSize?: number;
  severity?: string;
  eventType?: string;
  fromDate?: Date | null;
  toDate?: Date | null;
  search?: string;
}

@Injectable({
  providedIn: 'root'
})
export class EventsService {
  private readonly http = inject(HttpClient);
  private readonly ApplicationPathService = inject(ApplicationPathService);
  
  // State management
  private events = new BehaviorSubject<PaginatedResult<AppEvent> | null>(null);
  private pollingSubscription: Subscription | null = null;
  private lastEventTimestamp: Date | null = null;
  private isPolling = false;
  private pollInterval = 5000; // 5 seconds
  
  // Public observables
  events$ = this.events.asObservable();

  /**
   * Load events with pagination and filtering
   */
  loadEvents(filter: EventsFilter = {}): Observable<PaginatedResult<AppEvent>> {
    // Set default values if not provided
    const params = new HttpParams()
      .set('page', filter.page?.toString() || '1')
      .set('pageSize', filter.pageSize?.toString() || '100');
    
    // Add optional filters if they exist
    const paramsWithFilters = this.addFiltersToParams(params, filter);
    
    return this.http.get<PaginatedResult<AppEvent>>(this.ApplicationPathService.buildApiUrl('/events'), { params: paramsWithFilters })
      .pipe(
        tap(result => {
          this.events.next(result);
          
          // Update last event timestamp if there are events
          if (result.items.length > 0) {
            const newestEvent = result.items.reduce((prev, current) => {
              return new Date(current.timestamp) > new Date(prev.timestamp) ? current : prev;
            });
            this.lastEventTimestamp = new Date(newestEvent.timestamp);
          }
        })
      );
  }

  /**
   * Helper to add filters to HttpParams
   */
  private addFiltersToParams(params: HttpParams, filter: Partial<EventsFilter>): HttpParams {
    let updatedParams = params;
    
    if (filter.severity) {
      updatedParams = updatedParams.set('severity', filter.severity);
    }
    
    if (filter.eventType) {
      updatedParams = updatedParams.set('eventType', filter.eventType);
    }
    
    if (filter.fromDate) {
      updatedParams = updatedParams.set('fromDate', filter.fromDate.toISOString());
    }
    
    if (filter.toDate) {
      updatedParams = updatedParams.set('toDate', filter.toDate.toISOString());
    }
    
    if (filter.search) {
      updatedParams = updatedParams.set('search', filter.search);
    }
    
    return updatedParams;
  }

  /**
   * Get event types
   */
  getEventTypes(): Observable<string[]> {
    return this.http.get<string[]>(this.ApplicationPathService.buildApiUrl('/events/types'));
  }

  /**
   * Get severities
   */
  getSeverities(): Observable<string[]> {
    return this.http.get<string[]>(this.ApplicationPathService.buildApiUrl('/events/severities'));
  }
}

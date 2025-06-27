export interface AppEvent {
  id: string;
  timestamp: Date;
  eventType: string;
  message: string;
  data?: string;
  severity: string;
  trackingId?: string;
}

export interface EventStats {
  totalEvents: number;
  eventsBySeverity: { severity: string; count: number }[];
  eventsByType: { eventType: string; count: number }[];
  recentEventsCount: number;
}

export interface EventFilter {
  severity?: string;
  eventType?: string;
  search?: string;
  count?: number;
} 
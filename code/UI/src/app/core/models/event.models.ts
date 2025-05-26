export interface Event {
  id: string;
  timestamp: string;
  eventType: string;
  source: string;
  message: string;
  data?: string;
  severity: string;
  correlationId?: string;
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
  source?: string;
  search?: string;
  count?: number;
} 
import { NotificationConfig } from './notification-config.model';

export interface NotifiarrConfig extends NotificationConfig {
  apiKey?: string;
  channelId?: string;
} 

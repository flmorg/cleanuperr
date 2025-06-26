import { NotifiarrConfig } from './notifiarr-config.model';
import { AppriseConfig } from './apprise-config.model';

export interface NotificationsConfig {
  notifiarr?: NotifiarrConfig;
  apprise?: AppriseConfig;
} 

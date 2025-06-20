import { LogEventLevel } from './log-event-level.enum';
import { CertificateValidationType } from './certificate-validation-type.enum';

export interface GeneralConfig {
  displaySupportBanner: boolean;
  dryRun: boolean;
  httpMaxRetries: number;
  httpTimeout: number;
  httpCertificateValidation: CertificateValidationType;
  searchEnabled: boolean;
  searchDelay: number;
  logLevel: LogEventLevel;
  ignoredDownloads: string[];
}

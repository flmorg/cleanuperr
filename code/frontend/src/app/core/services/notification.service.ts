import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  // Notification events that components can subscribe to
  private errorNotification = new Subject<string>();
  private successNotification = new Subject<string>();
  private validationNotification = new Subject<void>();
  
  // Public observables
  public error$ = this.errorNotification.asObservable();
  public success$ = this.successNotification.asObservable();
  public validation$ = this.validationNotification.asObservable();
  
  // Track last notification to prevent duplicates
  private lastNotification: { type: string; message: string; timestamp: number } | null = null;
  private debounceTime = 300; // ms

  constructor() {}

  /**
   * Emits an error notification event
   */
  showError(message: string): void {
    if (this.isDuplicate('error', message)) return;
    this.trackNotification('error', message);
    this.errorNotification.next(message);
  }

  /**
   * Emits a success notification event
   */
  showSuccess(message: string): void {
    if (this.isDuplicate('success', message)) return;
    this.trackNotification('success', message);
    this.successNotification.next(message);
  }

  /**
   * Emits a validation error notification event
   */
  showValidationError(): void {
    if (this.isDuplicate('validation', 'form-error')) return;
    this.trackNotification('validation', 'form-error');
    this.validationNotification.next();
  }

  /**
   * Helper method to determine if a notification is a duplicate
   */
  private isDuplicate(type: string, message: string): boolean {
    const now = Date.now();
    
    if (
      this.lastNotification && 
      this.lastNotification.type === type && 
      this.lastNotification.message === message &&
      now - this.lastNotification.timestamp < this.debounceTime
    ) {
      return true;
    }
    
    return false;
  }

  /**
   * Helper method to track the last notification
   */
  private trackNotification(type: string, message: string): void {
    this.lastNotification = {
      type,
      message,
      timestamp: Date.now()
    };
  }
}

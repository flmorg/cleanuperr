import { Component, OnDestroy, OnInit } from '@angular/core';
import { MessageService } from 'primeng/api';
import { Subject, takeUntil } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { CommonModule } from '@angular/common';
import { ToastModule } from 'primeng/toast';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule, ToastModule],
  template: '<p-toast></p-toast>',
  providers: [MessageService]
})
export class ToastContainerComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  constructor(
    private notificationService: NotificationService,
    private messageService: MessageService
  ) {}

  ngOnInit(): void {
    // Subscribe to error notifications
    this.notificationService.error$
      .pipe(takeUntil(this.destroy$))
      .subscribe(message => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: message,
          life: 5000
        });
      });

    // Subscribe to success notifications
    this.notificationService.success$
      .pipe(takeUntil(this.destroy$))
      .subscribe(message => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: message,
          life: 3000
        });
      });

    // Subscribe to validation notifications
    this.notificationService.validation$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.messageService.add({
          severity: 'error',
          summary: 'Validation Error',
          detail: 'Please correct the errors in the form before saving.',
          life: 5000
        });
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}

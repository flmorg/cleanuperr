import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';
import { ConfirmationService } from 'primeng/api';

export interface CanComponentDeactivate {
  canDeactivate: () => boolean;
}

export const pendingChangesGuard: CanDeactivateFn<CanComponentDeactivate> = async (component) => {
  // If the component doesn't have unsaved changes, allow navigation
  if (component.canDeactivate()) {
    return true;
  }

  // Otherwise show a confirmation dialog
  const confirmationService = inject(ConfirmationService);
  
  return new Promise<boolean>((resolve) => {
    confirmationService.confirm({
      header: 'Unsaved Changes',
      message: 'You have unsaved changes. Are you sure you want to leave this page?',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Leave',
      rejectLabel: 'Stay',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => resolve(true),
      reject: () => resolve(false)
    });
  });
};

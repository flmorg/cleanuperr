import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProgressSpinnerModule } from 'primeng/progressspinner';

@Component({
  selector: 'app-loading-error-state',
  standalone: true,
  imports: [CommonModule, ProgressSpinnerModule],
  templateUrl: './loading-error-state.component.html',
  styleUrls: ['./loading-error-state.component.scss']
})
export class LoadingErrorStateComponent {
  @Input() loading: boolean = false;
  @Input() error: string | null = null;
  @Input() noConnectionMessage: string = 'Not connected to server';
  @Input() loadingMessage: string = 'Loading...';
  @Input() reloadSuggestion: string = 'Check your network connection or try reloading the page';
}

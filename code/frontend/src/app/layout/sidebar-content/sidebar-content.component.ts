import { Component, Input, inject, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';

interface MenuItem {
  label: string;
  icon: string;
  route: string;
  badge?: string;
}

@Component({
  selector: 'app-sidebar-content',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    ButtonModule
  ],
  templateUrl: './sidebar-content.component.html',
  styleUrl: './sidebar-content.component.scss'
})
export class SidebarContentComponent {
  @Input() menuItems: MenuItem[] = [];
  @Input() isMobile = false;
  @Output() navItemClicked = new EventEmitter<void>();
  
  // Inject router for active route styling
  public router = inject(Router);
  
  /**
   * Handle navigation item click
   */
  onNavItemClick(): void {
    if (this.isMobile) {
      this.navItemClicked.emit();
    }
  }
}

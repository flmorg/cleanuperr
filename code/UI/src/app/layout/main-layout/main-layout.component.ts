import { Component, inject, signal } from '@angular/core';
import { Router, RouterOutlet, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Title } from '@angular/platform-browser';

// PrimeNG Imports
import { ButtonModule } from 'primeng/button';
import { ToolbarModule } from 'primeng/toolbar';
import { FormsModule } from '@angular/forms';
import { MenuModule } from 'primeng/menu';
import { SidebarModule } from 'primeng/sidebar';
import { DividerModule } from 'primeng/divider';
import { RippleModule } from 'primeng/ripple';

interface MenuItem {
  label: string;
  icon: string;
  route: string;
  badge?: string;
}

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    ButtonModule,
    ToolbarModule,
    FormsModule,
    MenuModule,
    SidebarModule,
    DividerModule,
    RippleModule
  ],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss'
})
export class MainLayoutComponent {
  isSidebarCollapsed = signal<boolean>(false);
  
  // Menu items
  menuItems: MenuItem[] = [
    { label: 'Dashboard', icon: 'pi pi-home', route: '/dashboard' },
    { label: 'Logs', icon: 'pi pi-list', route: '/logs' },
    { label: 'Settings', icon: 'pi pi-cog', route: '/settings' }
  ];
  
  // Mobile menu state
  mobileSidebarVisible = signal<boolean>(false);
  
  // Inject router and title service
  public router = inject(Router);
  private titleService = inject(Title);
  
  constructor() {}
  
  /**
   * Get the current page title based on the active route
   */
  getPageTitle(): string {
    const currentUrl = this.router.url;
    
    if (currentUrl.includes('/dashboard')) {
      return 'Dashboard';
    } else if (currentUrl.includes('/logs')) {
      return 'Logs';
    } else if (currentUrl.includes('/settings')) {
      return 'Settings';
    } else {
      return 'Cleanuparr';
    }
  }
  
  toggleMobileSidebar(): void {
    this.mobileSidebarVisible.update(value => !value);
  }
  
  toggleSidebar(): void {
    this.isSidebarCollapsed.update(value => !value);
  }
}

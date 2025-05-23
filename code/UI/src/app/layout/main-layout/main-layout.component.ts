import { Component, inject, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';

// PrimeNG Imports
import { ButtonModule } from 'primeng/button';
import { ToolbarModule } from 'primeng/toolbar';
import { InputSwitchModule } from 'primeng/inputswitch';
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
    RouterLinkActive,
    ButtonModule,
    ToolbarModule,
    InputSwitchModule,
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
  darkMode = signal<boolean>(false);
  isSidebarCollapsed = signal<boolean>(false);
  
  // Menu items
  menuItems: MenuItem[] = [
    { label: 'Dashboard', icon: 'pi pi-home', route: '/dashboard' },
    { label: 'Logs', icon: 'pi pi-list', route: '/logs' },
    { label: 'Settings', icon: 'pi pi-cog', route: '/settings' }
  ];
  
  // Mobile menu state
  mobileSidebarVisible = signal<boolean>(false);
  
  constructor() {
    // Initialize theme based on system preference
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    this.darkMode.set(prefersDark);
  }
  
  toggleDarkMode(event: any): void {
    const isDark = event.checked;
    this.darkMode.set(isDark);
    
    // Apply theme to document
    const documentElement = document.documentElement;
    if (isDark) {
      documentElement.classList.add('dark');
      documentElement.style.colorScheme = 'dark';
    } else {
      documentElement.classList.remove('dark');
      documentElement.style.colorScheme = 'light';
    }
  }
  
  toggleMobileSidebar(): void {
    this.mobileSidebarVisible.update(value => !value);
  }
  
  toggleSidebar(): void {
    this.isSidebarCollapsed.update(value => !value);
  }
}

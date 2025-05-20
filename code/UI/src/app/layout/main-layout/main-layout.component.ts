import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';

// PrimeNG Imports
import { ButtonModule } from 'primeng/button';
import { ToolbarModule } from 'primeng/toolbar';
import { InputSwitchModule } from 'primeng/inputswitch';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    ButtonModule,
    ToolbarModule,
    InputSwitchModule,
    FormsModule
  ],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss'
})
export class MainLayoutComponent {
  darkMode = signal<boolean>(false);
  
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
}

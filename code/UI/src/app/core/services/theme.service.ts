import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly THEME_KEY = 'app-theme';
  private currentTheme = 'dark'; // Always dark mode

  constructor() {
    this.initializeTheme();
  }

  initializeTheme(): void {
    // Apply our custom Noir preset (dark theme with purple primary, emerald secondary)
    this.applyDarkTheme();
    // Save the theme preference
    localStorage.setItem(this.THEME_KEY, this.currentTheme);
  }

  /**
   * Apply the dark theme using our custom Noir preset
   * The preset handles all colors including purple primary and emerald secondary
   */
  private applyDarkTheme(): void {
    const documentElement = document.documentElement;
    
    // Set dark mode
    documentElement.classList.add('dark');
    documentElement.style.colorScheme = 'dark';
    
    // The Noir preset is applied in app.config.ts and handles all theme colors
    // No need to manually set CSS variables as they're managed by PrimeNG
  }

  // Public API methods
  getCurrentTheme(): string {
    return this.currentTheme;
  }

  isDarkMode(): boolean {
    return true; // Always dark mode
  }
}

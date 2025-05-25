import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly THEME_KEY = 'app-theme';
  private currentTheme: 'dark' = 'dark'; // Always dark mode

  constructor() {
    this.initializeTheme();
  }

  initializeTheme(): void {
    // Always use dark theme with purple accent from our custom preset
    this.applyDarkTheme();
    // Save the theme preference
    localStorage.setItem(this.THEME_KEY, this.currentTheme);
  }

  /**
   * Apply the dark theme to the document using our Noir preset
   * This is now the only theme for the application
   */
  private applyDarkTheme(): void {
    const documentElement = document.documentElement;
    
    // Set dark mode
    documentElement.classList.add('dark');
    documentElement.style.colorScheme = 'dark';
    
    // Update PrimeNG theme to our custom Noir preset (dark)
    const linkElement = document.getElementById('app-theme') as HTMLLinkElement;
    if (linkElement) {
      linkElement.href = 'noir-dark.css';
    }
    
    // Note: We no longer need to set CSS variables manually as they're defined in the Noir preset
    // The preset handles all the theme colors including the purple accent and dark surfaces
  }

  // Public API methods kept for compatibility
  getCurrentTheme(): 'dark' {
    return this.currentTheme;
  }

  isDarkMode(): boolean {
    return true; // Always dark mode
  }
}

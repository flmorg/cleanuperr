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
    // Always use dark theme with purple accent
    this.applyDarkPurpleTheme();
    // Save the theme preference
    localStorage.setItem(this.THEME_KEY, this.currentTheme);
  }

  /**
   * Apply the dark purple theme to the document
   * This is now the only theme for the application
   */
  private applyDarkPurpleTheme(): void {
    const documentElement = document.documentElement;
    
    // Set dark mode
    documentElement.classList.add('dark');
    documentElement.style.colorScheme = 'dark';
    
    // Apply custom CSS variables for purple accent
    documentElement.style.setProperty('--primary-color', '#7E57C2');
    documentElement.style.setProperty('--primary-color-text', '#ffffff');
    documentElement.style.setProperty('--primary-dark', '#5E35B1'); 
    documentElement.style.setProperty('--primary-light', '#B39DDB');
    
    // Additional dark theme variables
    documentElement.style.setProperty('--surface-ground', '#121212');
    documentElement.style.setProperty('--surface-section', '#1E1E1E');
    documentElement.style.setProperty('--surface-card', '#262626');
    documentElement.style.setProperty('--surface-overlay', '#2A2A2A');
    documentElement.style.setProperty('--surface-border', '#383838');
    
    documentElement.style.setProperty('--text-color', '#F5F5F5');
    documentElement.style.setProperty('--text-color-secondary', '#BDBDBD');
    documentElement.style.setProperty('--text-color-disabled', '#757575');
    
    // Update PrimeNG theme to dark
    const linkElement = document.getElementById('app-theme') as HTMLLinkElement;
    if (linkElement) {
      linkElement.href = 'lara-dark.css';
    }
  }

  // Public API methods kept for compatibility
  getCurrentTheme(): 'dark' {
    return this.currentTheme;
  }

  isDarkMode(): boolean {
    return true; // Always dark mode
  }
}

import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly THEME_KEY = 'app-theme';
  private currentTheme: 'light' | 'dark' = 'light';

  constructor() {
    this.initializeTheme();
  }

  initializeTheme(): void {
    // Check if there's a saved theme preference
    const savedTheme = localStorage.getItem(this.THEME_KEY);
    
    if (savedTheme === 'dark' || savedTheme === 'light') {
      this.currentTheme = savedTheme;
    } else {
      // Check system preference if no saved preference
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
      this.currentTheme = prefersDark ? 'dark' : 'light';
    }
    
    // Apply the theme
    this.applyTheme(this.currentTheme);
  }

  switchTheme(theme: 'light' | 'dark'): void {
    this.currentTheme = theme;
    this.applyTheme(theme);
    localStorage.setItem(this.THEME_KEY, theme);
  }

  private applyTheme(theme: 'light' | 'dark'): void {
    const documentElement = document.documentElement;
    
    if (theme === 'dark') {
      documentElement.classList.add('dark');
      documentElement.style.colorScheme = 'dark';
    } else {
      documentElement.classList.remove('dark');
      documentElement.style.colorScheme = 'light';
    }
    
    // Update PrimeNG theme
    const linkElement = document.getElementById('app-theme') as HTMLLinkElement;
    if (linkElement) {
      linkElement.href = `lara-${theme}.css`;
    }
  }

  getCurrentTheme(): 'light' | 'dark' {
    return this.currentTheme;
  }

  isDarkMode(): boolean {
    return this.currentTheme === 'dark';
  }
}

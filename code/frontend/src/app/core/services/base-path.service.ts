import { Injectable, isDevMode } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class BasePathService {
  
  /**
   * Gets the current base path from the dynamically updated environment
   */
  getBasePath(): string {
    // If in development mode, use the local API
    if (isDevMode()) {
      return `http://localhost:5000`;
    }

    // Use the server-injected base path or fallback to root
    return (window as any)['_server_base_path'] || '/';
  }

  /**
   * Builds a full URL with the base path
   */
  buildUrl(path: string): string {
    const basePath = this.getBasePath();
    const cleanPath = path.startsWith('/') ? path : '/' + path;
    
    return basePath === '/' ? cleanPath : basePath + cleanPath;
  }

  /**
   * Builds an API URL with the base path
   */
  buildApiUrl(apiPath: string): string {
    const basePath = this.getBasePath();
    const cleanApiPath = apiPath.startsWith('/') ? apiPath : '/' + apiPath;
    
    // In development mode, return full URL directly
    if (isDevMode()) {
      return basePath + '/api' + cleanApiPath;
    }
    
    return basePath === '/' ? '/api' + cleanApiPath : basePath + '/api' + cleanApiPath;
  }
} 
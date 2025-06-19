import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class BasePathService {
  
  /**
   * Gets the current base path from the dynamically updated environment
   */
  getBasePath(): string {
    // Fallback to window value if environment hasn't been updated yet
    return (window as any)['_app_base'] || '/api';
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
    
    return basePath === '/' ? '/api' + cleanApiPath : basePath + '/api' + cleanApiPath;
  }
} 
import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import { APP_BASE_HREF } from '@angular/common';

async function bootstrap() {
  const basePath = (window as any)['_app_base'] || '/';
  
  const app = await bootstrapApplication(AppComponent, {
    providers: [
      {
        provide: APP_BASE_HREF,
        useValue: basePath
      },
      ...appConfig.providers
    ]
  });

  return app;
}

bootstrap().catch(err => console.error(err));
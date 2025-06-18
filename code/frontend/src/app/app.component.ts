import { Component, OnInit, inject } from '@angular/core';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';
import { BasePathService } from './core/services/base-path.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [MainLayoutComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  title = 'Cleanuparr';

  private basePathService = inject(BasePathService);

  ngOnInit(): void {
    // BasePathService will automatically detect the base path in its constructor
    // We can subscribe to changes if needed
    this.basePathService.basePath$.subscribe(basePath => {
      console.log('Base path detected:', basePath);
    });
  }
}

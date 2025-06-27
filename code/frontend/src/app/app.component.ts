import { Component, inject } from '@angular/core';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, MainLayoutComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'Cleanuparr';
}

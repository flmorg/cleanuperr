import { Component, OnInit } from '@angular/core';
import { CommonModule, NgClass } from '@angular/common';
import { RouterLink } from '@angular/router';

// PrimeNG Components
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { ProgressBarModule } from 'primeng/progressbar';
import { TagModule } from 'primeng/tag';
import { TimelineModule } from 'primeng/timeline';

// Models
interface ActivityItem {
  title: string;
  description: string;
  time: string;
  icon: string;
  iconClass: string;
}

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [
    CommonModule,
    NgClass,
    RouterLink,
    CardModule,
    ButtonModule,
    ProgressBarModule,
    TagModule,
    TimelineModule
  ],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss'
})
export class DashboardPageComponent implements OnInit {
  // Sample activity items for the timeline
  activityItems: ActivityItem[] = [];

  ngOnInit() {
    // Initialize dashboard data
    this.initializeActivityData();
  }

  refreshDashboard() {
    console.log('Refreshing dashboard data...');
    // Here you would normally fetch new data
    // For now, we'll just reinitialize the demo data
    this.initializeActivityData();
  }

  private initializeActivityData() {
    // Sample activity data
    this.activityItems = [
      {
        title: 'System started',
        description: 'Application services initialized successfully',
        time: '10 minutes ago',
        icon: 'pi pi-power-off',
        iconClass: 'bg-primary'
      },
      {
        title: 'Database backup completed',
        description: 'Automatic backup task executed successfully',
        time: '2 hours ago',
        icon: 'pi pi-database',
        iconClass: 'bg-success'
      },
      {
        title: 'Configuration updated',
        description: 'System configuration changes applied',
        time: 'Yesterday, 14:23',
        icon: 'pi pi-cog',
        iconClass: 'bg-info'
      },
      {
        title: 'System update available',
        description: 'New version 1.2.5 is available for installation',
        time: '2 days ago',
        icon: 'pi pi-download',
        iconClass: 'bg-warning'
      }
    ];
  }
}

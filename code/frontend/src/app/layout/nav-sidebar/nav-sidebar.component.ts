import { Component, ChangeDetectionStrategy, input, output, signal, inject, computed } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { LogoComponent } from '@ui';
import { AppHubService } from '@core/realtime/app-hub.service';
import { AuthService } from '@core/auth/auth.service';
import { ThemeService } from '@core/services/theme.service';
import { ROUTES } from '@shared/routes';

interface NavItem {
  label: string;
  icon?: string;
  route: string;
}

@Component({
  selector: 'app-nav-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, NgIcon, LogoComponent],
  templateUrl: './nav-sidebar.component.html',
  styleUrl: './nav-sidebar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NavSidebarComponent {
  private readonly hub = inject(AppHubService);
  private readonly auth = inject(AuthService);
  protected readonly themeService = inject(ThemeService);

  readonly theme = this.themeService.theme;

  collapsed = input(false);
  mobileOpen = input(false);
  isMobile = input(false);
  navClicked = output<void>();

  settingsExpanded = signal(true);

  readonly currentVersion = computed(() => this.hub.appStatus()?.currentVersion ?? null);
  readonly latestVersion = computed(() => this.hub.appStatus()?.latestVersion ?? null);
  readonly hasUpdate = computed(() => {
    const current = this.currentVersion();
    const latest = this.latestVersion();
    return !!current && !!latest && current !== latest;
  });

  mainNavItems: NavItem[] = [
    { label: 'Dashboard', icon: 'tablerLayoutDashboard', route: ROUTES.dashboard },
    { label: 'Logs', icon: 'tablerFileText', route: '/logs' },
    { label: 'Events', icon: 'tablerBell', route: '/events' },
    { label: 'Strikes', icon: 'tablerBolt', route: '/strikes' },
    { label: 'Seeker Stats', icon: 'tablerChartDots', route: '/seeker-stats' },
  ];

  settingsItems: NavItem[] = [
    { label: 'General', icon: 'tablerSettings', route: '/settings/general' },
    { label: 'Queue Cleaner', icon: 'tablerPlaylistX', route: '/settings/queue-cleaner' },
    { label: 'Malware Blocker', icon: 'tablerShieldLock', route: '/settings/malware-blocker' },
    { label: 'Download Cleaner', icon: 'tablerTrash', route: '/settings/download-cleaner' },
    { label: 'Blacklist Sync', icon: 'tablerBan', route: '/settings/blacklist-sync' },
    { label: 'Seeker', icon: 'tablerSearch', route: '/settings/seeker' },
    { label: 'Media Managers', icon: 'tablerApps', route: '/settings/arr' },
    { label: 'Download Clients', icon: 'tablerDownload', route: '/settings/download-clients' },
  ];

  otherSettingsItems: NavItem[] = [
    { label: 'Notifications', icon: 'tablerBellRinging', route: '/settings/notifications' },
    { label: 'Account', icon: 'tablerUser', route: '/settings/account' },
    { label: 'Appearance', icon: 'tablerPalette', route: '/settings/appearance' },
  ];

  onNavItemClick(): void {
    if (this.isMobile()) {
      this.navClicked.emit();
    }
  }

  toggleSettings(): void {
    this.settingsExpanded.set(!this.settingsExpanded());
  }

  logout(): void {
    this.auth.logout();
  }
}

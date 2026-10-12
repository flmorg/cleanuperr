import { Component, ChangeDetectionStrategy, computed, input, output, inject } from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { ThemeService } from '@core/services/theme.service';
import { ToggleComponent, TooltipComponent } from '@ui';

@Component({
  selector: 'app-toolbar',
  standalone: true,
  imports: [NgIcon, ToggleComponent, TooltipComponent],
  templateUrl: './toolbar.component.html',
  styleUrl: './toolbar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToolbarComponent {
  sidebarCollapsed = input(false);
  toggleSidebar = output<void>();

  private themeService = inject(ThemeService);

  theme = this.themeService.theme;
  preference = this.themeService.preference;
  performanceMode = this.themeService.performanceMode;
  fullWidth = this.themeService.fullWidth;

  themeIcon = computed(() => {
    switch (this.preference()) {
      case 'dark':
        return 'tablerMoon';
      case 'light':
        return 'tablerSun';
      default:
        return 'tablerDeviceDesktop';
    }
  });

  themeTooltip = computed(() => {
    const preference = this.preference();
    if (preference === 'dark')
    {
      return 'Theme: Dark. Click for light.';
    }
    if (preference === 'light')
    {
      return 'Theme: Light. Click to follow system.';
    }
    return `Theme: System (${this.theme()}). Click for dark.`;
  });

  onToggleTheme(): void {
    this.themeService.cycleTheme();
  }

  onTogglePerformanceMode(): void {
    this.themeService.togglePerformanceMode();
  }

  onToggleFullWidth(): void {
    this.themeService.toggleFullWidth();
  }

  onToggleSidebar(): void {
    this.toggleSidebar.emit();
  }
}

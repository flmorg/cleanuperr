import { WritableSignal, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { NgIcon } from '@ng-icons/core';
import { ThemeService, ThemePreference, Theme } from '@core/services/theme.service';
import { ToolbarComponent } from './toolbar.component';

interface Harness {
  fixture: ComponentFixture<ToolbarComponent>;
  preference: WritableSignal<ThemePreference>;
  theme: WritableSignal<Theme>;
  cycleTheme: () => void;
}

describe('ToolbarComponent', () => {
  function setup(): Harness {
    const preference = signal<ThemePreference>('dark');
    const theme = signal<Theme>('dark');
    const cycleTheme = vi.fn();

    TestBed.configureTestingModule({
      providers: [
        {
          provide: ThemeService,
          useValue: {
            preference,
            theme,
            performanceMode: signal(false),
            fullWidth: signal(false),
            cycleTheme,
            togglePerformanceMode: vi.fn(),
            toggleFullWidth: vi.fn(),
          },
        },
      ],
    });

    const fixture = TestBed.createComponent(ToolbarComponent);
    fixture.detectChanges();
    return { fixture, preference, theme, cycleTheme };
  }

  function themeButton(fixture: ComponentFixture<ToolbarComponent>): HTMLButtonElement {
    return fixture.nativeElement.querySelector('.toolbar__icon-btn') as HTMLButtonElement;
  }

  function themeIconName(fixture: ComponentFixture<ToolbarComponent>): unknown {
    const icon = fixture.debugElement
      .query(By.css('.toolbar__icon-btn'))
      .query(By.directive(NgIcon));
    return icon.componentInstance.name();
  }

  it('shows the moon icon and dark tooltip for the dark preference', () => {
    const { fixture } = setup();

    expect(themeIconName(fixture)).toBe('tablerMoon');
    expect(themeButton(fixture).getAttribute('aria-label')).toBe('Theme: Dark. Click for light.');
  });

  it('shows the sun icon and light tooltip for the light preference', () => {
    const { fixture, preference, theme } = setup();

    preference.set('light');
    theme.set('light');
    fixture.detectChanges();

    expect(themeIconName(fixture)).toBe('tablerSun');
    expect(themeButton(fixture).getAttribute('aria-label')).toBe('Theme: Light. Click to follow system.');
  });

  it('shows the desktop icon and resolved theme for the system preference', () => {
    const { fixture, preference, theme } = setup();

    preference.set('system');
    theme.set('dark');
    fixture.detectChanges();

    expect(themeIconName(fixture)).toBe('tablerDeviceDesktop');
    expect(themeButton(fixture).getAttribute('aria-label')).toBe('Theme: System (dark). Click for dark.');

    theme.set('light');
    fixture.detectChanges();

    expect(themeButton(fixture).getAttribute('aria-label')).toBe('Theme: System (light). Click for dark.');
  });

  it('cycles the theme preference on click', () => {
    const { fixture, cycleTheme } = setup();

    themeButton(fixture).click();
    fixture.detectChanges();

    expect(cycleTheme).toHaveBeenCalledTimes(1);
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import {
  ACCENT_PRESETS,
  ACCENT_PRESET_HEX,
  Accent,
  ThemePreference,
  ThemeService,
} from '@core/services/theme.service';
import { FeatureBadgeService } from '@core/feature-badges/feature-badge.service';
import { AppearanceSettingsComponent } from './appearance-settings.component';

function createThemeService() {
  return {
    preference: signal<ThemePreference>('system'),
    accent: signal<Accent>('default'),
    customAccent: signal<string>('#8b5cf6'),
    setTheme: vi.fn(),
    setAccent: vi.fn(),
    setCustomAccent: vi.fn(),
  };
}

interface Setup {
  fixture: ComponentFixture<AppearanceSettingsComponent>;
  themeService: ReturnType<typeof createThemeService>;
}

function setup(): Setup {
  const themeService = createThemeService();

  TestBed.configureTestingModule({
    providers: [
      { provide: ThemeService, useValue: themeService },
      { provide: FeatureBadgeService, useValue: { isNew: vi.fn(() => false) } },
    ],
  });

  const fixture = TestBed.createComponent(AppearanceSettingsComponent);
  fixture.detectChanges();

  return { fixture, themeService };
}

function themeOptions(fixture: ComponentFixture<AppearanceSettingsComponent>): HTMLButtonElement[] {
  return Array.from(fixture.nativeElement.querySelectorAll('.theme-option'));
}

describe('AppearanceSettingsComponent', () => {
  it('renders dark, light and system theme options', () => {
    const { fixture } = setup();

    const labels = themeOptions(fixture).map((button) => button.textContent?.trim());

    expect(labels.length).toBe(3);
    expect(labels[0]).toContain('Dark');
    expect(labels[1]).toContain('Light');
    expect(labels[2]).toContain('System');
  });

  it('marks the option matching the current preference as active', () => {
    const { fixture, themeService } = setup();

    const [dark, light, system] = themeOptions(fixture);
    expect(system.classList).toContain('theme-option--active');
    expect(dark.classList).not.toContain('theme-option--active');
    expect(light.classList).not.toContain('theme-option--active');

    themeService.preference.set('dark');
    fixture.detectChanges();

    expect(dark.classList).toContain('theme-option--active');
    expect(system.classList).not.toContain('theme-option--active');
  });

  it('exposes the current preference through aria-pressed', () => {
    const { fixture, themeService } = setup();

    const [dark, light, system] = themeOptions(fixture);
    expect(system.getAttribute('aria-pressed')).toBe('true');
    expect(dark.getAttribute('aria-pressed')).toBe('false');
    expect(light.getAttribute('aria-pressed')).toBe('false');

    themeService.preference.set('light');
    fixture.detectChanges();

    expect(light.getAttribute('aria-pressed')).toBe('true');
    expect(system.getAttribute('aria-pressed')).toBe('false');
  });

  it('calls setTheme with the clicked preference', () => {
    const { fixture, themeService } = setup();

    const [dark, light, system] = themeOptions(fixture);

    system.click();
    fixture.detectChanges();
    expect(themeService.setTheme).toHaveBeenCalledWith('system');

    dark.click();
    fixture.detectChanges();
    expect(themeService.setTheme).toHaveBeenCalledWith('dark');

    light.click();
    fixture.detectChanges();
    expect(themeService.setTheme).toHaveBeenCalledWith('light');
  });

  it('marks the active accent swatch and switches on click', () => {
    const { fixture, themeService } = setup();

    const swatches = Array.from<HTMLButtonElement>(
      fixture.nativeElement.querySelectorAll('.swatch:not(.swatch--custom)'),
    );
    expect(swatches.length).toBe(ACCENT_PRESETS.length);
    expect(swatches[0].classList).toContain('swatch--active');

    const greenIndex = ACCENT_PRESETS.indexOf('green');
    swatches[greenIndex].click();
    fixture.detectChanges();

    expect(themeService.setAccent).toHaveBeenCalledWith('green');
  });

  it('uses the registered preset hex colors for the swatch dots', () => {
    const { fixture } = setup();

    const dots = Array.from<HTMLElement>(fixture.nativeElement.querySelectorAll('.swatch__dot'));
    const probe = document.createElement('div');

    ACCENT_PRESETS.forEach((preset, index) => {
      probe.style.background = ACCENT_PRESET_HEX[preset];
      expect(dots[index].style.background).toBe(probe.style.background);
    });
  });
});

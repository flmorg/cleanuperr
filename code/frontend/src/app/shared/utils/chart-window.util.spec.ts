import {
  bucketForWindow,
  chartYDomain,
  formatBucketDate,
  getChartDuration,
  WINDOWS,
} from './chart-window.util';

describe('chart-window.util', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  describe('bucketForWindow', () => {
    it('switches bucket exactly at the 24h and 720h boundaries', () => {
      expect(bucketForWindow(24)).toBe('hour');
      expect(bucketForWindow(25)).toBe('day');
      expect(bucketForWindow(720)).toBe('day');
      expect(bucketForWindow(721)).toBe('month');
    });

    it('maps every offered window to a bucket', () => {
      expect(WINDOWS.map((w) => bucketForWindow(w.hours))).toEqual([
        'hour',
        'day',
        'day',
        'month',
      ]);
    });
  });

  describe('chartYDomain', () => {
    it('falls back to a unit domain when there is nothing to plot', () => {
      expect(chartYDomain([])).toEqual([0, 1]);
      expect(chartYDomain([0, 0])).toEqual([0, 1]);
    });

    it('rounds the maximum up', () => {
      expect(chartYDomain([2.3])).toEqual([0, 3]);
    });

    it('clamps negative values to zero', () => {
      expect(chartYDomain([-5, -1])).toEqual([0, 1]);
    });
  });

  describe('formatBucketDate', () => {
    afterEach(() => {
      vi.restoreAllMocks();
    });

    const forceTwelveHourLocale = () => {
      const original = Date.prototype.toLocaleTimeString;
      vi.spyOn(Date.prototype, 'toLocaleTimeString').mockImplementation(function (this: Date, _locale, options) {
        return original.call(this, 'en-US', options);
      });
    };

    it('keeps the calendar day of the bucket regardless of the local timezone', () => {
      expect(formatBucketDate('2026-03-15T23:00:00Z', 168)).toContain('15');
    });

    it('formats yearly windows differently from monthly ones', () => {
      const date = '2026-03-15T23:00:00Z';

      expect(formatBucketDate(date, 8760)).not.toBe(formatBucketDate(date, 720));
    });

    it('formats 24h windows as a time rather than a date', () => {
      const formatted = formatBucketDate('2026-03-15T23:00:00Z', 24);

      expect(formatted).toMatch(/\d/);
      expect(formatted).not.toBe(formatBucketDate('2026-03-15T23:00:00Z', 168));
    });

    it('formats 24h windows with a 24-hour clock', () => {
      forceTwelveHourLocale();

      expect(formatBucketDate(new Date(2026, 2, 15, 13, 0).toISOString(), 24)).toBe('13:00');
    });

    it('formats midnight as 00 rather than 24', () => {
      forceTwelveHourLocale();

      expect(formatBucketDate(new Date(2026, 2, 15, 0, 0).toISOString(), 24)).toBe('00:00');
    });
  });

  describe('getChartDuration', () => {
    it('disables animation when the user prefers reduced motion', () => {
      vi.stubGlobal('matchMedia', () => ({ matches: true }));

      expect(getChartDuration()).toBe(0);
    });

    it('animates otherwise', () => {
      vi.stubGlobal('matchMedia', () => ({ matches: false }));

      expect(getChartDuration()).toBe(600);
    });
  });
});

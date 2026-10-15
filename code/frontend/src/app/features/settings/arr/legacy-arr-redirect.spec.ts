import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { legacyArrRedirect } from './legacy-arr-redirect';

describe('legacyArrRedirect', () => {
  it('redirects /settings/arr/:type to /settings/arr with fragment', () => {
    TestBed.runInInjectionContext(() => {
      const result = legacyArrRedirect({ params: { type: 'radarr' } } as unknown as Parameters<typeof legacyArrRedirect>[0]);

      expect(result).toEqual(
        TestBed.inject(Router).createUrlTree(['/settings/arr'], { fragment: 'radarr' }),
      );
    });
  });

  it('handles sonarr type', () => {
    TestBed.runInInjectionContext(() => {
      const result = legacyArrRedirect({ params: { type: 'sonarr' } } as unknown as Parameters<typeof legacyArrRedirect>[0]);
      const expected = TestBed.inject(Router).createUrlTree(['/settings/arr'], { fragment: 'sonarr' });

      expect(result.toString()).toEqual(expected.toString());
    });
  });

  it('handles lazylibrarian type', () => {
    TestBed.runInInjectionContext(() => {
      const result = legacyArrRedirect({ params: { type: 'lazylibrarian' } } as unknown as Parameters<typeof legacyArrRedirect>[0]);
      const expected = TestBed.inject(Router).createUrlTree(['/settings/arr'], { fragment: 'lazylibrarian' });

      expect(result.toString()).toEqual(expected.toString());
    });
  });
});

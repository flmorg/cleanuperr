import { Router, RedirectFunction } from '@angular/router';
import { inject } from '@angular/core';

export const legacyArrRedirect: RedirectFunction = ({ params }) => {
  return inject(Router).createUrlTree(['/settings/arr'], { fragment: params['type'] });
};

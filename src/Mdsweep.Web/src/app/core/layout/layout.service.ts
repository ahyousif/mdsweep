import { BreakpointObserver } from '@angular/cdk/layout';
import { inject, Service } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';

// Keep aligned with Tailwind's xl breakpoint used for side-by-side detail panels.
const DESKTOP_QUERY = '(min-width: 80rem)';

@Service()
export class LayoutService {
  private readonly breakpoints = inject(BreakpointObserver);

  readonly isDesktop = toSignal(
    this.breakpoints.observe(DESKTOP_QUERY).pipe(map((state) => state.matches)),
    { initialValue: this.breakpoints.isMatched(DESKTOP_QUERY) },
  );
}

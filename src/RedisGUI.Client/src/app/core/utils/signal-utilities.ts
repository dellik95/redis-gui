import { DestroyRef, inject, Signal, signal } from '@angular/core';

/**
 * Custom Angular Signal Hooks
 * Composable utilities for common signal patterns in Angular 21
 */

/**
 * Creates a signal that tracks media query matches
 *
 * @param query - Media query string
 * @returns Signal that is true when media query matches
 *
 * @example
 * ```typescript
 * readonly isMobile = useMediaQuery('(max-width: 768px)');
 * readonly isDark = useMediaQuery('(prefers-color-scheme: dark)');
 * ```
 */
export function useMediaQuery(query: string): Signal<boolean> {
  const mediaQuery = window.matchMedia(query);
  const matches = signal(mediaQuery.matches);
  const updateMatches = (e: MediaQueryListEvent | MediaQueryList) => {
    matches.set(e.matches);
  };

  mediaQuery.addEventListener('change', updateMatches);

  inject(DestroyRef).onDestroy(() => mediaQuery.removeEventListener('change', updateMatches));
  return matches.asReadonly();
}

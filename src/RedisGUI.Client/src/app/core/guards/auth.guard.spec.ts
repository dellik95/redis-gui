import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { ActivatedRouteSnapshot, provideRouter, RouterStateSnapshot } from '@angular/router';
import { authGuard, guestOnlyGuard } from './auth.guard';

describe('session guards on a browser refresh', () => {
  const route = new ActivatedRouteSnapshot();
  const state = {} as RouterStateSnapshot;
  let token: string | null;
  let user: string | null;

  beforeEach(() => {
    token = localStorage.getItem('auth_token');
    user = localStorage.getItem('current_user');
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideRouter([])] });
  });

  afterEach(() => {
    for (const [key, value] of [
      ['auth_token', token],
      ['current_user', user]
    ] as const) {
      if (value === null) localStorage.removeItem(key);
      else localStorage.setItem(key, value);
    }
  });

  it('restores a valid persisted session and rejects the same token when it expires', () => {
    const now = spyOn(Date, 'now').and.returnValue(1000);
    localStorage.setItem('auth_token', 'header.' + btoa(JSON.stringify({ exp: 2 })) + '.signature');
    localStorage.setItem(
      'current_user',
      JSON.stringify({
        id: 'test',
        firstName: 'Test',
        lastName: 'User',
        email: 'user@example.org',
        role: 'Admin'
      })
    );
    expect(TestBed.runInInjectionContext(() => authGuard(route, state))).toBeTrue();
    expect(String(TestBed.runInInjectionContext(() => guestOnlyGuard(route, state)))).toBe('/home');
    now.and.returnValue(2000);
    expect(String(TestBed.runInInjectionContext(() => authGuard(route, state)))).toBe('/login');
  });

  it('allows sign-in when storage contains a malformed token', () => {
    localStorage.setItem('auth_token', 'invalid');
    expect(TestBed.runInInjectionContext(() => guestOnlyGuard(route, state))).toBeTrue();
    expect(localStorage.getItem('auth_token')).toBeNull();
  });
});

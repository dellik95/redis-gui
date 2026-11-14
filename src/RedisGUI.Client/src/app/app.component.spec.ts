import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { AppComponent } from './app.component';
import { AuthService } from './core/services/auth/auth.service';

describe('shell density', () => {
  it('restores the saved preference and applies later changes to the root', () => {
    const old = localStorage.getItem('appDensity');
    const root = document.documentElement;
    const classes = root.className;
    try {
      localStorage.setItem('appDensity', 'comfortable');
      TestBed.configureTestingModule({
        providers: [provideRouter([]), provideHttpClient(), { provide: AuthService, useValue: {} }]
      });
      const fixture = TestBed.createComponent(AppComponent);
      fixture.detectChanges();
      expect(root.classList.contains('density-comfortable')).toBeTrue();
      fixture.componentInstance.setDensity('compact');
      fixture.detectChanges();
      expect(localStorage.getItem('appDensity')).toBe('compact');
      expect(root.classList.contains('density-compact')).toBeTrue();
      expect(root.classList.contains('density-comfortable')).toBeFalse();
      fixture.destroy();
    } finally {
      if (old === null) localStorage.removeItem('appDensity');
      else localStorage.setItem('appDensity', old);
      root.className = classes;
    }
  });
});

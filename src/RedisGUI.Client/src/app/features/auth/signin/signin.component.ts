import {
  Component,
  inject,
  signal,
  computed,
  ChangeDetectionStrategy,
  DestroyRef
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { form, FormField, required, email, minLength, submit } from '@angular/forms/signals';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../../core/services/auth/auth.service';
import { LoginRequest } from '../../../core/models/auth';
import { UiInputComponent, UiButtonComponent } from '../../../shared/ui';

/**
 * Sign In Component
 * Handles user authentication/login with Angular Signal Forms
 */
@Component({
  selector: 'app-signin',
  standalone: true,
  imports: [
    FormField,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    RouterLink,
    UiInputComponent,
    UiButtonComponent
  ],
  templateUrl: './signin.component.html',
  styleUrl: './signin.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SigninComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  /** Loading state during authentication */
  readonly isLoading = signal(false);

  /** Password visibility toggle state */
  readonly hidePassword = signal(true);

  /** Signal model for login credentials */
  readonly loginModel = signal({
    email: 'admin@redisgui.local',
    password: 'AdminPassword123!'
  });

  /**
   * Signal Form definition with schema validations
   */
  readonly loginForm = form(this.loginModel, s => {
    required(s.email, { message: 'Email is required' });
    email(s.email, { message: 'Please enter a valid email address' });
    required(s.password, { message: 'Password is required' });
    minLength(s.password, 6, { message: 'Password must be at least 6 characters' });
  });

  /** Whether the form can be submitted */
  readonly canSubmit = computed(() => {
    return this.loginForm().valid() && !this.isLoading();
  });

  /**
   * Resets form with demo admin credentials
   */
  fillDemoCredentials(): void {
    this.loginModel.set({
      email: 'admin@redisgui.local',
      password: 'AdminPassword123!'
    });
  }

  /**
   * Handles form submission for user login using Signal Forms submit()
   */
  onSubmit(event?: Event): void {
    if (event) event.preventDefault();

    submit(this.loginForm, async () => {
      this.isLoading.set(true);

      const request: LoginRequest = {
        email: this.loginModel().email,
        password: this.loginModel().password
      };

      this.authService
        .login(request)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.snackBar.open('Login successful! Welcome to RedisGUI.', 'Close', {
              duration: 3000,
              horizontalPosition: 'end',
              verticalPosition: 'top',
              panelClass: ['success-snackbar']
            });
            this.router.navigate(['/home']);
          },
          error: error => {
            this.isLoading.set(false);
            const errorMessage =
              error.error?.message || error.error?.detail || 'Invalid email or password';
            this.snackBar.open(errorMessage, 'Close', {
              duration: 5000,
              horizontalPosition: 'end',
              verticalPosition: 'top',
              panelClass: ['error-snackbar']
            });
          },
          complete: () => {
            this.isLoading.set(false);
          }
        });
    });
  }

  /**
   * Toggles password visibility
   */
  togglePasswordVisibility(): void {
    this.hidePassword.update(visible => !visible);
  }
}

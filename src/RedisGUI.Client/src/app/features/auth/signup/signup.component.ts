import {
  Component,
  inject,
  signal,
  computed,
  ChangeDetectionStrategy,
  DestroyRef
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import {
  form,
  FormField,
  required,
  email,
  minLength,
  validate,
  submit
} from '@angular/forms/signals';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../../core/services/auth/auth.service';
import { RegisterRequest } from '../../../core/models/auth';
import { UiInputComponent, UiButtonComponent } from '../../../shared/ui';

/**
 * Sign Up Component
 * Handles new user registration with Angular Signal Forms
 */
@Component({
  selector: 'app-signup',
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
  templateUrl: './signup.component.html',
  styleUrl: './signup.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SignupComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  /** Loading state during registration */
  readonly isLoading = signal(false);

  /** Password visibility toggle state */
  readonly hidePassword = signal(true);

  /** Confirm password visibility toggle state */
  readonly hideConfirmPassword = signal(true);

  /** Registration signal model */
  readonly registerModel = signal({
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    confirmPassword: ''
  });

  /** Registration Signal Form with declarative schema rules */
  readonly registerForm = form(this.registerModel, s => {
    required(s.firstName, { message: 'First name is required' });
    minLength(s.firstName, 2, { message: 'First name must be at least 2 characters' });

    required(s.lastName, { message: 'Last name is required' });
    minLength(s.lastName, 2, { message: 'Last name must be at least 2 characters' });

    required(s.email, { message: 'Email is required' });
    email(s.email, { message: 'Please enter a valid email address' });

    required(s.password, { message: 'Password is required' });
    minLength(s.password, 6, { message: 'Password must be at least 6 characters' });

    required(s.confirmPassword, { message: 'Please confirm your password' });
    validate(s.confirmPassword, ctx => {
      if (ctx.value() !== this.registerModel().password) {
        return { kind: 'passwordMismatch', message: 'Passwords do not match' };
      }
      return null;
    });
  });

  /** Whether the form can be submitted */
  readonly canSubmit = computed(() => {
    return this.registerForm().valid() && !this.isLoading();
  });

  /**
   * Pre-fills demo user data for quick testing
   */
  fillDemoCredentials(): void {
    this.registerModel.set({
      firstName: 'Demo',
      lastName: 'User',
      email: 'demo@redisgui.local',
      password: 'AdminPassword123!',
      confirmPassword: 'AdminPassword123!'
    });
  }

  /**
   * Handles form submission for user registration using Signal Forms submit()
   */
  onSubmit(event?: Event): void {
    if (event) event.preventDefault();

    submit(this.registerForm, async () => {
      this.isLoading.set(true);

      const { firstName, lastName, email, password } = this.registerModel();
      const request: RegisterRequest = {
        firstName,
        lastName,
        email,
        password
      };

      this.authService
        .register(request)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.snackBar.open('Registration successful! Please sign in.', 'Close', {
              duration: 4000,
              horizontalPosition: 'end',
              verticalPosition: 'top',
              panelClass: ['success-snackbar']
            });
            this.router.navigate(['/login']);
          },
          error: error => {
            this.isLoading.set(false);
            const errorMessage =
              error.error?.message || error.error?.detail || 'Registration failed';
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

  togglePasswordVisibility(): void {
    this.hidePassword.update(visible => !visible);
  }

  toggleConfirmPasswordVisibility(): void {
    this.hideConfirmPassword.update(visible => !visible);
  }
}

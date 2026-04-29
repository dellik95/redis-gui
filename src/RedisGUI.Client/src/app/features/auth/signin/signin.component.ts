import { Component, inject, signal, computed, ChangeDetectionStrategy } from '@angular/core';

import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../../core/services/auth/auth.service';
import { LoginRequest } from '../../../core/models/auth';

/**
 * Sign In Component
 * Handles user authentication/login with Angular 21 signal-based patterns
 *
 * Features:
 * - Email and password validation
 * - Password visibility toggle
 * - Loading state management
 * - Error handling with user-friendly messages
 * - Automatic redirect to home on successful login
 * - Modern reactive forms with signals
 * - Zoneless change detection compatible
 *
 * @example
 * ```html
 * <app-signin />
 * ```
 */
@Component({
  selector: 'app-signin',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    RouterLink
  ],
  templateUrl: './signin.component.html',
  styleUrl: './signin.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SigninComponent {
  // 1. Injected dependencies
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  // 2. State signals
  /** Loading state during authentication */
  readonly isLoading = signal(false);

  /** Password visibility toggle state */
  readonly hidePassword = signal(true);

  // 3. Form definition
  /** Login form with email and password fields */
  readonly loginForm = new FormGroup({
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email]
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(6)]
    })
  });

  // 4. Form control signals (for reactive access)
  private readonly emailControl = this.loginForm.controls.email;
  private readonly passwordControl = this.loginForm.controls.password;

  // 5. Computed signals for form state
  /** Whether the form can be submitted */
  readonly canSubmit = computed(() =>
    this.loginForm.valid && !this.isLoading()
  );

  /** Email field error message (reactive) */
  readonly emailError = computed(() => {
    const control = this.emailControl;
    if (!control.touched) return null;
    if (control.hasError('required')) return 'Email is required';
    if (control.hasError('email')) return 'Please enter a valid email address';
    return null;
  });

  /** Password field error message (reactive) */
  readonly passwordError = computed(() => {
    const control = this.passwordControl;
    if (!control.touched) return null;
    if (control.hasError('required')) return 'Password is required';
    if (control.hasError('minlength')) return 'Password must be at least 6 characters';
    return null;
  });

  /**
   * Handles form submission for user login
   */
  onSubmit(): void {
    if (!this.loginForm.valid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);

    const request: LoginRequest = {
      email: this.emailControl.value,
      password: this.passwordControl.value
    };

    this.authService.login(request)
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: () => {
          this.snackBar.open('Login successful! Welcome back.', 'Close', {
            duration: 3000,
            horizontalPosition: 'end',
            verticalPosition: 'top',
            panelClass: ['success-snackbar']
          });
          this.router.navigate(['/home']);
        },
        error: (error) => {
          this.isLoading.set(false);
          const errorMessage = error.error?.message || 'Invalid email or password';
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
  }

  /**
   * Toggles password visibility
   */
  togglePasswordVisibility(): void {
    this.hidePassword.update(visible => !visible);
  }
}

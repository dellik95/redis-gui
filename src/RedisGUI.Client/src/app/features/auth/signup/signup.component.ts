import {
  Component,
  inject,
  signal,
  computed,
  ChangeDetectionStrategy
} from '@angular/core';

import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
  AbstractControl,
  ValidationErrors
} from '@angular/forms';
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
import { RegisterRequest } from '../../../core/models/auth';

/**
 * Sign Up Component
 * Handles new user registration with Angular 21 signal-based patterns
 *
 * Features:
 * - Form validation (name, email, password)
 * - Password confirmation matching
 * - Password visibility toggles
 * - Loading state management
 * - Error handling with user-friendly messages
 * - Automatic redirect to login on successful registration
 * - Modern reactive forms with signals
 * - Zoneless change detection compatible
 *
 * @example
 * ```html
 * <app-signup />
 * ```
 */
@Component({
  selector: 'app-signup',
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
  templateUrl: './signup.component.html',
  styleUrl: './signup.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SignupComponent {
  // 1. Injected dependencies
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  // 2. State signals
  /** Loading state during registration */
  readonly isLoading = signal(false);

  /** Password visibility toggle state */
  readonly hidePassword = signal(true);

  /** Confirm password visibility toggle state */
  readonly hideConfirmPassword = signal(true);

  // 3. Form definition
  /** Registration form with validation */
  readonly registerForm = new FormGroup(
    {
      firstName: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.minLength(2)]
      }),
      lastName: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.minLength(2)]
      }),
      email: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.email]
      }),
      password: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.minLength(6)]
      }),
      confirmPassword: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required]
      })
    },
    {
      validators: this.passwordMatchValidator
    }
  );

  // 4. Form control references for easier access
  private readonly firstNameControl = this.registerForm.controls.firstName;
  private readonly lastNameControl = this.registerForm.controls.lastName;
  private readonly emailControl = this.registerForm.controls.email;
  private readonly passwordControl = this.registerForm.controls.password;
  private readonly confirmPasswordControl = this.registerForm.controls.confirmPassword;

  // 5. Computed signals for form state
  /** Whether the form can be submitted */
  readonly canSubmit = computed(() =>
    this.registerForm.valid && !this.isLoading()
  );

  /** First name field error message */
  readonly firstNameError = computed(() => {
    const control = this.firstNameControl;
    if (!control.touched) return null;
    if (control.hasError('required')) return 'First name is required';
    if (control.hasError('minlength')) return 'First name must be at least 2 characters';
    return null;
  });

  /** Last name field error message */
  readonly lastNameError = computed(() => {
    const control = this.lastNameControl;
    if (!control.touched) return null;
    if (control.hasError('required')) return 'Last name is required';
    if (control.hasError('minlength')) return 'Last name must be at least 2 characters';
    return null;
  });

  /** Email field error message */
  readonly emailError = computed(() => {
    const control = this.emailControl;
    if (!control.touched) return null;
    if (control.hasError('required')) return 'Email is required';
    if (control.hasError('email')) return 'Please enter a valid email address';
    return null;
  });

  /** Password field error message */
  readonly passwordError = computed(() => {
    const control = this.passwordControl;
    if (!control.touched) return null;
    if (control.hasError('required')) return 'Password is required';
    if (control.hasError('minlength')) return 'Password must be at least 6 characters';
    return null;
  });

  /** Confirm password field error message */
  readonly confirmPasswordError = computed(() => {
    const control = this.confirmPasswordControl;
    if (!control.touched) return null;
    if (control.hasError('required')) return 'Please confirm your password';
    if (this.registerForm.hasError('passwordMismatch')) return 'Passwords do not match';
    return null;
  });

  /**
   * Custom validator to check if password and confirmPassword match
   */
  private passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password');
    const confirmPassword = control.get('confirmPassword');

    if (!password || !confirmPassword) {
      return null;
    }

    return password.value === confirmPassword.value ? null : { passwordMismatch: true };
  }

  /**
   * Handles form submission for user registration
   */
  onSubmit(): void {
    if (!this.registerForm.valid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);

    const request: RegisterRequest = {
      email: this.emailControl.value,
      password: this.passwordControl.value,
      firstName: this.firstNameControl.value,
      lastName: this.lastNameControl.value
    };

    this.authService
      .register(request)
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: () => {
          this.snackBar.open('Registration successful! Please log in.', 'Close', {
            duration: 4000,
            horizontalPosition: 'end',
            verticalPosition: 'top',
            panelClass: ['success-snackbar']
          });
          this.router.navigate(['/login']);
        },
        error: error => {
          this.isLoading.set(false);
          const errorMessage = error.error?.message || 'Registration failed. Please try again.';
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
    this.hidePassword.update(v => !v);
  }

  /**
   * Toggles confirm password visibility
  /**
   * Toggles confirm password visibility
   */
  toggleConfirmPasswordVisibility(): void {
    this.hideConfirmPassword.update(v => !v);
  }
}

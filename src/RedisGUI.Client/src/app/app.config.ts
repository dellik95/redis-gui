import { ApplicationConfig, provideZonelessChangeDetection } from "@angular/core";
import { provideRouter, withComponentInputBinding } from "@angular/router";
import { routes } from "./app.routes";
import { provideAnimationsAsync } from "@angular/platform-browser/animations/async";
import { provideHttpClient, withInterceptors } from "@angular/common/http";
import { retryInterceptor } from "./core/interceptors/retry.interceptor";
import { errorInterceptor } from "./core/interceptors/error.interceptor";
import { loaderInterceptor } from "./core/interceptors/loader.interceptor";
import { authInterceptor } from "./core/interceptors/auth.interceptor";

/**
 * Application configuration
 * Configures providers for the Angular application including:
 * - Zoneless change detection (Angular 21) for optimal performance
 * - HTTP client with functional interceptors
 * - Router with component input binding for automatic route param injection
 * - Animations
 *
 * Interceptors are executed in order: Auth -> Retry -> Error -> Loader
 */
export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    provideHttpClient(
      withInterceptors([
        authInterceptor,    // First: Add JWT token to requests
        retryInterceptor,  // Second: Retry failed requests
        errorInterceptor,  // Third: Handle errors
        loaderInterceptor  // Last: Show/hide loading indicator
      ])
    ),
    provideRouter(
      routes,
      withComponentInputBinding()
    ),
    provideAnimationsAsync()
  ]
};


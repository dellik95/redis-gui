import { ApplicationConfig, importProvidersFrom, provideZoneChangeDetection } from "@angular/core";
import { provideRouter, withComponentInputBinding, withDebugTracing, withRouterConfig } from "@angular/router";

import { routes } from "./app.routes";
import { provideAnimationsAsync } from "@angular/platform-browser/animations/async";
import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptorsFromDi } from "@angular/common/http";
import { ErrorInterceptor } from "./core/interceptors/error.interceptor";
import { LoaderInterceptor } from "./core/interceptors/loader.interceptor";
import { RetryInterceptor } from "./core/interceptors/retry.interceptor";

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideHttpClient(withInterceptorsFromDi()),
    provideRouter(routes,
      withComponentInputBinding()
    ),
    provideAnimationsAsync(),
    // Interceptors are executed in order: Retry -> Error -> Loader
    {
      provide: HTTP_INTERCEPTORS,
      useClass: RetryInterceptor,
      multi: true
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: ErrorInterceptor,
      multi: true
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: LoaderInterceptor,
      multi: true
    },
    provideAnimationsAsync()
  ]
};
 

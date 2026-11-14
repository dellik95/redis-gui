import { Routes } from '@angular/router';
import { authGuard, guestOnlyGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  // Public routes (only accessible when not authenticated)
  {
    path: '',
    loadComponent: () => import('./features/auth/auth.component').then(x => x.AuthComponent),
    canActivate: [guestOnlyGuard],
    children: [
      {
        path: 'login',
        loadComponent: () =>
          import('./features/auth/signin/signin.component').then(x => x.SigninComponent),
        data: {
          breadcrumb: 'Sign In'
        }
      },
      {
        path: 'register',
        loadComponent: () =>
          import('./features/auth/signup/signup.component').then(x => x.SignupComponent),
        data: {
          breadcrumb: 'Register'
        }
      },
      {
        path: '',
        redirectTo: 'login',
        pathMatch: 'full'
      }
    ]
  },
  // Protected routes (require authentication)
  {
    path: 'home',
    loadComponent: () =>
      import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
    canActivate: [authGuard],
    data: {
      breadcrumb: 'Dashboard'
    }
  },
  {
    path: 'storage/:id',
    loadComponent: () =>
      import('./features/storage-management/storage-management.component').then(
        x => x.StorageManagementComponent
      ),
    canActivate: [authGuard],
    data: {
      breadcrumb: 'Storage Manager'
    }
  },
  {
    path: 'health',
    loadComponent: () =>
      import('./features/health-monitoring/health-monitoring.component').then(
        x => x.HealthMonitoringComponent
      ),
    canActivate: [authGuard],
    data: {
      breadcrumb: 'Health Monitoring'
    }
  },
  {
    path: 'audit',
    loadComponent: () =>
      import('./features/audit-logs/audit-logs.component').then(x => x.AuditLogsComponent),
    canActivate: [authGuard],
    data: {
      breadcrumb: 'Audit Logs'
    }
  },
  {
    path: 'info',
    loadComponent: () => import('./features/info/info.component').then(m => m.InfoComponent),
    canActivate: [authGuard],
    data: {
      breadcrumb: 'Information'
    }
  },
  {
    path: 'info/:connectionId',
    loadComponent: () => import('./features/info/info.component').then(m => m.InfoComponent),
    canActivate: [authGuard],
    data: {
      breadcrumb: 'Information'
    }
  },
  // Fallback redirect
  {
    path: '**',
    redirectTo: '/login',
    pathMatch: 'full'
  }
];

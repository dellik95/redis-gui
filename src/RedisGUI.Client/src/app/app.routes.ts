import { Routes } from "@angular/router";
import { DashboardComponent } from "./features/dashboard/dashboard.component";
import { InfoComponent } from "./features/info/info.component";
import { authGuard, guestOnlyGuard } from "./core/guards/auth.guard";

export const routes: Routes = [
    // Public routes (only accessible when not authenticated)
    {
        path: '',
        loadComponent: () => import("./features/auth/auth.component").then(x => x.AuthComponent),
        canActivate: [guestOnlyGuard],
        children: [
            {
                path: 'login',
                loadComponent: () => import("./features/auth/signin/signin.component").then(x => x.SigninComponent),
                data: {
                    breadcrumb: "Sign In"
                }
            },
            {
                path: 'register',
                loadComponent: () => import("./features/auth/signup/signup.component").then(x => x.SignupComponent),
                data: {
                    breadcrumb: "Register"
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
        component: DashboardComponent,
        canActivate: [authGuard],
        data: {
            breadcrumb: "Dashboard"
        }
    },
    {
        path: "storage/:id",
        loadComponent: () => import("./features/storage-management/storage-management.component").then(x => x.StorageManagementComponent),
        canActivate: [authGuard],
        data: {
            breadcrumb: "Storage Manager"
        }
    },
    {
        path: "transaction/:id",
        loadComponent: () => import("./features/redis-operations/transaction/transaction-builder.component").then(x => x.TransactionBuilderComponent),
        canActivate: [authGuard],
        data: {
            breadcrumb: "Transactions"
        }
    },
    {
        path: "lua/:id",
        loadComponent: () => import("./features/redis-operations/lua/lua-script-console.component").then(x => x.LuaScriptConsoleComponent),
        canActivate: [authGuard],
        data: {
            breadcrumb: "Lua Scripts"
        }
    },
    {
        path: "pubsub/:id",
        loadComponent: () => import("./features/redis-operations/pubsub/pub-sub-panel.component").then(x => x.PubSubPanelComponent),
        canActivate: [authGuard],
        data: {
            breadcrumb: "Pub/Sub"
        }
    },
    {
        path: 'health',
        loadComponent: () => import("./features/health-monitoring/health-monitoring.component").then(x => x.HealthMonitoringComponent),
        canActivate: [authGuard],
        data: {
            breadcrumb: "Health Monitoring"
        }
    },
    {
        path: 'audit',
        loadComponent: () => import("./features/audit-logs/audit-logs.component").then(x => x.AuditLogsComponent),
        canActivate: [authGuard],
        data: {
            breadcrumb: "Audit Logs"
        }
    },
    {
        path: 'info', 
        component: InfoComponent,
        canActivate: [authGuard],
        data: {
            breadcrumb: "Information"
        }
    },
    // Fallback redirect
    {
        path: '**', 
        redirectTo: '/login', 
        pathMatch: 'full'
    }
];

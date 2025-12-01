import { Routes } from "@angular/router";
import { DashboardComponent } from "./features/dashboard/dashboard.component";
import { InfoComponent } from "./features/info/info.component";

export const routes: Routes = [
    {
        path: 'home',
        component: DashboardComponent,
        data: {
            breadcrumb: "Dashboard"
        }
    },
    {
        path: "storage/:id",
        loadComponent: () => import("./features/storage-management/storage-management.component").then(x => x.StorageManagementComponent),
        data: {
            breadcrumb: "Storage Manager"
        }
    },
    {
        path: "transaction/:id",
        loadComponent: () => import("./features/redis-operations/transaction/transaction-builder.component").then(x => x.TransactionBuilderComponent),
        data: {
            breadcrumb: "Transactions"
        }
    },
    {
        path: "lua/:id",
        loadComponent: () => import("./features/redis-operations/lua/lua-script-console.component").then(x => x.LuaScriptConsoleComponent),
        data: {
            breadcrumb: "Lua Scripts"
        }
    },
    {
        path: "pubsub/:id",
        loadComponent: () => import("./features/redis-operations/pubsub/pub-sub-panel.component").then(x => x.PubSubPanelComponent),
        data: {
            breadcrumb: "Pub/Sub"
        }
    },
    {
        path: 'info', 
        component: InfoComponent,
        data: {
            breadcrumb: "Information"
        }
    },
    {
        path: '', redirectTo: '/home', pathMatch: 'full'
    },
];

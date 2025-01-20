import { Routes } from "@angular/router";
import { DashboardComponent } from "./dashboard/dashboard.component";
import { InfoComponent } from "./info/info.component";

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
        loadComponent: () => import("./storage-management/storage-management.component").then(x => x.StorageManagementComponent),
        data: {
            breadcrumb: "Storage manager"
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

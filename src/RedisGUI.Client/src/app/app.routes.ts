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

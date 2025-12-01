import { ChangeDetectorRef, Component, inject } from "@angular/core";
import { RouterLink, RouterLinkActive, RouterOutlet } from "@angular/router";
import { LoadingComponent } from "./shared/components/loading/loading.component";
import { LoadingService } from "./shared/components/loading/loading.service";
import { MatToolbarModule } from "@angular/material/toolbar";
import { MediaMatcher } from "@angular/cdk/layout";
import { CommonModule } from "@angular/common";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatListModule } from "@angular/material/list";
import { MatSidenavModule } from "@angular/material/sidenav";
import { MatMenuModule } from "@angular/material/menu";
import { MatCheckboxModule } from "@angular/material/checkbox";
import { MatDividerModule } from "@angular/material/divider";
import { SideNavService } from "./shared/services/side-nav.service";
import { BreadcrumbComponent } from "./shared/components/breadcrumb/breadcrumb.component";
import { SectionActionsComponent } from "./shared/components/section-actions/section-actions.component";
import { ThemePicker } from "./shared/ui/theme-picker/theme-picker";

@Component({
	selector: "app-root",
	imports: [RouterOutlet, LoadingComponent, BreadcrumbComponent, MatToolbarModule, MatButtonModule, MatSidenavModule, MatListModule, MatIconModule, MatMenuModule, MatCheckboxModule, MatDividerModule, RouterLink, RouterLinkActive, SectionActionsComponent, CommonModule, ThemePicker],
	templateUrl: "./app.component.html",
	styleUrl: "./app.component.scss",
})
export class AppComponent {
	title = "Wunderwaffe";
	mobileQuery: MediaQueryList;
	sideNavService = inject(SideNavService);

	private loadingService = inject(LoadingService);
	private mobileQueryListener: () => void;

	density: "default" | "comfortable" | "compact" = "default";

	constructor() {
		const changeDetectorRef = inject(ChangeDetectorRef);
		const media = inject(MediaMatcher);

		this.mobileQuery = media.matchMedia("(max-width: 600px)");
		this.mobileQueryListener = () => changeDetectorRef.detectChanges();
		this.mobileQuery.addListener(this.mobileQueryListener);

		// Initialize preferences from localStorage
		try {
			const dens = localStorage.getItem("appDensity");
			if (dens === "comfortable" || dens === "compact") this.density = dens as any;
			// Apply density class
			this.setDensity(this.density);
		} catch {}
	}

	ngOnDestroy(): void {
		this.mobileQuery.removeListener(this.mobileQueryListener);
	}
	setDensity(d: "default" | "comfortable" | "compact") {
		this.density = d;
		try {
			localStorage.setItem("appDensity", d);
		} catch {}
		const html = document.documentElement;
		html.classList.remove("density-default", "density-comfortable", "density-compact");
		html.classList.add(`density-${d}`);
	}
}

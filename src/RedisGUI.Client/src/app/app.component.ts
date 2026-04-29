import { Component, inject, signal, linkedSignal, ChangeDetectionStrategy } from "@angular/core";
import { RouterLink, RouterLinkActive, RouterOutlet } from "@angular/router";
import { NgTemplateOutlet } from "@angular/common";

import { MatToolbarModule } from "@angular/material/toolbar";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatListModule } from "@angular/material/list";
import { MatSidenavModule } from "@angular/material/sidenav";
import { MatMenuModule } from "@angular/material/menu";
import { MatCheckboxModule } from "@angular/material/checkbox";
import { MatDividerModule } from "@angular/material/divider";

import { LoadingComponent } from "./shared/components/loading/loading.component";
import { BreadcrumbComponent } from "./shared/components/breadcrumb/breadcrumb.component";
import { SectionActionsComponent } from "./shared/components/section-actions/section-actions.component";
import { ThemePicker } from "./shared/ui/theme-picker/theme-picker";

import { AuthStore } from "./core/stores/auth.store";
import { SideNavStore } from "./core/stores/side-nav.store";
import { AuthService } from "./core/services/auth/auth.service";
import { useMediaQuery } from "./core/utils/signal-utilities";

type DensityType = "default" | "comfortable" | "compact";

/**
 * Root Application Component
 * Main application shell with navigation, sidebar, and layout management
 *
 * Features:
 * - Responsive layout (mobile/desktop)
 * - Side navigation
 * - User authentication UI
 * - Theme management
 * - Density settings (default/comfortable/compact)
 * - Breadcrumb navigation
 * - Section actions portal
 * - Zoneless change detection compatible
 * - Modern signal hooks for media queries
 */
@Component({
	selector: "app-root",
	imports: [
		RouterOutlet,
		LoadingComponent,
		BreadcrumbComponent,
		MatToolbarModule,
		MatButtonModule,
		MatSidenavModule,
		MatListModule,
		MatIconModule,
		MatMenuModule,
		MatCheckboxModule,
		MatDividerModule,
		RouterLink,
		RouterLinkActive,
		SectionActionsComponent,
		NgTemplateOutlet,
		ThemePicker
	],
	templateUrl: "./app.component.html",
	styleUrl: "./app.component.scss",
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppComponent {
	// 1. Constants
	/** Application title */
	readonly title = "Wunderwaffe";

	// 2. Injected dependencies
	readonly sideNavStore = inject(SideNavStore);
	readonly authStore = inject(AuthStore);
	readonly authService = inject(AuthService);

	// 3. State signals
	/** Whether the app is in mobile view (using custom hook) */
	readonly isMobile = useMediaQuery("(max-width: 600px)");

	/** UI density setting (default/comfortable/compact) with localStorage persistence */
	readonly density = signal<DensityType>(this.getStoredDensity());

	// 4. Linked signal for density with side effects
	/** Synced density value that applies CSS changes */
	readonly appliedDensity = linkedSignal(() => {
		const densityValue = this.density();
		this.applyDensityClass(densityValue);
		this.saveDensity(densityValue);
		return densityValue;
	});

	// 5. Constructor with initialization
	constructor() {
		// Apply initial density on startup
		this.applyDensityClass(this.density());
	}

	/**
	 * Gets stored density from localStorage with fallback
	 */
	private getStoredDensity(): DensityType {
		try {
			const stored = localStorage.getItem("appDensity");
			if (stored === "comfortable" || stored === "compact") {
				return stored;
			}
		} catch {
			// Ignore localStorage errors
		}
		return "default";
	}

	/**
	 * Saves density to localStorage
	 */
	private saveDensity(density: DensityType): void {
		try {
			localStorage.setItem("appDensity", density);
		} catch {
			// Ignore localStorage errors
		}
	}

	/**
	 * Applies density CSS class to document root
	 */
	private applyDensityClass(density: DensityType): void {
		const html = document.documentElement;
		html.classList.remove("density-default", "density-comfortable", "density-compact");
		html.classList.add(`density-${density}`);
	}

	/**
	 * Sets the UI density
	 *
	 * @param d - Density setting: 'default', 'comfortable', or 'compact'
	 */
	setDensity(d: DensityType): void {
		this.density.set(d);
	}

	/**
	 * Logs out the current user
	 * Clears authentication state and redirects to login
	 */
	logout(): void {
		this.authService.logout();
	}
}

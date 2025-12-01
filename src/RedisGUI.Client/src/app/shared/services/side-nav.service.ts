import { Injectable } from "@angular/core";
import { SideNavLinkDefinition } from "../../models/side-nav.type";
import { BehaviorSubject, Observable } from "rxjs";

/**
 * Service for managing side navigation links dynamically
 * Supports global routes and connection-specific routes
 */
@Injectable({
	providedIn: "root"
})
export class SideNavService {

	private readonly defaultRoutes: SideNavLinkDefinition[] = [
		{
			href: "/",
			icon: "dashboard",
			title: "Dashboard",
			description: "Manage your Redis connections"
		},
		{
			href: "/info",
			icon: "info",
			title: "About",
			description: "Application information"
		}
	];

	private sideNavLinks = new BehaviorSubject<SideNavLinkDefinition[]>(this.defaultRoutes);
	public sideNavLinks$: Observable<SideNavLinkDefinition[]> = this.sideNavLinks.asObservable();

	private currentConnectionId: string | null = null;

	constructor() { }

	/**
	 * Add connection-specific routes to navigation
	 * @param connectionId The connection ID
	 * @param connectionName The display name for the connection
	 */
	addConnectionRoutes(connectionId: string, connectionName: string): void {
		this.currentConnectionId = connectionId;
		
		const connectionRoutes: SideNavLinkDefinition[] = [
			{
				href: `/storage/${connectionId}`,
				icon: "storage",
				title: connectionName,
				description: "Storage management",
				badge: "active"
			},
			{
				href: `/pubsub/${connectionId}`,
				icon: "message",
				title: "Pub/Sub",
				description: "Publish and subscribe to channels"
			},
			{
				href: `/transaction/${connectionId}`,
				icon: "swap_horiz",
				title: "Transactions",
				description: "Execute Redis transactions"
			},
			{
				href: `/lua/${connectionId}`,
				icon: "code",
				title: "Lua Scripts",
				description: "Execute Lua scripts"
			}
		];

		// Combine default routes with connection routes
		const allRoutes = [
			...this.defaultRoutes,
			...connectionRoutes
		];

		this.sideNavLinks.next(allRoutes);
	}

	/**
	 * Clear connection-specific routes and restore default navigation
	 */
	clearConnectionRoutes(): void {
		this.currentConnectionId = null;
		this.sideNavLinks.next([...this.defaultRoutes]);
	}

	/**
	 * Get the current connection ID if any
	 */
	getCurrentConnectionId(): string | null {
		return this.currentConnectionId;
	}

	/**
	 * Add a custom route to the navigation
	 */
	addCustomRoute(route: SideNavLinkDefinition): void {
		const current = this.sideNavLinks.value;
		this.sideNavLinks.next([...current, route]);
	}

	/**
	 * Remove a route by href
	 */
	removeRoute(href: string): void {
		const current = this.sideNavLinks.value;
		this.sideNavLinks.next(current.filter(route => route.href !== href));
	}
}

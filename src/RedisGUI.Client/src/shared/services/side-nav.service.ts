import { Injectable } from "@angular/core";
import { SideNavLinkDefinition } from "../types/side-nav.type";
import { BehaviorSubject } from "rxjs";

@Injectable({
	providedIn: "root"
})
export class SideNavService {

	private sideNavLinks: BehaviorSubject<SideNavLinkDefinition[]> = new BehaviorSubject<SideNavLinkDefinition[]>([]);

	sideNavLinks$ = this.sideNavLinks.asObservable();

	constructor() { 

		this.sideNavLinks.next([
			{
				href: "home",
				icon: "home",
				title: "Home"
			},
			{
				href: "info",
				icon: "home",
				title: "Info"
			}
		])
	}
}

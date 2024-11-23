import { Injectable } from "@angular/core";
import { SideNavType } from "../types/side-nav.type";
import { BehaviorSubject } from "rxjs";

@Injectable({
	providedIn: "root"
})
export class SideNavService {

	private sideNavLinks: BehaviorSubject<SideNavType[]> = new BehaviorSubject<SideNavType[]>([]);

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

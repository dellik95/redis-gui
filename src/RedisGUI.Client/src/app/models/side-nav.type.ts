/**
 * Definition for a side navigation link item
 */
export interface SideNavLinkDefinition {
	/**
	 * Route path for the link
	 */
	href: string;

	/**
	 * Material icon name
	 */
	icon: string;

	/**
	 * Display title
	 */
	title: string;

	/**
	 * Optional badge text or count
	 */
	badge?: string | number;

	/**
	 * Optional description text
	 */
	description?: string;
}

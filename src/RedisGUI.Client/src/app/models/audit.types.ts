/**
 * Audit log item returned from the API
 */
export interface AuditLogItem {
	/**
	 * Audit log ID
	 */
	id: string;

	/**
	 * User who performed the action
	 */
	userId: string | null;

	/**
	 * The action performed
	 */
	action: string;

	/**
	 * Type of entity affected
	 */
	entityType: string;

	/**
	 * ID of entity affected
	 */
	entityId: string | null;

	/**
	 * When the action occurred
	 */
	timestamp: string;

	/**
	 * Client IP address
	 */
	ipAddress: string | null;

	/**
	 * Client user agent
	 */
	userAgent: string | null;

	/**
	 * Previous value (for updates)
	 */
	oldValue: string | null;

	/**
	 * New value (for creates/updates)
	 */
	newValue: string | null;
}

/**
 * Response from get audit logs endpoint
 */
export interface GetAuditLogsResponse {
	/**
	 * List of audit log items
	 */
	items: AuditLogItem[];

	/**
	 * Total count of matching records
	 */
	totalCount: number;

	/**
	 * Number of records skipped
	 */
	skip: number;

	/**
	 * Number of records taken
	 */
	take: number;
}

/**
 * Request parameters for querying audit logs
 */
export interface GetAuditLogsRequest {
	/**
	 * Filter by user ID
	 */
	userId?: string;

	/**
	 * Filter by entity type
	 */
	entityType?: string;

	/**
	 * Filter by entity ID
	 */
	entityId?: string;

	/**
	 * Filter by start date
	 */
	fromDate?: string;

	/**
	 * Filter by end date
	 */
	toDate?: string;

	/**
	 * Number of records to skip
	 */
	skip?: number;

	/**
	 * Number of records to take
	 */
	take?: number;
}


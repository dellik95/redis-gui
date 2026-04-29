/**
 * Request payload for creating a new connection
 */
export interface CreateConnectionRequest {
	/**
	 * Display name for the connection
	 */
	name: string;

	/**
	 * Redis server host address
	 */
	host: string;

	/**
	 * Redis server port
	 */
	port: number;

	/**
	 * Authentication username (optional)
	 */
	username?: string | null;

	/**
	 * Authentication password (optional)
	 */
	password?: string | null;

	/**
	 * Redis database index (0-15)
	 */
	database: number;
}

/**
 * Request payload for updating an existing connection
 */
export interface UpdateConnectionRequest {
	/**
	 * Connection ID to update
	 */
	id: string;

	/**
	 * Display name for the connection
	 */
	name: string;

	/**
	 * Redis server host address
	 */
	host: string;

	/**
	 * Redis server port
	 */
	port: number;

	/**
	 * Authentication username (optional)
	 */
	username?: string | null;

	/**
	 * Authentication password (optional)
	 */
	password?: string | null;

	/**
	 * Redis database index (0-15)
	 */
	database: number;
}

/**
 * Request payload for checking connection validity
 */
export interface CheckConnectionRequest {
	/**
	 * Redis server host address
	 */
	host: string;

	/**
	 * Redis server port
	 */
	port: number;

	/**
	 * Authentication username (optional)
	 */
	username?: string | null;

	/**
	 * Authentication password (optional)
	 */
	password?: string | null;

	/**
	 * Redis database index (0-15)
	 */
	database: number;
}

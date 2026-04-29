/**
 * Represents a Redis connection configuration
 * Matches backend GetConnectionResponse DTO
 */
export interface Connection {
	/**
	 * Unique identifier for the connection
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
	username: string | null;

	/**
	 * Authentication password (optional)
	 */
	password: string | null;

	/**
	 * Redis database index (0-15)
	 */
	database: number;
}

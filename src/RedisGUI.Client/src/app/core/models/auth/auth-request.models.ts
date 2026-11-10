/**
 * Request payload for user registration
 */
export interface RegisterRequest {
  /**
   * User's email address (must be unique)
   */
  email: string;

  /**
   * User's password (minimum 6 characters)
   */
  password: string;

  /**
   * User's first name
   */
  firstName: string;

  /**
   * User's last name
   */
  lastName: string;
}

/**
 * Request payload for user login
 */
export interface LoginRequest {
  /**
   * User's email address
   */
  email: string;

  /**
   * User's password
   */
  password: string;
}

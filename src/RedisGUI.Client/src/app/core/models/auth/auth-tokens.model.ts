import { User } from './user.model';

/**
 * Response from the login endpoint
 * Matches backend LoginUserResponse DTO
 */
export interface LoginResponse {
  /**
   * User ID (maps from backend UserId)
   */
  userId: string;

  /**
   * User email (maps from backend Email)
   */
  email: string;

  /**
   * User's first name (maps from backend FirstName)
   */
  firstName: string;

  /**
   * User's last name (maps from backend LastName)
   */
  lastName: string;

  /**
   * User's role (maps from backend Role)
   */
  role: 'Admin' | 'User' | 'ReadOnly';

  /**
   * JWT token for authentication (maps from backend Token)
   */
  token: string;

  /**
   * Token expiration timestamp (maps from backend ExpiresAt)
   */
  expiresAt: string;
}

/**
 * Response from the register endpoint
 */
export interface RegisterResponse {
  /**
   * ID of the newly created user
   */
  userId: string;
}


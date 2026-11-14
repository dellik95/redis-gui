/**
 * Represents an authenticated user in the system
 */
/** Matches RedisGUI.Domain.Users.UserRole and its JSON representation. */
export enum UserRole {
  User = 'User',
  Admin = 'Admin',
  ReadOnly = 'ReadOnly'
}

export interface User {
  /**
   * Unique identifier for the user
   */
  id: string;

  /**
   * User's email address
   */
  email: string;

  /**
   * User's first name
   */
  firstName: string;

  /**
   * User's last name
   */
  lastName: string;

  /**
   * User's role in the system
   * - Admin: Full access to all features
   * - User: Can create, read, update, delete own resources
   * - ReadOnly: Can only view resources
   */
  role: UserRole;
}

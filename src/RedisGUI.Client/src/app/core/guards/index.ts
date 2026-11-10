/**
 * Route Guards
 * Barrel export for all route guards
 * 
 * Guards are used in app.routes.ts to protect routes based on:
 * - Authentication status (authGuard)
 * - User roles (adminGuard, writeAccessGuard)
 * - Guest-only pages (guestOnlyGuard)
 */
export { authGuard, adminGuard, writeAccessGuard, guestOnlyGuard } from './auth.guard';


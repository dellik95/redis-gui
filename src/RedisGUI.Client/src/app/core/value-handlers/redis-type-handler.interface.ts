import { RedisDataType } from '../../models/redis-keys.types';

/**
 * JSON representation of Redis value with type-specific structure
 * Used for serialization/deserialization of Redis values
 */
export interface RedisValueJson {
  [key: string]: unknown;
}

/**
 * Visual representation types for Redis values
 * These are the formats used in the UI editor
 */
export type VisualValue = 
  | string                    // String type
  | string[]                  // List or Set type
  | Array<{ key: string; value: string }>  // Hash type
  | Array<{ member: string; score: number }>  // SortedSet type
  | Array<{ id: string; fields: Array<{ key: string; value: string }> }>;  // Stream type

/**
 * Validation result from type handler
 */
export interface ValidationResult {
  valid: boolean;
  errors: string[];
}

/**
 * Strategy interface for handling different Redis data types
 * 
 * Each Redis data type (String, Hash, List, Set, SortedSet) has its own handler
 * that knows how to transform data between visual editor, JSON, and Redis formats
 * 
 * @example
 * ```typescript
 * const handler = new StringTypeHandler();
 * const json = handler.toJson('hello world');
 * const visual = handler.toVisual('hello world');
 * const valid = handler.validate('hello world');
 * ```
 */
export interface RedisTypeHandler {
  /**
   * The Redis data type this handler manages
   */
  readonly type: RedisDataType;

  /**
   * Convert raw Redis value to visual editor format
   * @param data Raw value from Redis (can be any type from Redis)
   * @returns Value formatted for visual editor
   */
  toVisual(data: unknown): VisualValue;

  /**
   * Convert value to JSON representation
   * @param data Value in visual editor format
   * @returns JSON string representation
   */
  toJson(data: unknown): string;

  /**
   * Parse JSON back to visual editor format
   * @param json JSON string or parsed object
   * @returns Value formatted for visual editor
   */
  fromJson(json: string | RedisValueJson): VisualValue;

  /**
   * Validate value structure for this type
   * @param value Value to validate (can be any type)
   * @returns Validation result with errors if invalid
   */
  validate(value: unknown): ValidationResult;

  /**
   * Get default/empty value for this type
   * @returns Empty value appropriate for this type
   */
  getDefaultValue(): VisualValue;

  /**
   * Format value for display in UI
   * @param value Value to format (can be any type)
   * @returns Human-readable string representation
   */
  formatForDisplay(value: unknown): string;
}

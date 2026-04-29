import { RedisDataType } from '../../models/redis-keys.types';
import { RedisTypeHandler, RedisValueJson, ValidationResult, VisualValue } from './redis-type-handler.interface';

/**
 * Handler for Redis String data type
 * 
 * Strings are the simplest Redis type - just a text value
 * 
 * JSON format: { "value": "string content" }
 * Visual format: string
 */
export class StringTypeHandler implements RedisTypeHandler {
  readonly type = RedisDataType.String;

  /**
   * Convert raw Redis value to visual editor format (string)
   * 
   * @param data Raw value from Redis (can be any type)
   * @returns String representation of the value
   */
  toVisual(data: unknown): string {
    if (data === null || data === undefined) {
      return '';
    }
    return String(data);
  }

  /**
   * Convert value to JSON representation
   * 
   * @param data Value in visual editor format (string)
   * @returns JSON string representation
   */
  toJson(data: unknown): string {
    const value = this.toVisual(data);
    return JSON.stringify({ value }, null, 2);
  }

  /**
   * Parse JSON back to visual editor format
   * 
   * @param json JSON string or parsed object
   * @returns String value
   * @throws Error if JSON is invalid
   */
  fromJson(json: string | RedisValueJson): string {
    try {
      const parsed = typeof json === 'string' ? JSON.parse(json) : json;
      return this.toVisual(parsed.value);
    } catch (error) {
      throw new Error(`Invalid JSON for String type: ${error}`);
    }
  }

  /**
   * Validate value structure for String type
   * 
   * @param value Value to validate
   * @returns Validation result with errors if invalid
   */
  validate(value: unknown): ValidationResult {
    // Strings accept anything convertible to string
    if (value === null || value === undefined) {
      return {
        valid: false,
        errors: ['String value cannot be null or undefined']
      };
    }
    return { valid: true, errors: [] };
  }

  getDefaultValue(): string {
    return '';
  }

  /**
   * Format value for display in UI
   * Truncates long strings and shows character count
   * 
   * @param value Value to format
   * @returns Human-readable string representation
   */
  formatForDisplay(value: unknown): string {
    const str = this.toVisual(value);
    const maxLength = 100;
    if (str.length > maxLength) {
      return `${str.substring(0, maxLength)}... (${str.length} chars)`;
    }
    return str;
  }
}

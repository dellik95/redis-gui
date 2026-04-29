import { RedisDataType } from '../../models/redis-keys.types';
import { RedisTypeHandler, RedisValueJson, ValidationResult, VisualValue } from './redis-type-handler.interface';

/**
 * Handler for Redis List data type
 * 
 * Lists are ordered collections of strings
 * 
 * Visual format: string[]
 * JSON format: { "items": ["item1", "item2", "item3"] }
 */
export class ListTypeHandler implements RedisTypeHandler {
  readonly type = RedisDataType.List;

  /**
   * Convert raw Redis value to visual editor format (string array)
   * 
   * @param data Raw value from Redis (array of values)
   * @returns Array of strings
   */
  toVisual(data: unknown): string[] {
    if (!Array.isArray(data)) {
      return [];
    }
    return data.map(item => String(item));
  }

  /**
   * Convert value to JSON representation
   * 
   * @param data Value in visual editor format (string array)
   * @returns JSON string representation
   */
  toJson(data: unknown): string {
    const items = this.toVisual(data);
    return JSON.stringify({ items }, null, 2);
  }

  /**
   * Parse JSON back to visual editor format
   * 
   * @param json JSON string or parsed object
   * @returns Array of strings
   * @throws Error if JSON is invalid
   */
  fromJson(json: string | RedisValueJson): string[] {
    try {
      const parsed = typeof json === 'string' ? JSON.parse(json) : json;
      
      if (!parsed.items || !Array.isArray(parsed.items)) {
        throw new Error('JSON must contain "items" array');
      }

      return this.toVisual(parsed.items);
    } catch (error) {
      throw new Error(`Invalid JSON for List type: ${error}`);
    }
  }

  /**
   * Validate value structure for List type
   * 
   * @param value Value to validate (should be string array)
   * @returns Validation result with errors if invalid
   */
  validate(value: unknown): ValidationResult {
    if (!Array.isArray(value)) {
      return {
        valid: false,
        errors: ['List value must be an array']
      };
    }

    const errors: string[] = [];
    
    value.forEach((item, index) => {
      if (item === null || item === undefined) {
        errors.push(`Item at index ${index}: cannot be null or undefined`);
      }
    });

    return {
      valid: errors.length === 0,
      errors
    };
  }

  getDefaultValue(): string[] {
    return [];
  }

  /**
   * Format value for display in UI
   * Shows first 3 items and total count
   * 
   * @param value Value to format
   * @returns Human-readable string representation
   */
  formatForDisplay(value: unknown): string {
    const items = this.toVisual(value);
    const count = items.length;
    
    if (count === 0) {
      return 'Empty list';
    }
    
    const preview = items.slice(0, 3).join(', ');
    return count > 3 
      ? `${preview}... (${count} items)`
      : preview;
  }
}

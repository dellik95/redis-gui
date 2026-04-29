import { RedisDataType } from '../../models/redis-keys.types';
import { RedisTypeHandler, RedisValueJson, ValidationResult, VisualValue } from './redis-type-handler.interface';

/**
 * Handler for Redis Set data type
 * 
 * Sets are unordered collections of unique strings
 * 
 * Visual format: string[]
 * JSON format: { "members": ["member1", "member2", "member3"] }
 */
export class SetTypeHandler implements RedisTypeHandler {
  readonly type = RedisDataType.Set;

  /**
   * Convert raw Redis value to visual editor format (string array)
   * Removes duplicates as Sets only contain unique values
   * 
   * @param data Raw value from Redis (array of values)
   * @returns Array of unique strings
   */
  toVisual(data: unknown): string[] {
    if (!Array.isArray(data)) {
      return [];
    }
    // Remove duplicates and convert to strings
    return [...new Set(data.map(item => String(item)))];
  }

  /**
   * Convert value to JSON representation
   * 
   * @param data Value in visual editor format (string array)
   * @returns JSON string representation
   */
  toJson(data: unknown): string {
    const members = this.toVisual(data);
    return JSON.stringify({ members }, null, 2);
  }

  /**
   * Parse JSON back to visual editor format
   * 
   * @param json JSON string or parsed object
   * @returns Array of unique strings
   * @throws Error if JSON is invalid
   */
  fromJson(json: string | RedisValueJson): string[] {
    try {
      const parsed = typeof json === 'string' ? JSON.parse(json) : json;
      
      if (!parsed.members || !Array.isArray(parsed.members)) {
        throw new Error('JSON must contain "members" array');
      }

      return this.toVisual(parsed.members);
    } catch (error) {
      throw new Error(`Invalid JSON for Set type: ${error}`);
    }
  }

  /**
   * Validate value structure for Set type
   * Ensures array format and checks for duplicates
   * 
   * @param value Value to validate (should be string array)
   * @returns Validation result with errors if invalid
   */
  validate(value: unknown): ValidationResult {
    if (!Array.isArray(value)) {
      return {
        valid: false,
        errors: ['Set value must be an array']
      };
    }

    const errors: string[] = [];
    
    value.forEach((item, index) => {
      if (item === null || item === undefined) {
        errors.push(`Member at index ${index}: cannot be null or undefined`);
      }
      if (typeof item === 'string' && item.trim() === '') {
        errors.push(`Member at index ${index}: cannot be empty string`);
      }
    });

    // Check for duplicates
    const stringified = value.map(v => String(v));
    const duplicates = stringified.filter((member, index) => 
      stringified.indexOf(member) !== index
    );
    
    if (duplicates.length > 0) {
      errors.push(`Duplicate members found: ${duplicates.join(', ')}`);
    }

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
   * Shows first 3 members and total count
   * 
   * @param value Value to format
   * @returns Human-readable string representation
   */
  formatForDisplay(value: unknown): string {
    const members = this.toVisual(value);
    const count = members.length;
    
    if (count === 0) {
      return 'Empty set';
    }
    
    const preview = members.slice(0, 3).join(', ');
    return count > 3 
      ? `${preview}... (${count} members)`
      : preview;
  }
}

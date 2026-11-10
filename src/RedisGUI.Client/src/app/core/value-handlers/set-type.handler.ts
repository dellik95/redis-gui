import { Injectable } from '@angular/core';
import { RedisDataType } from '../../models/redis-keys.types';
import { RedisTypeHandler, RedisValueJson, ValidationResult } from './redis-type-handler.interface';

/**
 * Handler for Redis Set data type
 * 
 * Sets are unordered collections of unique strings
 * 
 * Visual format: string[]
 * JSON format: { "members": ["member1", "member2", "member3"] }
 */
@Injectable({
  providedIn: 'root'
})
export class SetTypeHandler implements RedisTypeHandler {
  readonly type = RedisDataType.Set;

  /**
   * Convert raw Redis value to visual editor format (string array)
   * Removes duplicates as Sets only contain unique values
   * 
   * @param data Raw value from Redis (array or { members: [] })
   * @returns Array of unique strings
   */
  toVisual(data: unknown): string[] {
    if (!data) {
      return [];
    }
    if (typeof data === 'object' && 'members' in (data as Record<string, unknown>)) {
      const members = (data as Record<string, unknown>)['members'];
      return Array.isArray(members) ? [...new Set(members.map(m => String(m)))] : [];
    }
    if (!Array.isArray(data)) {
      return [];
    }
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
   * 
   * @param value Value to validate (must be string array)
   * @returns Validation result with errors if invalid
   */
  validate(value: unknown): ValidationResult {
    const errors: string[] = [];

    if (!Array.isArray(value)) {
      return { valid: false, errors: ['Value must be an array of members'] };
    }

    return {
      valid: errors.length === 0,
      errors
    };
  }

  /**
   * Get default empty value for Set type
   * 
   * @returns Default empty string array
   */
  getDefaultValue(): string[] {
    return [];
  }

  formatForDisplay(value: unknown): string {
    const visual = this.toVisual(value);
    return `${visual.length} members`;
  }
}

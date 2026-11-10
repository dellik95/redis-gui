import { Injectable } from '@angular/core';
import { RedisDataType } from '../../models/redis-keys.types';
import { RedisTypeHandler, RedisValueJson, ValidationResult } from './redis-type-handler.interface';

/**
 * Handler for Redis List data type
 * 
 * Lists are ordered collections of strings
 * 
 * Visual format: string[]
 * JSON format: { "items": ["item1", "item2", "item3"] }
 */
@Injectable({
  providedIn: 'root'
})
export class ListTypeHandler implements RedisTypeHandler {
  readonly type = RedisDataType.List;

  /**
   * Convert raw Redis value to visual editor format (string array)
   * 
   * @param data Raw value from Redis (array of values or { items: [] })
   * @returns Array of strings
   */
  toVisual(data: unknown): string[] {
    if (!data) {
      return [];
    }
    if (typeof data === 'object' && 'items' in (data as Record<string, unknown>)) {
      const items = (data as Record<string, unknown>)['items'];
      return Array.isArray(items) ? items.map(item => String(item)) : [];
    }
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
   * @param value Value to validate (must be string array)
   * @returns Validation result with errors if invalid
   */
  validate(value: unknown): ValidationResult {
    const errors: string[] = [];

    if (!Array.isArray(value)) {
      return { valid: false, errors: ['Value must be an array of items'] };
    }

    return {
      valid: errors.length === 0,
      errors
    };
  }

  /**
   * Get default empty value for List type
   * 
   * @returns Default empty string array
   */
  getDefaultValue(): string[] {
    return [];
  }

  formatForDisplay(value: unknown): string {
    const visual = this.toVisual(value);
    return `${visual.length} items`;
  }
}

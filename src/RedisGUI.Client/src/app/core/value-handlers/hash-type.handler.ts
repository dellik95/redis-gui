import { RedisDataType } from '../../models/redis-keys.types';
import { RedisTypeHandler, RedisValueJson, ValidationResult, VisualValue } from './redis-type-handler.interface';

/**
 * Field-value pair for Hash visual representation
 */
export interface HashField {
  key: string;
  value: string;
}

/**
 * Handler for Redis Hash data type
 * 
 * Hashes are maps of field-value pairs
 * 
 * Visual format: Array<{ key: string, value: string }>
 * JSON format: { "fields": { "field1": "value1", "field2": "value2" } }
 */
export class HashTypeHandler implements RedisTypeHandler {
  readonly type = RedisDataType.Hash;

  /**
   * Convert raw Redis value to visual editor format (HashField array)
   * 
   * @param data Raw value from Redis (object with key-value pairs)
   * @returns Array of HashField objects
   */
  toVisual(data: unknown): HashField[] {
    if (!data || typeof data !== 'object') {
      return [];
    }

    return Object.entries(data).map(([key, value]) => ({
      key,
      value: String(value)
    }));
  }

  /**
   * Convert value to JSON representation
   * 
   * @param data Value in visual editor format (HashField array) or object format
   * @returns JSON string representation
   */
  toJson(data: unknown): string {
    let fields: { [key: string]: string } = {};

    if (Array.isArray(data)) {
      // From visual editor format
      data.forEach(field => {
        fields[field.key] = field.value;
      });
    } else if (data && typeof data === 'object') {
      // Already in object format
      fields = data as { [key: string]: string };
    }

    return JSON.stringify({ fields }, null, 2);
  }

  /**
   * Parse JSON back to visual editor format
   * 
   * @param json JSON string or parsed object
   * @returns Array of HashField objects
   * @throws Error if JSON is invalid
   */
  fromJson(json: string | RedisValueJson): HashField[] {
    try {
      const parsed = typeof json === 'string' ? JSON.parse(json) : json;
      
      if (!parsed.fields || typeof parsed.fields !== 'object') {
        throw new Error('JSON must contain "fields" object');
      }

      return this.toVisual(parsed.fields);
    } catch (error) {
      throw new Error(`Invalid JSON for Hash type: ${error}`);
    }
  }

  /**
   * Validate value structure for Hash type
   * 
   * @param value Value to validate (should be HashField array)
   * @returns Validation result with errors if invalid
   */
  validate(value: unknown): ValidationResult {
    const errors: string[] = [];

    if (!Array.isArray(value)) {
      errors.push('Hash value must be an array of field-value pairs');
      return { valid: false, errors };
    }

    value.forEach((field, index) => {
      if (!field.key || typeof field.key !== 'string') {
        errors.push(`Field at index ${index}: key is required and must be a string`);
      }
      if (field.key.trim() === '') {
        errors.push(`Field at index ${index}: key cannot be empty`);
      }
    });

    // Check for duplicate keys
    const keys = value.map(f => f.key);
    const duplicates = keys.filter((key, index) => keys.indexOf(key) !== index);
    if (duplicates.length > 0) {
      errors.push(`Duplicate keys found: ${duplicates.join(', ')}`);
    }

    return {
      valid: errors.length === 0,
      errors
    };
  }

  getDefaultValue(): HashField[] {
    return [];
  }

  /**
   * Format value for display in UI
   * Shows first 3 fields and total count
   * 
   * @param value Value to format
   * @returns Human-readable string representation
   */
  formatForDisplay(value: unknown): string {
    const fields = Array.isArray(value) ? value : this.toVisual(value);
    const count = fields.length;
    
    if (count === 0) {
      return 'Empty hash';
    }
    
    const preview = fields.slice(0, 3)
      .map(f => `${f.key}: ${f.value}`)
      .join(', ');
    
    return count > 3 
      ? `${preview}... (${count} fields)`
      : preview;
  }
}

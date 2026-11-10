import { Injectable } from '@angular/core';
import { RedisDataType } from '../../models/redis-keys.types';
import { RedisTypeHandler, RedisValueJson, ValidationResult } from './redis-type-handler.interface';

/**
 * Handler for Redis String data type
 * Supports raw string and JSON document values
 */
@Injectable({
  providedIn: 'root'
})
export class StringTypeHandler implements RedisTypeHandler {
  readonly type = RedisDataType.String;

  /**
   * Convert raw Redis value to visual editor format (string)
   */
  toVisual(data: unknown): string {
    if (data === null || data === undefined) {
      return '';
    }
    if (typeof data === 'object' && 'value' in (data as Record<string, unknown>)) {
      const val = (data as Record<string, unknown>)['value'];
      return typeof val === 'object' && val !== null ? JSON.stringify(val, null, 2) : String(val ?? '');
    }
    if (typeof data === 'object') {
      return JSON.stringify(data, null, 2);
    }
    return String(data);
  }

  /**
   * Convert value to JSON representation
   */
  toJson(data: unknown): string {
    const raw = this.toVisual(data);
    const trimmed = raw.trim();
    if ((trimmed.startsWith('{') && trimmed.endsWith('}')) || (trimmed.startsWith('[') && trimmed.endsWith(']'))) {
      try {
        const parsed = JSON.parse(trimmed);
        return JSON.stringify(parsed, null, 2);
      } catch {
        // Not valid JSON, wrap in value object
      }
    }
    return JSON.stringify({ value: raw }, null, 2);
  }

  /**
   * Parse JSON back to visual editor format
   */
  fromJson(json: string | RedisValueJson): string {
    try {
      const parsed = typeof json === 'string' ? JSON.parse(json) : json;
      if (typeof parsed === 'object' && parsed !== null && 'value' in parsed && Object.keys(parsed).length === 1) {
        const val = (parsed as Record<string, unknown>)['value'];
        return typeof val === 'object' && val !== null ? JSON.stringify(val, null, 2) : String(val ?? '');
      }
      if (typeof parsed === 'object' && parsed !== null) {
        return JSON.stringify(parsed, null, 2);
      }
      return String(parsed ?? '');
    } catch (error) {
      throw new Error(`Invalid JSON for String type: ${error instanceof Error ? error.message : String(error)}`);
    }
  }

  /**
   * Validate string value
   */
  validate(value: unknown): ValidationResult {
    return {
      valid: typeof value === 'string',
      errors: typeof value === 'string' ? [] : ['Value must be a string']
    };
  }

  getDefaultValue(): string {
    return '';
  }
}

import { Injectable } from '@angular/core';
import { RedisDataType } from '../../models/redis-keys.types';
import { RedisTypeHandler, RedisValueJson, ValidationResult } from './redis-type-handler.interface';

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
 * Supports both plain string values and nested JSON objects in fields!
 */
@Injectable({
  providedIn: 'root'
})
export class HashTypeHandler implements RedisTypeHandler {
  readonly type = RedisDataType.Hash;

  /**
   * Convert raw Redis value to visual editor format (HashField array)
   */
  toVisual(data: unknown): HashField[] {
    if (!data || typeof data !== 'object') {
      return [];
    }

    const fields = 'fields' in (data as Record<string, unknown>)
      ? (data as Record<string, unknown>)['fields']
      : data;

    if (!fields || typeof fields !== 'object') {
      return [];
    }

    return Object.entries(fields as Record<string, unknown>).map(([key, value]) => ({
      key,
      value: typeof value === 'object' && value !== null
        ? JSON.stringify(value, null, 2)
        : String(value ?? '')
    }));
  }

  /**
   * Convert value to JSON representation
   */
  toJson(data: unknown): string {
    const fields: Record<string, unknown> = {};

    if (Array.isArray(data)) {
      data.forEach((field: HashField) => {
        if (!field.key) return;
        const trimmed = (field.value || '').trim();
        if ((trimmed.startsWith('{') && trimmed.endsWith('}')) || (trimmed.startsWith('[') && trimmed.endsWith(']'))) {
          try {
            fields[field.key] = JSON.parse(trimmed);
            return;
          } catch {
            // Not valid JSON, keep as string
          }
        }
        fields[field.key] = field.value;
      });
    } else if (data && typeof data === 'object') {
      Object.assign(fields, data);
    }

    return JSON.stringify({ fields }, null, 2);
  }

  /**
   * Parse JSON back to visual editor format
   */
  fromJson(json: string | RedisValueJson): HashField[] {
    try {
      const parsed = typeof json === 'string' ? JSON.parse(json) : json;
      if (!parsed || typeof parsed !== 'object') {
        throw new Error('JSON must be an object');
      }

      const fields = 'fields' in parsed && parsed.fields && typeof parsed.fields === 'object'
        ? parsed.fields
        : parsed;

      return Object.entries(fields as Record<string, unknown>).map(([key, value]) => ({
        key,
        value: typeof value === 'object' && value !== null
          ? JSON.stringify(value, null, 2)
          : String(value ?? '')
      }));
    } catch (error) {
      throw new Error(`Invalid JSON for Hash type: ${error instanceof Error ? error.message : String(error)}`);
    }
  }

  /**
   * Validate value structure for Hash type
   */
  validate(value: unknown): ValidationResult {
    if (!Array.isArray(value)) {
      return { valid: false, errors: ['Value must be an array of fields'] };
    }

    const errors: string[] = [];
    const keys = new Set<string>();

    value.forEach((field: HashField, index: number) => {
      if (!field.key || field.key.trim() === '') {
        errors.push(`Field #${index + 1}: Key is required`);
      } else if (keys.has(field.key)) {
        errors.push(`Field "${field.key}": Duplicate key`);
      } else {
        keys.add(field.key);
      }
    });

    return {
      valid: errors.length === 0,
      errors
    };
  }

  getDefaultValue(): HashField[] {
    return [];
  }
}

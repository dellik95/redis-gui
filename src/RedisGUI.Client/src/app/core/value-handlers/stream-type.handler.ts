import { Injectable } from '@angular/core';
import { RedisDataType } from '../../models/redis-keys.types';
import { RedisTypeHandler, RedisValueJson, ValidationResult } from './redis-type-handler.interface';

/**
 * Field key-value pair for Stream entry
 */
export interface StreamField {
  key: string;
  value: string;
}

/**
 * Entry structure for Stream visual representation
 */
export interface StreamVisualEntry {
  id: string;
  fields: StreamField[];
}

/**
 * Handler for Redis Stream data type
 * 
 * Streams are append-only log data structures consisting of entries with unique IDs
 * and associated key-value field pairs.
 * 
 * Visual format: Array<{ id: string, fields: Array<{ key: string, value: string }> }>
 * JSON format: { "entries": [{ "id": "1618000000000-0", "values": { "field1": "val1" } }] }
 *               OR direct array: [{ "id": "1618000000000-0", "values": { "field1": "val1" } }]
 */
@Injectable({
  providedIn: 'root'
})
export class StreamTypeHandler implements RedisTypeHandler {
  readonly type = RedisDataType.Stream;

  /**
   * Convert raw Redis value to visual editor format
   */
  toVisual(data: unknown): StreamVisualEntry[] {
    if (!data) {
      return [];
    }

    let parsedData: unknown = data;
    if (typeof data === 'string') {
      const trimmed = data.trim();
      if (trimmed.startsWith('[') || trimmed.startsWith('{')) {
        try {
          parsedData = JSON.parse(trimmed);
        } catch {
          return [];
        }
      } else if (trimmed.includes(':[')) {
        // Parse semicolon-separated format: "id:[k1=v1,k2=v2];id2:[k=v]"
        const items = trimmed.split(';').filter(s => s.trim().length > 0);
        return items.map(item => {
          const colonIdx = item.indexOf(':[');
          if (colonIdx === -1) return { id: item, fields: [] };
          const id = item.substring(0, colonIdx).trim();
          const fieldsStr = item.substring(colonIdx + 2).replace(/\]$/, '');
          const fieldPairs = fieldsStr.split(',').filter(p => p.includes('='));
          const fields = fieldPairs.map(p => {
            const eqIdx = p.indexOf('=');
            return {
              key: p.substring(0, eqIdx).trim(),
              value: p.substring(eqIdx + 1).trim()
            };
          });
          return { id, fields };
        });
      }
    }

    let entries: unknown[] = [];
    if (Array.isArray(parsedData)) {
      entries = parsedData;
    } else if (typeof parsedData === 'object' && parsedData !== null) {
      const rec = parsedData as Record<string, unknown>;
      const rawEntries = rec['entries'] ?? rec['Entries'] ?? rec['items'] ?? rec['values'] ?? rec['Values'];
      if (Array.isArray(rawEntries)) {
        entries = rawEntries;
      }
    }

    return entries.map((entry: any) => {
      if (!entry || typeof entry !== 'object') {
        return { id: '*', fields: [] };
      }

      const id = String(entry.id ?? entry.Id ?? '*');
      let fields: StreamField[] = [];

      // 1. Direct fields array: [{ key: 'k', value: 'v' }]
      if (Array.isArray(entry.fields)) {
        fields = entry.fields.map((f: any) => ({
          key: String(f?.key ?? f?.name ?? ''),
          value: String(f?.value ?? '')
        }));
      } else if (Array.isArray(entry.Fields)) {
        fields = entry.Fields.map((f: any) => ({
          key: String(f?.key ?? f?.name ?? ''),
          value: String(f?.value ?? '')
        }));
      }
      // 2. Fields object: { "field1": "val1" }
      else if (entry.fields && typeof entry.fields === 'object') {
        fields = Object.entries(entry.fields).map(([k, v]) => ({
          key: String(k),
          value: String(v ?? '')
        }));
      } else if (entry.Fields && typeof entry.Fields === 'object') {
        fields = Object.entries(entry.Fields).map(([k, v]) => ({
          key: String(k),
          value: String(v ?? '')
        }));
      }
      // 3. Values dictionary: { "field1": "val1" }
      else if (entry.values && typeof entry.values === 'object') {
        fields = Object.entries(entry.values).map(([k, v]) => ({
          key: String(k),
          value: String(v ?? '')
        }));
      } else if (entry.Values && typeof entry.Values === 'object') {
        fields = Object.entries(entry.Values).map(([k, v]) => ({
          key: String(k),
          value: String(v ?? '')
        }));
      }

      return { id, fields };
    });
  }

  /**
   * Convert value to JSON representation
   */
  toJson(data: unknown): string {
    const visual = this.toVisual(data);
    const entries = visual.map(e => {
      const values: Record<string, string> = {};
      if (Array.isArray(e.fields)) {
        e.fields.forEach(f => {
          if (f.key !== undefined && f.key !== null && f.key !== '') {
            values[f.key] = f.value ?? '';
          }
        });
      }
      return {
        id: e.id || '*',
        values
      };
    });

    return JSON.stringify({ entries }, null, 2);
  }

  /**
   * Parse JSON back to visual editor format
   */
  fromJson(json: string | RedisValueJson): StreamVisualEntry[] {
    try {
      if (!json) return [];
      const parsed = typeof json === 'string' ? JSON.parse(json) : json;
      if (!parsed || typeof parsed !== 'object') {
        throw new Error('JSON must be an object or array of entries');
      }

      if (Array.isArray(parsed)) {
        return this.toVisual(parsed);
      }

      if (typeof parsed === 'object') {
        const rec = parsed as Record<string, unknown>;
        const rawEntries = rec['entries'] ?? rec['Entries'] ?? rec['items'] ?? rec['values'] ?? rec['Values'];
        if (Array.isArray(rawEntries)) {
          return this.toVisual(rawEntries);
        }
      }

      throw new Error('JSON must contain an "entries" array or be an array of stream entries');
    } catch (error) {
      throw new Error(`Invalid JSON for Stream type: ${error instanceof Error ? error.message : error}`);
    }
  }

  /**
   * Validate stream entries
   */
  validate(value: unknown): ValidationResult {
    const errors: string[] = [];

    if (!Array.isArray(value)) {
      return { valid: false, errors: ['Stream value must be an array of entries'] };
    }

    value.forEach((entry: any, index: number) => {
      if (!entry || typeof entry !== 'object') {
        errors.push(`Entry #${index + 1} must be an object`);
        return;
      }

      const hasFieldsArray = Array.isArray(entry.fields);
      const hasValuesObj = entry.values && typeof entry.values === 'object';
      const hasUpperValuesObj = entry.Values && typeof entry.Values === 'object';

      if (!hasFieldsArray && !hasValuesObj && !hasUpperValuesObj) {
        errors.push(`Entry #${index + 1} must have fields`);
      }
    });

    return {
      valid: errors.length === 0,
      errors
    };
  }

  /**
   * Get default empty value
   */
  getDefaultValue(): StreamVisualEntry[] {
    return [];
  }

  /**
   * Format for display
   */
  formatForDisplay(value: unknown): string {
    const visual = this.toVisual(value);
    return `${visual.length} stream entries`;
  }
}

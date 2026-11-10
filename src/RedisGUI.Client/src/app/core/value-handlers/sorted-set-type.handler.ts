import { Injectable } from '@angular/core';
import { RedisDataType } from '../../models/redis-keys.types';
import { RedisTypeHandler, RedisValueJson, ValidationResult } from './redis-type-handler.interface';

/**
 * Member-score pair for Sorted Set visual representation
 */
export interface SortedSetMember {
  member: string;
  score: number;
}

/**
 * Handler for Redis Sorted Set data type
 * 
 * Sorted Sets are collections of unique members with associated scores
 * Members are ordered by their score
 * 
 * Visual format: Array<{ member: string, score: number }>
 * JSON format: { "membersWithScores": { "member1": 1.0, "member2": 2.0 } }
 */
@Injectable({
  providedIn: 'root'
})
export class SortedSetTypeHandler implements RedisTypeHandler {
  readonly type = RedisDataType.SortedSet;

  /**
   * Convert raw Redis value to visual editor format (SortedSetMember array)
   * 
   * @param data Raw value from Redis (array or object format or { membersWithScores: {} })
   * @returns Array of SortedSetMember objects
   */
  toVisual(data: unknown): SortedSetMember[] {
    if (!data) {
      return [];
    }

    const source = typeof data === 'object' && 'membersWithScores' in (data as Record<string, unknown>)
      ? (data as Record<string, unknown>)['membersWithScores']
      : data;

    if (Array.isArray(source)) {
      return source.map((item: any) => ({
        member: String(item.member || item.value || item),
        score: Number(item.score || 0)
      }));
    } else if (source && typeof source === 'object') {
      return Object.entries(source as Record<string, unknown>).map(([member, score]) => ({
        member,
        score: Number(score)
      }));
    }
    return [];
  }

  /**
   * Convert value to JSON representation
   * 
   * @param data Value in visual editor format (SortedSetMember array) or object format
   * @returns JSON string representation
   */
  toJson(data: unknown): string {
    const membersWithScores: { [key: string]: number } = {};

    if (Array.isArray(data)) {
      data.forEach((item: SortedSetMember) => {
        membersWithScores[item.member] = item.score;
      });
    } else if (data && typeof data === 'object') {
      Object.assign(membersWithScores, data);
    }

    return JSON.stringify({ membersWithScores }, null, 2);
  }

  /**
   * Parse JSON back to visual editor format
   * 
   * @param json JSON string or parsed object
   * @returns Array of SortedSetMember objects
   * @throws Error if JSON is invalid
   */
  fromJson(json: string | RedisValueJson): SortedSetMember[] {
    try {
      const parsed = typeof json === 'string' ? JSON.parse(json) : json;
      
      if (!parsed.membersWithScores || typeof parsed.membersWithScores !== 'object') {
        throw new Error('JSON must contain "membersWithScores" object');
      }

      return this.toVisual(parsed.membersWithScores);
    } catch (error) {
      throw new Error(`Invalid JSON for Sorted Set type: ${error}`);
    }
  }

  /**
   * Validate value structure for Sorted Set type
   * 
   * @param value Value to validate (must be SortedSetMember array)
   * @returns Validation result with errors if invalid
   */
  validate(value: unknown): ValidationResult {
    const errors: string[] = [];

    if (!Array.isArray(value)) {
      return { valid: false, errors: ['Value must be an array of members with scores'] };
    }

    const members = value as SortedSetMember[];
    const seenMembers = new Set<string>();

    members.forEach((item, index) => {
      if (!item.member || !item.member.trim()) {
        errors.push(`Item ${index + 1}: Member is required`);
      } else if (seenMembers.has(item.member)) {
        errors.push(`Duplicate member: "${item.member}"`);
      } else {
        seenMembers.add(item.member);
      }

      if (typeof item.score !== 'number' || isNaN(item.score)) {
        errors.push(`Item ${index + 1}: Score must be a valid number`);
      }
    });

    return {
      valid: errors.length === 0,
      errors
    };
  }

  /**
   * Get default empty value for Sorted Set type
   * 
   * @returns Default empty SortedSetMember array
   */
  getDefaultValue(): SortedSetMember[] {
    return [];
  }

  formatForDisplay(value: unknown): string {
    const visual = this.toVisual(value);
    return `${visual.length} members`;
  }
}

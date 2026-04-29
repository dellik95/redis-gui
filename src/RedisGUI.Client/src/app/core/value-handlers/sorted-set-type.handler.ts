import { RedisDataType } from '../../models/redis-keys.types';
import { RedisTypeHandler, RedisValueJson, ValidationResult, VisualValue } from './redis-type-handler.interface';

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
export class SortedSetTypeHandler implements RedisTypeHandler {
  readonly type = RedisDataType.SortedSet;

  /**
   * Convert raw Redis value to visual editor format (SortedSetMember array)
   * 
   * @param data Raw value from Redis (array or object format)
   * @returns Array of SortedSetMember objects
   */
  toVisual(data: unknown): SortedSetMember[] {
    if (Array.isArray(data)) {
      // Already in array format
      return data.map(item => ({
        member: String(item.member || item.value || item),
        score: Number(item.score || 0)
      }));
    } else if (data && typeof data === 'object') {
      // Object format: { member: score }
      return Object.entries(data).map(([member, score]) => ({
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
    let membersWithScores: { [member: string]: number } = {};

    if (Array.isArray(data)) {
      // From visual editor format
      data.forEach(item => {
        membersWithScores[item.member] = item.score;
      });
    } else if (data && typeof data === 'object') {
      // Already in object format
      membersWithScores = data as { [member: string]: number };
    }

    return JSON.stringify({ membersWithScores }, null, 2);
  }

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
   * @param value Value to validate (should be SortedSetMember array)
   * @returns Validation result with errors if invalid
   */
  validate(value: unknown): ValidationResult {
    const errors: string[] = [];

    if (!Array.isArray(value)) {
      errors.push('Sorted Set value must be an array of member-score pairs');
      return { valid: false, errors };
    }

    value.forEach((item, index) => {
      if (!item.member || typeof item.member !== 'string') {
        errors.push(`Item at index ${index}: member is required and must be a string`);
      }
      if (item.member && item.member.trim() === '') {
        errors.push(`Item at index ${index}: member cannot be empty`);
      }
      if (item.score === null || item.score === undefined) {
        errors.push(`Item at index ${index}: score is required`);
      }
      if (typeof item.score !== 'number' || isNaN(item.score)) {
        errors.push(`Item at index ${index}: score must be a valid number`);
      }
    });

    // Check for duplicate members
    const members = value.map(item => item.member);
    const duplicates = members.filter((member, index) => 
      members.indexOf(member) !== index
    );
    
    if (duplicates.length > 0) {
      errors.push(`Duplicate members found: ${duplicates.join(', ')}`);
    }

    return {
      valid: errors.length === 0,
      errors
    };
  }

  getDefaultValue(): SortedSetMember[] {
    return [];
  }

  /**
   * Format value for display in UI
   * Shows first 3 members (sorted by score) and total count
   * 
   * @param value Value to format
   * @returns Human-readable string representation
   */
  formatForDisplay(value: unknown): string {
    const members = Array.isArray(value) ? value : this.toVisual(value);
    const count = members.length;
    
    if (count === 0) {
      return 'Empty sorted set';
    }
    
    // Sort by score for display
    const sorted = [...members].sort((a, b) => a.score - b.score);
    const preview = sorted.slice(0, 3)
      .map(m => `${m.member} (${m.score})`)
      .join(', ');
    
    return count > 3 
      ? `${preview}... (${count} members)`
      : preview;
  }
}

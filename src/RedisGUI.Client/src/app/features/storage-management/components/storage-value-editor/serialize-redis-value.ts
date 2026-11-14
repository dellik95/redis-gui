import { RedisDataType, type RedisValue } from '../../../../models/redis-keys.types';
import type {
  HashField,
  SortedSetMember,
  StreamVisualEntry
} from '../../../../core/value-handlers';

export function serializeRedisValue(type: RedisDataType, value: unknown): RedisValue {
  const items = Array.isArray(value) ? value : [];
  switch (type) {
    case RedisDataType.String:
      return { value: String(value ?? '') };
    case RedisDataType.List:
      return { items: items as string[] };
    case RedisDataType.Set:
      return { members: items as string[] };
    case RedisDataType.Hash:
      return {
        fields: Object.fromEntries(
          (items as HashField[]).filter(f => f.key).map(f => [f.key, f.value])
        )
      };
    case RedisDataType.SortedSet:
      return {
        membersWithScores: Object.fromEntries(
          (items as SortedSetMember[]).filter(m => m.member).map(m => [m.member, m.score])
        )
      };
    case RedisDataType.Stream:
      return {
        entries: (items as StreamVisualEntry[]).map(entry => {
          const fields = Object.fromEntries(
            (entry.fields ?? []).filter(f => f.key?.trim()).map(f => [f.key.trim(), f.value])
          );
          return { id: entry.id || '*', fields, values: fields };
        })
      };
  }
}

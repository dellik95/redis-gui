// Redis data type models
export enum RedisDataType {
  String = 'String',
  List = 'List',
  Set = 'Set',
  Hash = 'Hash',
  SortedSet = 'SortedSet',
  Stream = 'Stream'
}

export function toRedisDataType(type: string): RedisDataType {
  switch (type.toLowerCase()) {
    case 'list':
      return RedisDataType.List;
    case 'set':
      return RedisDataType.Set;
    case 'hash':
      return RedisDataType.Hash;
    case 'sortedset':
    case 'zset':
      return RedisDataType.SortedSet;
    case 'stream':
      return RedisDataType.Stream;
    default:
      return RedisDataType.String;
  }
}

export interface SelectedRedisKey {
  key: string;
  type: RedisDataType;
  ttl: number;
}

/** Redis `TYPE` command values returned by the API. */
export type RedisWireDataType =
  'string' | 'list' | 'set' | 'hash' | 'sortedset' | 'zset' | 'stream';

/** Well-known Redis TTL responses, expressed in seconds. */
export const REDIS_TTL = {
  Persistent: -1,
  Missing: -2
} as const;

// Base class for Redis value data
export interface RedisValueData {}

/** API response for GET /storage/{connectionId}/{key}. */
export interface GetRedisValueResponse {
  key: string;
  value: unknown;
  type: RedisWireDataType;
  timeToLive?: string;
  ttl?: number;
}

export interface RedisKeyMutationResponse {
  success: boolean;
}

export interface SeedRedisDataResponse {
  totalKeysCreated: number;
  targetDatabases: number[];
  keyCountPerType: Record<string, number>;
  summary: string;
}

export interface StringValueData extends RedisValueData {
  value: string;
}

export interface ListValueData extends RedisValueData {
  items: string[];
}

export interface SetValueData extends RedisValueData {
  members: string[];
}

export interface HashValueData extends RedisValueData {
  fields: { [key: string]: string };
}

export interface SortedSetValueData extends RedisValueData {
  membersWithScores: { [member: string]: number };
}

export interface StreamEntry {
  values?: Record<string, string>;
  id: string;
  fields: { [key: string]: string };
}

export interface StreamValueData extends RedisValueData {
  entries: StreamEntry[];
}

// Union type for all Redis value data types
export type RedisValue =
  | StringValueData
  | ListValueData
  | SetValueData
  | HashValueData
  | SortedSetValueData
  | StreamValueData;

// Grouped keys response models
export interface RedisKeyInfo {
  key: string;
  /**
   * Display type. The API response is constrained to RedisWireDataType, while
   * this UI model also accepts extension/module types reported by Redis.
   */
  type: RedisWireDataType | string;
  size: number;
  ttl: number | null;
  memoryUsage: number | null;
}

export interface RedisKeyGroup {
  name: string;
  path: string;
  keyCount: number;
  children: RedisKeyGroup[];
  keys: RedisKeyInfo[];
}

export interface GetGroupedRedisKeysResponse {
  groups: RedisKeyGroup[];
  totalKeys: number;
  viewMode: 'tree' | 'flat';
}

// Request models
export interface UpdateRedisValueRequest {
  connectionId: string;
  key: string;
  type: RedisDataType;
  value: RedisValueData;
  ttl?: number;
}

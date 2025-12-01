// Redis data type models
export enum RedisDataType {
    String = 'String',
    List = 'List',
    Set = 'Set',
    Hash = 'Hash',
    SortedSet = 'SortedSet',
    Stream = 'Stream'
}

// Base class for Redis value data
export interface RedisValueData {}

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
    id: string;
    fields: { [key: string]: string };
}

export interface StreamValueData extends RedisValueData {
    entries: StreamEntry[];
}

// Grouped keys response models
export interface RedisKeyInfo {
    key: string;
    type: string;
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

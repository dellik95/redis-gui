import { DataSource } from "@angular/cdk/collections";
import { Observable } from "rxjs";
import { inject, signal, effect } from "@angular/core";
import { RedisKeysService } from "../../../services/api/redis-keys.service";
import { RedisKeyInfo, RedisKeyGroup } from "../../../models/redis-keys.types";

export enum StorageValueType {
    HASH = 'Hash',
    LIST = 'List',
    SET = 'Set',
    SORTED_SET = 'SortedSet',
    STRING = 'String',
    STREAM = 'Stream'
}

export type StorageValue = {
    type: StorageValueType;
    key: string;
    ttl: number | null; 
    size: number;
    memoryUsage?: number | null;
}

export class StorageValuesDataSource extends DataSource<StorageValue> {
    private redisKeysService = inject(RedisKeysService);
    public data$ = signal<StorageValue[]>([]);
    public loading$ = signal<boolean>(false);

    connect(): Observable<StorageValue[]> {
        // If using signals, convert to observable for compatibility
        return new Observable<StorageValue[]>( (subscriber: any) => {
            subscriber.next(this.data$());
            // No teardown logic needed for signals
        });
    }

    disconnect(): void {
        this.data$.set([]);
        this.loading$.set(false);
    }

    loadKeys(
        connectionId: string,
        pattern: string = '*',
        delimiter: string = ':',
        viewMode: 'tree' | 'flat' = 'tree'
    ): void {
        this.loading$.set(true);
        this.redisKeysService.getGroupedKeys(connectionId, pattern, delimiter, viewMode)
            .subscribe({
                next: (response) => {
                    const keys = this.flattenGroups(response.groups);
                    const storageValues = keys.map(k => this.mapToStorageValue(k));
                    this.data$.set(storageValues);
                    this.loading$.set(false);
                },
                error: (err) => {
                    console.error('Error loading keys:', err);
                    this.data$.set([]);
                    this.loading$.set(false);
                }
            });
    }

    private flattenGroups(groups: RedisKeyGroup[]): RedisKeyInfo[] {
        let result: RedisKeyInfo[] = [];
        
        for (const group of groups) {
            // Add keys from this group
            result = result.concat(group.keys);
            
            // Recursively add keys from child groups
            if (group.children && group.children.length > 0) {
                result = result.concat(this.flattenGroups(group.children));
            }
        }
        
        return result;
    }

    private mapToStorageValue(keyInfo: RedisKeyInfo): StorageValue {
        return {
            key: keyInfo.key,
            type: this.mapRedisType(keyInfo.type),
            ttl: keyInfo.ttl,
            size: keyInfo.size,
            memoryUsage: keyInfo.memoryUsage
        };
    }

    private mapRedisType(type: string): StorageValueType {
        switch (type.toLowerCase()) {
            case 'string': return StorageValueType.STRING;
            case 'list': return StorageValueType.LIST;
            case 'set': return StorageValueType.SET;
            case 'hash': return StorageValueType.HASH;
            case 'sortedset': return StorageValueType.SORTED_SET;
            case 'stream': return StorageValueType.STREAM;
            default: return StorageValueType.STRING;
        }
    }
}

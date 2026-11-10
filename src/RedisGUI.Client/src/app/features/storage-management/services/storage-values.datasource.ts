import { DataSource } from "@angular/cdk/collections";
import { Observable } from "rxjs";
import { inject, signal, effect } from "@angular/core";
import { toObservable } from "@angular/core/rxjs-interop";
import { RedisKeysStore } from "../../../core/stores/redis-keys.store";
import { RedisKeyInfo } from "../../../models/redis-keys.types";

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
    private readonly redisKeysStore = inject(RedisKeysStore);
    public readonly data$ = signal<StorageValue[]>([]);
    public readonly loading$ = signal<boolean>(false);
    private readonly previousKeysCount = signal<number>(0);
    private readonly dataObservable$: Observable<StorageValue[]>;

    constructor() {
        super();
        this.dataObservable$ = toObservable(this.data$);

        // Watch store state changes - use raw keys directly
        effect(() => {
            const keys = this.redisKeysStore.keys();
            const isLoading = this.redisKeysStore.isLoading();
            const currentKeysCount = keys.length;
            const prevCount = this.previousKeysCount();
            
            // Only update data if we have keys OR if we're not loading (to clear on actual empty state)
            // This prevents clearing data during reload operations
            if (keys.length > 0) {
                const storageValues = keys.map(k => this.mapToStorageValue(k));
                this.data$.set(storageValues);
            } else if (!isLoading) {
                // Only clear data if we're not loading (actual empty state, not during reload)
                this.data$.set([]);
            }
            // Otherwise, preserve existing data during loading
            
            // Only show loading spinner for keys loading, not value loading
            // If we have keys and isLoading is true, it's likely a value load operation
            // Only show loading if:
            // 1. We're loading AND we don't have keys yet (initial load)
            // 2. OR we're loading AND keys count is different from previous (reloading keys)
            const isKeysLoading = isLoading && (
                currentKeysCount === 0 || // Initial load - no keys yet
                (prevCount > 0 && currentKeysCount !== prevCount) // Keys count changed (reload in progress)
            );
            
            this.loading$.set(isKeysLoading);
            this.previousKeysCount.set(currentKeysCount);
        });
    }

    connect(): Observable<StorageValue[]> {
        return this.dataObservable$;
    }

    disconnect(): void {
        this.data$.set([]);
        this.loading$.set(false);
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

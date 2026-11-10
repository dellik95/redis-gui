import { Injectable, inject } from '@angular/core';
import { RedisDataType } from '../../models/redis-keys.types';
import { RedisTypeHandler } from './redis-type-handler.interface';
import { StringTypeHandler } from './string-type.handler';
import { HashTypeHandler } from './hash-type.handler';
import { ListTypeHandler } from './list-type.handler';
import { SetTypeHandler } from './set-type.handler';
import { SortedSetTypeHandler } from './sorted-set-type.handler';
import { StreamTypeHandler } from './stream-type.handler';

/**
 * Registry service for Redis type handlers
 * 
 * Manages the collection of type-specific handlers using the Strategy pattern
 * Provides a centralized way to get the appropriate handler for any Redis data type
 * 
 * @example
 * ```typescript
 * const registry = inject(RedisTypeHandlerRegistry);
 * const handler = registry.getHandler(RedisDataType.Hash);
 * const json = handler.toJson(hashData);
 * ```
 */
@Injectable({
  providedIn: 'root'
})
export class RedisTypeHandlerRegistry {
  private readonly handlers = new Map<RedisDataType, RedisTypeHandler>();

  constructor() {
    const stringHandler = inject(StringTypeHandler);
    const hashHandler = inject(HashTypeHandler);
    const listHandler = inject(ListTypeHandler);
    const setHandler = inject(SetTypeHandler);
    const sortedSetHandler = inject(SortedSetTypeHandler);
    const streamHandler = inject(StreamTypeHandler);

    this.handlers.set(RedisDataType.String, stringHandler);
    this.handlers.set(RedisDataType.Hash, hashHandler);
    this.handlers.set(RedisDataType.List, listHandler);
    this.handlers.set(RedisDataType.Set, setHandler);
    this.handlers.set(RedisDataType.SortedSet, sortedSetHandler);
    this.handlers.set(RedisDataType.Stream, streamHandler);
  }

  /**
   * Get the handler for a specific Redis data type (case-insensitive)
   * @param type The Redis data type
   * @returns The corresponding type handler
   * @throws Error if handler not found for type
   */
  getHandler(type: RedisDataType | string): RedisTypeHandler {
    let normalized = type;
    if (typeof type === 'string') {
      const lower = type.toLowerCase();
      if (lower === 'string') normalized = RedisDataType.String;
      else if (lower === 'list') normalized = RedisDataType.List;
      else if (lower === 'set') normalized = RedisDataType.Set;
      else if (lower === 'hash') normalized = RedisDataType.Hash;
      else if (lower === 'zset' || lower === 'sortedset') normalized = RedisDataType.SortedSet;
      else if (lower === 'stream') normalized = RedisDataType.Stream;
    }

    const handler = this.handlers.get(normalized as RedisDataType);
    
    if (!handler) {
      throw new Error(`No handler registered for Redis type: ${type}`);
    }
    
    return handler;
  }

  /**
   * Get all registered handlers
   * @returns Array of all type handlers
   */
  getAllHandlers(): RedisTypeHandler[] {
    return Array.from(this.handlers.values());
  }

  /**
   * Check if a handler exists for a type
   * @param type The Redis data type
   * @returns True if handler exists
   */
  hasHandler(type: RedisDataType): boolean {
    return this.handlers.has(type);
  }

  /**
   * Register a custom handler (useful for testing or extending)
   * @param type The Redis data type
   * @param handler The handler instance
   */
  registerHandler(type: RedisDataType, handler: RedisTypeHandler): void {
    this.handlers.set(type, handler);
  }

  /**
   * Get all supported Redis data types
   * @returns Array of supported types
   */
  getSupportedTypes(): RedisDataType[] {
    return Array.from(this.handlers.keys());
  }
}

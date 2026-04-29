import { Component, input, inject, effect, signal, output, ChangeDetectionStrategy } from '@angular/core';

import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTreeModule } from '@angular/material/tree';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';

import { RedisKeysStore } from '../../../core/stores/redis-keys.store';
import { RedisKeyInfo, RedisDataType, RedisKeyGroup, RedisValue } from '../../../models/redis-keys.types';

/**
 * Tree node structure for displaying Redis keys in a hierarchical tree view
 */
interface TreeNode {
  name: string;
  path: string;
  isGroup: boolean;
  keyCount: number;
  keyInfo?: RedisKeyInfo;
  children?: TreeNode[];
}

/**
 * Tree View List Component
 * Displays Redis keys in a hierarchical tree structure based on key delimiters
 * Groups keys by their path segments and allows navigation through the tree
 *
 * @example
 * ```html
 * <app-tree-view-list
 *   [connectionId]="connectionId"
 *   [pattern]="'*'"
 *   [delimiter]="':'"
 *   (keySelected)="onKeySelected($event)"
 * />
 * ```
 */
@Component({
  selector: 'app-tree-view-list',
  standalone: true,
  imports: [
    MatTreeModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatTooltipModule
  ],
  templateUrl: './tree-view-list.component.html',
  styleUrl: './tree-view-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TreeViewListComponent {
  // 1. Injected dependencies
  private readonly redisKeysStore = inject(RedisKeysStore);

  // 2. Inputs
  /** Required connection ID for loading keys */
  readonly connectionId = input.required<string>();

  /** Pattern to filter keys (default: '*') */
  readonly pattern = input<string>('*');

  /** Delimiter used to split key paths (default: ':') */
  readonly delimiter = input<string>(':');

  // 3. Outputs
  /** Emitted when a key is selected from the tree */
  readonly keySelected = output<{
    key: string;
    type: RedisDataType;
    value: RedisValue;
    ttl: number
  }>();

  // 4. State signals
  /** Tree data source for Material Tree */
  readonly dataSource = signal<TreeNode[]>([]);

  /** Loading state indicator */
  readonly isLoading = signal<boolean>(false);

  /** Currently loading key for emission tracking */
  private readonly pendingKey = signal<{ key: string; type: string } | null>(null);

  constructor() {
    // Load keys when connectionId or pattern changes
    effect(() => {
      const connectionId = this.connectionId();
      const pattern = this.pattern();

      if (connectionId) {
        this.redisKeysStore.loadKeys({
          connectionId,
          pattern: pattern || '*'
        });
      }
    });

    // Watch for changes in the store and group keys for tree display
    effect(() => {
      const keys = this.redisKeysStore.keys();
      const delimiter = this.delimiter();

      if (keys.length > 0) {
        // Group keys as tree structure
        const groups = this.groupKeysAsTree(keys, delimiter);
        const treeNodes = this.buildTreeFromGroups(groups);
        this.dataSource.set(treeNodes);
      } else {
        this.dataSource.set([]);
      }

      this.isLoading.set(this.redisKeysStore.isLoading());
    });

    // Watch for selectedKey changes and emit when it matches pending key
    effect(() => {
      const pending = this.pendingKey();
      const selectedKey = this.redisKeysStore.selectedKey();

      if (pending && selectedKey && selectedKey.key === pending.key) {
        this.keySelected.emit({
          key: pending.key,
          type: this.mapTypeToRedisDataType(pending.type),
          value: selectedKey.value,
          ttl: selectedKey.ttl ?? -1
        });
        // Clear pending after emission
        this.pendingKey.set(null);
      }
    });
  }

  // 5. Tree accessors (for Material Tree)
  /** Accessor function for Material Tree to get children */
  readonly childrenAccessor = (node: TreeNode) => node.children ?? [];

  /** Determines if a node has children (for expand/collapse) */
  readonly hasChild = (_: number, node: TreeNode) => node.isGroup && !!node.children && node.children.length > 0;

  // 6. Public methods
  /**
   * Handles key selection when a tree node is clicked
   * Loads the key value and emits the selection event
   *
   * @param node - The tree node that was clicked
   */
  onKeyClick(node: TreeNode): void {
    if (!node.isGroup && node.keyInfo) {
      // Set pending key to track which key we're loading
      this.pendingKey.set({
        key: node.keyInfo.key,
        type: node.keyInfo.type
      });

      // Load the key value - effect will handle emission when ready
      this.redisKeysStore.loadValue({
        connectionId: this.connectionId(),
        key: node.keyInfo.key
      });
    }
  }

  /**
   * Gets the Material icon name for a tree node
   *
   * @param node - The tree node
   * @returns Material icon name
   */
  getNodeIcon(node: TreeNode): string {
    if (node.isGroup) {
      return 'folder';
    }

    if (node.keyInfo) {
      switch (node.keyInfo.type.toLowerCase()) {
        case 'string': return 'text_fields';
        case 'list': return 'list';
        case 'set': return 'group_work';
        case 'hash': return 'tag';
        case 'sortedset': return 'sort';
        case 'stream': return 'stream';
        default: return 'description';
      }
    }

    return 'description';
  }

  /**
   * Gets the tooltip text for a tree node
   *
   * @param node - The tree node
   * @returns Tooltip text
   */
  getNodeTooltip(node: TreeNode): string {
    if (node.isGroup) {
      return `${node.name} (${node.keyCount} keys)`;
    }

    if (node.keyInfo) {
      const ttl = node.keyInfo.ttl !== null && node.keyInfo.ttl > 0
        ? `TTL: ${node.keyInfo.ttl}s`
        : 'No expiry';
      return `${node.keyInfo.type} | Size: ${node.keyInfo.size} bytes | ${ttl}`;
    }

    return node.name;
  }

  // 7. Private methods
  /**
   * Builds tree nodes from Redis key groups
   *
   * @param groups - Array of Redis key groups
   * @returns Array of tree nodes
   */
  private buildTreeFromGroups(groups: RedisKeyGroup[]): TreeNode[] {
    const result: TreeNode[] = [];

    for (const group of groups) {
      const node: TreeNode = {
        name: group.name,
        path: group.path,
        isGroup: true,
        keyCount: group.keyCount,
        children: []
      };

      // Add child groups
      if (group.children && group.children.length > 0) {
        node.children = this.buildTreeFromGroups(group.children);
      }

      // Add keys as leaf nodes
      if (group.keys && group.keys.length > 0) {
        const keyNodes = group.keys.map(key => ({
          name: this.getKeyDisplayName(key.key, group.path),
          path: key.key,
          isGroup: false,
          keyCount: 0,
          keyInfo: key
        }));

        if (!node.children) {
          node.children = [];
        }
        node.children.push(...keyNodes);
      }

      // If no children, remove the children property
      if (!node.children || node.children.length === 0) {
        delete node.children;
      }

      result.push(node);
    }

    return result;
  }

  /**
   * Extracts the display name for a key by removing the group path prefix
   *
   * @param fullKey - The full key path
   * @param groupPath - The group path to remove
   * @returns Display name for the key
   */
  private getKeyDisplayName(fullKey: string, groupPath: string): string {
    // Extract the key name by removing the group path prefix
    if (groupPath && fullKey.startsWith(groupPath)) {
      const name = fullKey.substring(groupPath.length);
      // Remove leading delimiter
      return name.replace(/^[:\/\.]/, '');
    }
    return fullKey;
  }

  /**
   * Groups Redis keys into a tree structure based on the delimiter
   *
   * @param keyInfos - Array of Redis key information
   * @param delimiter - Delimiter used to split key paths
   * @returns Array of Redis key groups organized as a tree
   */
  private groupKeysAsTree(keyInfos: RedisKeyInfo[], delimiter: string): RedisKeyGroup[] {
    const root: Map<string, RedisKeyGroup> = new Map();

    for (const keyInfo of keyInfos) {
      const parts = keyInfo.key.split(delimiter).filter(p => p.length > 0);

      if (parts.length === 0) {
        // Root level key
        if (!root.has('')) {
          root.set('', {
            name: 'Root',
            path: '',
            keys: [],
            children: [],
            keyCount: 0
          });
        }
        const rootGroup = root.get('')!;
        rootGroup.keys.push(keyInfo);
        rootGroup.keyCount++;
      } else {
        // Nested key - build tree structure
        this.addKeyToTree(root, keyInfo, parts, delimiter, 0, '');
      }
    }

    return Array.from(root.values());
  }

  /**
   * Recursively adds a key to the tree structure
   *
   * @param currentLevel - Current level of the tree (Map of groups)
   * @param keyInfo - Redis key information to add
   * @param parts - Key path parts (split by delimiter)
   * @param delimiter - Delimiter used in key paths
   * @param depth - Current depth in the tree
   * @param currentPath - Current path in the tree
   */
  private addKeyToTree(
    currentLevel: Map<string, RedisKeyGroup>,
    keyInfo: RedisKeyInfo,
    parts: string[],
    delimiter: string,
    depth: number,
    currentPath: string
  ): void {
    const part = parts[depth];
    if (!part) return;

    const newPath = currentPath ? `${currentPath}${delimiter}${part}` : part;

    // Get or create the group at this level
    if (!currentLevel.has(part)) {
      currentLevel.set(part, {
        name: part,
        path: newPath,
        keys: [],
        children: [],
        keyCount: 0
      });
    }

    const group = currentLevel.get(part)!;

    // If the next part is the last one, add the key to this group (not create a new group)
    if (depth + 1 === parts.length - 1) {
      // Next part is the leaf - add the key directly to this group
      group.keys.push(keyInfo);
      group.keyCount++;
    } else {
      // Recursively add to next level
      const childrenMap = new Map<string, RedisKeyGroup>();
      for (const child of group.children) {
        childrenMap.set(child.name, child);
      }

      this.addKeyToTree(childrenMap, keyInfo, parts, delimiter, depth + 1, newPath);

      // Update the group with new children and increment key count
      group.children = Array.from(childrenMap.values());
      group.keyCount++;
      // Update the map with the modified group
      currentLevel.set(part, group);
    }
  }

  /**
   * Maps string type to RedisDataType enum
   *
   * @param type - String representation of Redis data type
   * @returns RedisDataType enum value
   */
  private mapTypeToRedisDataType(type: string): RedisDataType {
    switch (type.toLowerCase()) {
      case 'string': return RedisDataType.String;
      case 'list': return RedisDataType.List;
      case 'set': return RedisDataType.Set;
      case 'hash': return RedisDataType.Hash;
      case 'sortedset': return RedisDataType.SortedSet;
      case 'stream': return RedisDataType.Stream;
      default: return RedisDataType.String;
    }
  }
}

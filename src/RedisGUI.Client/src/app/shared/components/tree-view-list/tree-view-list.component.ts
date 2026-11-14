import {
  Component,
  input,
  model,
  inject,
  effect,
  signal,
  computed,
  viewChild,
  output,
  ChangeDetectionStrategy
} from '@angular/core';

import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTree, MatTreeModule } from '@angular/material/tree';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar } from '@angular/material/snack-bar';

import { RedisKeysStore } from '../../../core/stores/redis-keys.store';
import { RedisKeyInfo, SelectedRedisKey, toRedisDataType } from '../../../models/redis-keys.types';

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
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TreeViewListComponent {
  // 1. Injected dependencies
  private readonly redisKeysStore = inject(RedisKeysStore);
  private readonly snackBar = inject(MatSnackBar);

  // 2. Inputs
  /** Required connection ID for loading keys */
  readonly connectionId = input.required<string>();

  /** Pattern to filter keys (default: '*') */
  readonly pattern = model<string>('*');

  /** Delimiter used to split key paths (default: ':') */
  readonly delimiter = model<string>(':');

  /** Currently active key path for selection highlight */
  readonly suppliedKeys = input<RedisKeyInfo[]>();
  private readonly displayedKeys = computed(
    () => this.suppliedKeys() ?? this.redisKeysStore.keys()
  );
  readonly activeKey = input<string | null>(null);

  // 3. Outputs
  /** Emitted when a key is selected from the tree */
  readonly keySelected = output<SelectedRedisKey>();

  /** Emitted when adding a new key directly into a subfolder */
  readonly addKeyInFolder = output<string>();

  /** Emitted when deleting a specific key in the tree */
  readonly deleteKey = output<string>();

  /** Emitted when renaming a key in the tree */
  readonly renameKey = output<string>();

  /** Emitted when duplicating a key in the tree */
  readonly duplicateKey = output<string>();

  /** Emitted when deleting an entire folder/namespace in the tree */
  readonly deleteFolder = output<{
    path: string;
    name: string;
    keyCount: number;
    keys: string[];
  }>();

  // 4. View children
  readonly tree = viewChild(MatTree);

  // 5. State signals
  /** Tree data source for Material Tree */
  readonly dataSource = signal<TreeNode[]>([]);

  /** Total number of leaf keys */
  readonly totalKeysCount = computed<number>(() => this.displayedKeys().length);

  /** Total number of folder groups */
  readonly totalFoldersCount = computed<number>(() => {
    let count = 0;
    const countGroups = (nodes: TreeNode[]) => {
      for (const n of nodes) {
        if (n.isGroup) {
          count++;
          if (n.children) countGroups(n.children);
        }
      }
    };
    countGroups(this.dataSource());
    return count;
  });

  /** Loading state indicator computed from store */
  readonly isLoading = computed<boolean>(() => this.redisKeysStore.isLoading());

  /** Persistent set of expanded group paths */
  readonly expandedNodePaths = signal<Set<string>>(new Set<string>());

  constructor() {
    // Load keys when connectionId or pattern changes
    effect(() => {
      const connectionId = this.connectionId();
      const pattern = this.pattern();

      if (connectionId && this.suppliedKeys() === undefined) {
        this.redisKeysStore.loadKeys({
          connectionId,
          pattern: pattern || '*'
        });
      }
    });

    // Watch for changes in the store keys or delimiter and group keys for tree display.
    // Isolated from store.isLoading() so fetching individual key values doesn't reconstruct the tree.
    effect(() => {
      const keys = this.displayedKeys();
      const delimiter = this.delimiter();

      if (keys.length > 0) {
        const treeNodes = this.buildTree(keys, delimiter);
        this.dataSource.set(treeNodes);

        // Re-apply expansion state so tree does not collapse on data refresh
        setTimeout(() => {
          const matTree = this.tree();
          if (matTree) {
            for (const path of this.expandedNodePaths()) {
              const node = this.findGroupNodeByPath(treeNodes, path);
              if (node) {
                matTree.expand(node);
              }
            }
          }
        }, 50);
      } else {
        this.dataSource.set([]);
      }
    });

    // Automatically expand parent folders of activeKey so it remains visible
    effect(() => {
      const active = this.activeKey();
      const delimiter = this.delimiter();
      const nodes = this.dataSource();

      if (active && nodes.length > 0) {
        queueMicrotask(() => {
          this.expandParentsOfKey(active, delimiter, nodes);
        });
      }
    });
  }

  // 6. Tree accessors and trackers (for Material Tree)
  /** Accessor function for Material Tree to get children */
  readonly childrenAccessor = (node: TreeNode) => node.children ?? [];

  /** Determines if a node is a group / folder */
  readonly hasChild = (_: number, node: TreeNode) => node.isGroup;

  /** TrackBy function for Material Tree recycling */
  readonly trackByPath = (_: number, node: TreeNode) => this.getNodeId(node);

  /** Key extractor function for preserving expansion state */
  readonly getExpansionKey = (node: TreeNode) => this.getNodeId(node);

  /**
   * Unique node identity. A folder and a plain key may share the same path
   * (e.g. key "test" and folder "test" built from "test:child"), so the node kind is part of the id.
   * Without this the tree would recycle a folder row for a key row (or vice versa).
   */
  private getNodeId(node: TreeNode): string {
    return node.isGroup ? `group:${node.path}` : `key:${node.path}`;
  }

  // 7. Public methods
  /**
   * Handles key selection when a tree node is clicked.
   * Emits the selected key immediately to open the edit panel,
   * while ensuring the parent folder path remains expanded.
   *
   * @param node - The tree node that was clicked
   */
  onKeyClick(node: TreeNode): void {
    if (!node.isGroup && node.keyInfo) {
      // Ensure the parent groups of this clicked key remain expanded
      this.expandParentsOfKey(node.keyInfo.key, this.delimiter(), this.dataSource());

      this.keySelected.emit({
        key: node.keyInfo.key,
        type: toRedisDataType(node.keyInfo.type),
        ttl: node.keyInfo.ttl ?? -1
      });
    }
  }

  /**
   * Toggles a group node open or closed and persists its path in the set
   */
  /**
   * Adds a new key directly into the specified subfolder / group
   */
  onAddKeyInGroup(node: TreeNode, event: MouseEvent): void {
    event.stopPropagation();
    const delim = this.delimiter() || ':';
    const folderPrefix = node.path.endsWith(delim) ? node.path : `${node.path}${delim}`;
    this.addKeyInFolder.emit(folderPrefix);
  }

  /**
   * Copies the folder prefix to the clipboard
   */
  onCopyFolderPath(node: TreeNode, event: MouseEvent): void {
    event.stopPropagation();
    const delim = this.delimiter() || ':';
    const folderPrefix = node.path.endsWith(delim) ? node.path : `${node.path}${delim}`;
    navigator.clipboard.writeText(folderPrefix).then(() => {
      this.snackBar.open(`Copied folder "${folderPrefix}" to clipboard`, 'Close', {
        duration: 2500
      });
    });
  }

  /**
   * Triggers deletion of the entire folder and all nested keys
   */
  onDeleteFolder(node: TreeNode, event: MouseEvent): void {
    event.stopPropagation();
    const allKeys = this.collectAllKeysUnderNode(node);
    this.deleteFolder.emit({
      path: node.path,
      name: node.name,
      keyCount: node.keyCount,
      keys: allKeys
    });
  }

  /**
   * Copies a key name to clipboard
   */
  onCopyKey(node: TreeNode, event: MouseEvent): void {
    event.stopPropagation();
    const key = node.keyInfo?.key || node.path;
    navigator.clipboard.writeText(key).then(() => {
      this.snackBar.open(`Copied "${key}" to clipboard`, 'Close', { duration: 2500 });
    });
  }

  /**
   * Triggers deletion of an individual key
   */
  onDeleteKey(node: TreeNode, event: MouseEvent): void {
    event.stopPropagation();
    const key = node.keyInfo?.key || node.path;
    this.deleteKey.emit(key);
  }

  /**
   * Triggers renaming of an individual key
   */
  onRenameKey(node: TreeNode, event: MouseEvent): void {
    event.stopPropagation();
    const key = node.keyInfo?.key || node.path;
    this.renameKey.emit(key);
  }

  /**
   * Triggers duplication of an individual key
   */
  onDuplicateKey(node: TreeNode, event: MouseEvent): void {
    event.stopPropagation();
    const key = node.keyInfo?.key || node.path;
    this.duplicateKey.emit(key);
  }

  /**
   * Collects all leaf key names recursively under a given tree node
   */
  collectAllKeysUnderNode(node: TreeNode): string[] {
    if (!node.isGroup) {
      return node.keyInfo ? [node.keyInfo.key] : [node.path];
    }
    const keys: string[] = [];
    if (node.children) {
      for (const child of node.children) {
        keys.push(...this.collectAllKeysUnderNode(child));
      }
    }
    return keys;
  }

  /**
   * Expands all group folders across the entire tree
   */
  expandAll(): void {
    const allPaths = new Set<string>();
    const collectGroupPaths = (nodes: TreeNode[]) => {
      for (const n of nodes) {
        if (n.isGroup) {
          allPaths.add(n.path);
          if (n.children) collectGroupPaths(n.children);
        }
      }
    };
    collectGroupPaths(this.dataSource());
    this.expandedNodePaths.set(allPaths);
    const matTree = this.tree();
    if (matTree) {
      for (const n of this.dataSource()) {
        matTree.expandDescendants(n);
      }
    }
  }

  /**
   * Collapses all folders across the tree
   */
  collapseAll(): void {
    this.expandedNodePaths.set(new Set<string>());
    const matTree = this.tree();
    if (matTree) {
      for (const n of this.dataSource()) {
        matTree.collapseDescendants(n);
      }
    }
  }

  toggleGroup(node: TreeNode, event?: MouseEvent): void {
    if (event) {
      event.stopPropagation();
    }
    const current = new Set(this.expandedNodePaths());
    const isCurrentlyExpanded = current.has(node.path);

    if (isCurrentlyExpanded) {
      current.delete(node.path);
    } else {
      current.add(node.path);
    }
    this.expandedNodePaths.set(current);

    const matTree = this.tree();
    if (matTree) {
      if (!isCurrentlyExpanded) {
        matTree.expand(node);
      } else {
        matTree.collapse(node);
      }
    }
  }

  /**
   * Checks whether a group node is currently expanded
   */
  isExpanded(node: TreeNode): boolean {
    if (this.expandedNodePaths().has(node.path)) {
      return true;
    }
    const matTree = this.tree();
    return matTree ? matTree.isExpanded(node) : false;
  }

  /**
   * Expands all parent folder nodes containing the specified key.
   */
  expandParentsOfKey(keyPath: string, delimiter: string, nodes: TreeNode[]): void {
    if (!keyPath) return;

    const parts = keyPath.split(delimiter).filter(p => p.length > 0);
    if (parts.length <= 1) return;

    const current = new Set(this.expandedNodePaths());
    const matTree = this.tree();
    let currentPath = '';

    for (let i = 0; i < parts.length - 1; i++) {
      currentPath = currentPath ? `${currentPath}${delimiter}${parts[i]}` : parts[i]!;
      current.add(currentPath);
      const groupNode = this.findGroupNodeByPath(nodes, currentPath);
      if (groupNode && matTree) {
        matTree.expand(groupNode);
      }
    }

    this.expandedNodePaths.set(current);
  }

  private findGroupNodeByPath(nodes: TreeNode[], targetPath: string): TreeNode | null {
    for (const node of nodes) {
      if (node.isGroup && node.path === targetPath) {
        return node;
      }
      if (node.children && node.children.length > 0) {
        const found = this.findGroupNodeByPath(node.children, targetPath);
        if (found) return found;
      }
    }
    return null;
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
        case 'string':
          return 'text_fields';
        case 'list':
          return 'list';
        case 'set':
          return 'group_work';
        case 'hash':
          return 'tag';
        case 'sortedset':
          return 'sort';
        case 'stream':
          return 'stream';
        default:
          return 'description';
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
      const ttl =
        node.keyInfo.ttl !== null && node.keyInfo.ttl > 0
          ? `TTL: ${node.keyInfo.ttl}s`
          : 'No expiry';
      return `${node.keyInfo.type} | Size: ${node.keyInfo.size} bytes | ${ttl}`;
    }

    return node.name;
  }

  // 7. Private methods
  /**
   * Builds a hierarchical tree from a flat list of Redis keys.
   *
   * Keys are split by the delimiter: every segment except the last becomes a folder,
   * the last segment becomes a leaf. Keys without a delimiter (e.g. "Test") are rendered
   * as leaf nodes at the root level, never as empty folders.
   *
   * @param keyInfos - Flat list of Redis keys
   * @param delimiter - Delimiter used to split key paths
   * @returns Root-level tree nodes (folders first, then keys)
   */
  private buildTree(keyInfos: RedisKeyInfo[], delimiter: string): TreeNode[] {
    const roots: TreeNode[] = [];
    const groupsByPath = new Map<string, TreeNode>();

    for (const keyInfo of keyInfos) {
      const segments = delimiter
        ? keyInfo.key.split(delimiter).filter(p => p.length > 0)
        : [keyInfo.key];

      // Plain keys (no delimiter) live at the root level as leaves
      if (segments.length <= 1) {
        roots.push(this.createLeafNode(keyInfo, keyInfo.key));
        continue;
      }

      let siblings = roots;
      let currentPath = '';

      for (let i = 0; i < segments.length - 1; i++) {
        const segment = segments[i]!;
        currentPath = currentPath ? `${currentPath}${delimiter}${segment}` : segment;

        let group = groupsByPath.get(currentPath);
        if (!group) {
          group = { name: segment, path: currentPath, isGroup: true, keyCount: 0, children: [] };
          groupsByPath.set(currentPath, group);
          siblings.push(group);
        }

        group.keyCount++;
        siblings = group.children!;
      }

      siblings.push(this.createLeafNode(keyInfo, segments[segments.length - 1]!));
    }

    this.sortTreeNodes(roots);
    return roots;
  }

  /**
   * Creates a leaf (key) tree node
   */
  private createLeafNode(keyInfo: RedisKeyInfo, name: string): TreeNode {
    return {
      name,
      path: keyInfo.key,
      isGroup: false,
      keyCount: 0,
      keyInfo
    };
  }

  /**
   * Recursively sorts nodes: folders first, then keys, both alphabetically (natural order)
   */
  private sortTreeNodes(nodes: TreeNode[]): void {
    nodes.sort((a, b) => {
      if (a.isGroup !== b.isGroup) {
        return a.isGroup ? -1 : 1;
      }
      return a.name.localeCompare(b.name, undefined, { numeric: true, sensitivity: 'base' });
    });

    for (const node of nodes) {
      if (node.children && node.children.length > 0) {
        this.sortTreeNodes(node.children);
      }
    }
  }

  /**
   * Maps string type to RedisDataType enum
   *
   * @param type - String representation of Redis data type
   * @returns RedisDataType enum value
   */
}

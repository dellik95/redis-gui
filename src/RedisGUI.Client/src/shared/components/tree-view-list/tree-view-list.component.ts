import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTreeModule } from '@angular/material/tree';

interface TreeNode {
  name: string;
  children?: TreeNode[];
}

@Component({
  selector: 'app-tree-view-list',
  imports: [MatTreeModule, MatIconModule, MatButtonModule],
  templateUrl: './tree-view-list.component.html',
  styleUrl: './tree-view-list.component.scss',
})
export class TreeViewListComponent {

  dataSource: TreeNode[] = [];

  constructor() {
    const rawData = [
      'Child1:Child1.1',
      'Child1:Child1.2',
      'Child1.3',
      'Child2',
      'Child3:Child3.1:Child3.1.1',
    ];
    this.dataSource = this.buildTree(rawData);
  }

  childrenAccessor = (node: TreeNode) => node.children ?? [];

  hasChild = (_: number, node: TreeNode) => !!node.children && node.children.length > 0;

  private buildTree(data: string[]): TreeNode[] {
    const tree: TreeNode[] = [];
    data.forEach((line) => {
      const parts = line.split(':');
      let currentLevel = tree;
      parts.forEach((part, index) => {
        let node = currentLevel.find((n) => n.name === part);
        if (!node) {
          node = { name: part, children: [] };
          currentLevel.push(node);
        }
        if (index === parts.length - 1) {
          delete node.children;
        } else {
          currentLevel = node.children!;
        }
      });
    });

    return tree;
  }
}

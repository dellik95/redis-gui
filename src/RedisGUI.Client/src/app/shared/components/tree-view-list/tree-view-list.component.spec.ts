import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

import { TreeViewListComponent } from './tree-view-list.component';
import { RedisKeysStore } from '../../../core/stores/redis-keys.store';
import { RedisKeyInfo } from '../../../models/redis-keys.types';

const key = (name: string, type = 'String'): RedisKeyInfo => ({
  key: name,
  type,
  size: 1,
  ttl: null,
  memoryUsage: null
});

describe('TreeViewListComponent', () => {
  let fixture: ComponentFixture<TreeViewListComponent>;
  const keys = signal<RedisKeyInfo[]>([]);

  const fakeStore = {
    keys,
    isLoading: signal(false),
    loadKeys: () => undefined
  };

  const render = async (keyNames: RedisKeyInfo[]) => {
    keys.set(keyNames);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  };

  const rows = (selector: string): string[] =>
    Array.from(fixture.nativeElement.querySelectorAll(selector) as NodeListOf<HTMLElement>).map(
      el => (el.querySelector('.node-name, .group-name')?.textContent ?? '').trim()
    );

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TreeViewListComponent],
      providers: [
        { provide: RedisKeysStore, useValue: fakeStore },
        { provide: MatSnackBar, useValue: { open: () => undefined } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TreeViewListComponent);
    fixture.componentRef.setInput('connectionId', 'conn-1');
  });

  it('renders keys without delimiter as root-level leaf keys, not folders', async () => {
    await render([
      key('test'),
      key('афівафів'),
      key('testrewqrweq'),
      key('demo:users:1'),
      key('demo:config')
    ]);

    expect(rows('.tree-group-node')).toEqual(['demo']);
    expect(rows('.tree-leaf-node')).toEqual(
      jasmine.arrayContaining(['test', 'testrewqrweq', 'афівафів'])
    );
  });

  it('builds nested folders with correct key counts', async () => {
    await render([key('a:b:c'), key('a:b:d'), key('a:e'), key('plain')]);
    const tree = fixture.componentInstance.dataSource();
    expect(tree.map(node => [node.name, node.isGroup])).toEqual([
      ['a', true],
      ['plain', false]
    ]);
    expect(tree[0]?.keyCount).toBe(3);
    expect(tree[0]?.children?.map(node => [node.name, node.isGroup])).toEqual([
      ['b', true],
      ['e', false]
    ]);
    expect(tree[0]?.children?.[0]?.keyCount).toBe(2);
  });

  it('keeps a plain key and a folder with the same name as separate nodes', async () => {
    await render([key('test'), key('test:child')]);

    expect(rows('.tree-group-node')).toEqual(['test']);
    expect(rows('.tree-leaf-node')).toEqual(['test']);
  });
});

import { Component, input, output } from '@angular/core';
import { MatIcon } from '@angular/material/icon';

export interface RedisTypeCounts {
  all: number;
  string: number;
  list: number;
  set: number;
  hash: number;
  sortedset: number;
  stream: number;
}

@Component({
  selector: 'app-key-type-filter',
  imports: [MatIcon],
  templateUrl: './key-type-filter.html',
  styleUrl: './key-type-filter.scss'
})
export class KeyTypeFilter {
  readonly typeFilter = input.required<string>();
  readonly typeCounts = input.required<RedisTypeCounts>();
  readonly filterChange = output<string>();
  readonly typeFilters = [
    { key: 'all', value: '', label: 'All', icon: '' },
    { key: 'string', value: 'string', label: 'String', icon: 'text_fields' },
    { key: 'hash', value: 'hash', label: 'Hash', icon: 'tag' },
    { key: 'list', value: 'list', label: 'List', icon: 'list' },
    { key: 'set', value: 'set', label: 'Set', icon: 'group_work' },
    { key: 'sortedset', value: 'sortedset', label: 'Sorted Set', icon: 'sort' },
    { key: 'stream', value: 'stream', label: 'Stream', icon: 'stream' }
  ] as const;
}

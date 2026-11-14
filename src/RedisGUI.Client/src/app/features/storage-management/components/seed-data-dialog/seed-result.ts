import { Component, computed, input } from '@angular/core';
import { UpperCasePipe } from '@angular/common';
import { MatIcon } from '@angular/material/icon';
import type { SeedDataResult } from '../../../../core/services/redis-cli.service';

@Component({
  selector: 'app-seed-result',
  imports: [UpperCasePipe, MatIcon],
  templateUrl: './seed-result.html',
  styleUrl: './seed-result.scss'
})
export class SeedResult {
  readonly result = input.required<SeedDataResult>();
  readonly typeIcon = input.required<(type: string) => string>();
  readonly typeCounts = computed(() => Object.entries(this.result().keyCountPerType));
}

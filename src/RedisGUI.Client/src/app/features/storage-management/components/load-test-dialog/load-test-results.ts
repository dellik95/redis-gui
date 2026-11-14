import { Component, input } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { MatIcon } from '@angular/material/icon';
import type { LoadTestResult } from '../../../../core/services/redis-cli.service';

@Component({
  selector: 'app-load-test-results',
  imports: [DecimalPipe, MatIcon],
  templateUrl: './load-test-results.html',
  styleUrl: './load-test-results.scss'
})
export class LoadTestResults {
  readonly database = input.required<number>();
  readonly result = input.required<LoadTestResult>();

  calcBarWidth(val: number, max: number): number {
    if (!max || max <= 0) return 10;
    return Math.max(5, Math.min(100, (val / max) * 100));
  }
}

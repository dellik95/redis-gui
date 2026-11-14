import { Component, input } from '@angular/core';
import { DecimalPipe, UpperCasePipe } from '@angular/common';
import { MatIcon } from '@angular/material/icon';
import { MatProgressBar } from '@angular/material/progress-bar';
import type { LoadTestProgress } from '../../../../core/services/redis-cli.service';

@Component({
  selector: 'app-load-test-progress',
  imports: [DecimalPipe, UpperCasePipe, MatIcon, MatProgressBar],
  templateUrl: './load-test-progress.html',
  styleUrl: './load-test-progress.scss'
})
export class LoadTestProgressView {
  readonly liveProgress = input.required<LoadTestProgress | null>();
  readonly benchmarkMode = input.required<'duration' | 'count'>();
  readonly durationSeconds = input.required<number>();
  readonly concurrency = input.required<number>();
  readonly operationType = input.required<string>();
  readonly database = input.required<number>();
  readonly progressPercent = input.required<number>();

  formatSeconds(sec: number): string {
    const s = Math.floor(sec);
    const ms = Math.floor((sec - s) * 10);
    const m = Math.floor(s / 60);
    const remS = s % 60;
    return `${m.toString().padStart(2, '0')}:${remS.toString().padStart(2, '0')}.${ms}`;
  }
}

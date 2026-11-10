import { Component, inject, signal, ChangeDetectionStrategy, DestroyRef } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RedisCliService, LoadTestRequest, LoadTestResult, LoadTestProgress } from '../../../../core/services/redis-cli.service';

export interface LoadTestDialogData {
  connectionId: string;
  database: number;
}

@Component({
  selector: 'app-load-test-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatProgressBarModule,
    MatTooltipModule,
    DecimalPipe
  ],
  template: `
    <div class="dialog-header">
      <div class="title-row">
        <div class="header-icon-box" [class.running]="isRunning()">
          <mat-icon class="header-icon">{{ isRunning() ? 'speed' : 'speed' }}</mat-icon>
        </div>
        <div class="title-text-group">
          <span class="title-text">Redis Load Testing & Benchmark</span>
          <span class="title-subtext">Generate synthetic traffic, benchmark real-time latency, and auto-purge test keys</span>
        </div>
      </div>
      <div class="header-actions">
        @if (isRunning()) {
          <button mat-flat-button color="warn" class="header-stop-btn" (click)="stopLoadTest()" [disabled]="isStopping()">
            <mat-icon>stop</mat-icon>
            <span>{{ isStopping() ? 'Stopping...' : 'Stop Benchmark' }}</span>
          </button>
        }
        <button mat-icon-button mat-dialog-close matTooltip="Close" class="close-btn" [disabled]="isRunning() && !isStopping()">
          <mat-icon>close</mat-icon>
        </button>
      </div>
    </div>

    <mat-dialog-content class="dialog-content">
      <!-- Config Form (shown when not running and no results) -->
      @if (!isRunning() && !result()) {
        <div class="config-section">
          <!-- Information notice -->
          <div class="info-banner">
            <mat-icon class="info-icon">verified_user</mat-icon>
            <div class="info-text">
              <strong>Guaranteed Safe & Isolated:</strong> Benchmarks execute with dedicated keys (<code>__bench_*:</code>). All test keys are automatically purged and verified after completion or cancellation.
            </div>
          </div>

          <!-- Benchmark Mode Switch: Duration vs Operations -->
          <div class="form-group">
            <label class="form-label">Benchmark Mode</label>
            <div class="mode-switch-group">
              <button
                type="button"
                class="mode-switch-btn"
                [class.active]="benchmarkMode() === 'duration'"
                (click)="benchmarkMode.set('duration')">
                <mat-icon>hourglass_top</mat-icon>
                <span>By Duration (Continuous Stream)</span>
              </button>
              <button
                type="button"
                class="mode-switch-btn"
                [class.active]="benchmarkMode() === 'count'"
                (click)="benchmarkMode.set('count')">
                <mat-icon>tag</mat-icon>
                <span>By Operations Count</span>
              </button>
            </div>
          </div>

          <!-- If Duration Mode: Duration Pills -->
          @if (benchmarkMode() === 'duration') {
            <div class="form-group">
              <label class="form-label">Test Duration (seconds)</label>
              <div class="preset-pill-group">
                @for (sec of [5, 10, 30, 60, 120, 300]; track sec) {
                  <button
                    type="button"
                    class="preset-pill"
                    [class.active]="durationSeconds() === sec"
                    (click)="durationSeconds.set(sec)">
                    {{ sec >= 60 ? (sec / 60) + ' min' : sec + ' s' }}
                  </button>
                }
              </div>
            </div>
          }

          <!-- If Count Mode: Operations Count Pills -->
          @if (benchmarkMode() === 'count') {
            <div class="form-group">
              <label class="form-label">Total Operations</label>
              <div class="preset-pill-group">
                @for (count of [200, 500, 1000, 2500, 5000, 10000]; track count) {
                  <button
                    type="button"
                    class="preset-pill"
                    [class.active]="operationsCount() === count"
                    (click)="operationsCount.set(count)">
                    {{ count | number }}
                  </button>
                }
              </div>
            </div>
          }

          <!-- Concurrency Selector -->
          <div class="form-group">
            <label class="form-label">Concurrent Clients / Streams</label>
            <div class="preset-pill-group">
              @for (c of [1, 5, 10, 20, 50]; track c) {
                <button
                  type="button"
                  class="preset-pill"
                  [class.active]="concurrency() === c"
                  (click)="concurrency.set(c)">
                  {{ c }} {{ c === 1 ? 'client' : 'clients' }}
                </button>
              }
            </div>
          </div>

          <!-- Operation Type Selector -->
          <div class="form-group">
            <label class="form-label">Traffic Workload</label>
            <div class="workload-grid">
              <button
                type="button"
                class="workload-card"
                [class.active]="operationType() === 'mixed'"
                (click)="operationType.set('mixed')">
                <mat-icon>swap_horiz</mat-icon>
                <div class="workload-info">
                  <span class="workload-title">Mixed (50% SET / 50% GET)</span>
                  <span class="workload-desc">Balanced real-world read/write benchmark</span>
                </div>
              </button>

              <button
                type="button"
                class="workload-card"
                [class.active]="operationType() === 'write'"
                (click)="operationType.set('write')">
                <mat-icon>edit_note</mat-icon>
                <div class="workload-info">
                  <span class="workload-title">Write-Intensive (SET only)</span>
                  <span class="workload-desc">Measures memory allocation and write throughput</span>
                </div>
              </button>

              <button
                type="button"
                class="workload-card"
                [class.active]="operationType() === 'read'"
                (click)="operationType.set('read')">
                <mat-icon>visibility</mat-icon>
                <div class="workload-info">
                  <span class="workload-title">Read-Intensive (GET only)</span>
                  <span class="workload-desc">Measures query retrieval speed on warmed keys</span>
                </div>
              </button>
            </div>
          </div>

          <!-- Payload Size Selector -->
          <div class="form-group">
            <label class="form-label">Payload Size per Key</label>
            <div class="preset-pill-group">
              <button type="button" class="preset-pill" [class.active]="payloadSize() === 64" (click)="payloadSize.set(64)">64 B</button>
              <button type="button" class="preset-pill" [class.active]="payloadSize() === 128" (click)="payloadSize.set(128)">128 B (Standard)</button>
              <button type="button" class="preset-pill" [class.active]="payloadSize() === 512" (click)="payloadSize.set(512)">512 B</button>
              <button type="button" class="preset-pill" [class.active]="payloadSize() === 1024" (click)="payloadSize.set(1024)">1 KB</button>
            </div>
          </div>

          <div class="target-db-note">
            Target Database: <span class="db-chip">DB {{ data.database }}</span>
          </div>

          @if (errorMessage()) {
            <div class="error-banner">
              <mat-icon>error_outline</mat-icon>
              <span>{{ errorMessage() }}</span>
            </div>
          }
        </div>
      }

      <!-- Real-Time Live Streaming State -->
      @if (isRunning()) {
        <div class="running-dashboard">
          <div class="running-header">
            <div class="live-pill">
              <span class="live-dot"></span>
              <span>LIVE BENCHMARK IN PROGRESS</span>
            </div>
            <div class="live-meta">
              <span>DB {{ data.database }}</span> • 
              <span>{{ concurrency() }} clients</span> • 
              <span>{{ operationType() | uppercase }}</span>
            </div>
          </div>

          <!-- Live Progress Metric Grid -->
          <div class="live-kpi-grid">
            <div class="live-kpi-card ops">
              <span class="live-kpi-label">Current Throughput</span>
              <span class="live-kpi-val">{{ (liveProgress()?.currentOpsPerSecond ?? 0) | number:'1.0-0' }} <small>ops/s</small></span>
              <span class="live-kpi-sub">Overall: {{ (liveProgress()?.overallOpsPerSecond ?? 0) | number:'1.0-0' }} ops/s</span>
            </div>

            <div class="live-kpi-card time">
              <span class="live-kpi-label">Elapsed Time</span>
              <span class="live-kpi-val">{{ formatSeconds(liveProgress()?.elapsedSeconds ?? 0) }}</span>
              <span class="live-kpi-sub">
                @if (benchmarkMode() === 'duration') {
                  Target: {{ durationSeconds() }}s
                } @else {
                  Completed: {{ liveProgress()?.completedOperations | number }} ops
                }
              </span>
            </div>

            <div class="live-kpi-card lat">
              <span class="live-kpi-label">Live Avg Latency</span>
              <span class="live-kpi-val">{{ (liveProgress()?.avgLatencyMs ?? 0) | number:'1.2-3' }} <small>ms</small></span>
              <span class="live-kpi-sub">Sub-millisecond</span>
            </div>

            <div class="live-kpi-card succ">
              <span class="live-kpi-label">Success Rate</span>
              <span class="live-kpi-val">{{ (liveProgress()?.successRatePercent ?? 100) | number:'1.0-1' }}%</span>
              <span class="live-kpi-sub">{{ liveProgress()?.completedOperations | number }} ops completed</span>
            </div>
          </div>

          <!-- Progress Bar -->
          <div class="progress-bar-container">
            <div class="progress-header">
              <span>Progress</span>
              <span>{{ getProgressPercent() | number:'1.0-1' }}%</span>
            </div>
            <mat-progress-bar
              [mode]="benchmarkMode() === 'duration' || (liveProgress()?.totalOperations ?? 0) > 0 ? 'determinate' : 'indeterminate'"
              [value]="getProgressPercent()">
            </mat-progress-bar>
          </div>

          <div class="cleanup-live-notice">
            <mat-icon>security</mat-icon>
            <span>Auto-cleanup active: benchmark keys will be wiped immediately upon finish or stop.</span>
          </div>
        </div>
      }

      <!-- Test Results View -->
      @if (!isRunning() && result(); as res) {
        <div class="results-container">
          <!-- Top KPI metrics grid -->
          <div class="kpi-grid">
            <div class="kpi-card highlight">
              <div class="kpi-icon-wrap ops">
                <mat-icon>bolt</mat-icon>
              </div>
              <div class="kpi-body">
                <span class="kpi-label">Throughput</span>
                <span class="kpi-value">{{ res.opsPerSecond | number:'1.0-0' }} <small>ops/s</small></span>
              </div>
            </div>

            <div class="kpi-card">
              <div class="kpi-icon-wrap time">
                <mat-icon>timer</mat-icon>
              </div>
              <div class="kpi-body">
                <span class="kpi-label">Total Duration</span>
                <span class="kpi-value">{{ (res.totalDurationMs / 1000) | number:'1.2-2' }} <small>s</small></span>
              </div>
            </div>

            <div class="kpi-card">
              <div class="kpi-icon-wrap avg">
                <mat-icon>insights</mat-icon>
              </div>
              <div class="kpi-body">
                <span class="kpi-label">Avg Latency</span>
                <span class="kpi-value">{{ res.avgLatencyMs | number:'1.2-3' }} <small>ms</small></span>
              </div>
            </div>

            <div class="kpi-card success">
              <div class="kpi-icon-wrap success">
                <mat-icon>task_alt</mat-icon>
              </div>
              <div class="kpi-body">
                <span class="kpi-label">Success Rate</span>
                <span class="kpi-value">{{ ((res.successfulOperations / (res.totalOperations || 1)) * 100) | number:'1.0-1' }}%</span>
                <span class="kpi-sub">({{ res.successfulOperations }} / {{ res.totalOperations }})</span>
              </div>
            </div>
          </div>

          <!-- Cleanup confirmation badge -->
          <div class="cleanup-success-card">
            <mat-icon class="cleanup-icon">check_circle</mat-icon>
            <div class="cleanup-text">
              <strong>Test Data Purged:</strong>
              <span>{{ res.cleanedKeysCount }} test keys were successfully flushed and cleaned up from DB {{ data.database }}. No orphan data remains.</span>
            </div>
          </div>

          <!-- Latency Distribution Percentiles -->
          <div class="percentiles-section">
            <h4 class="section-title">Latency Percentiles (ms)</h4>
            <div class="percentile-bars-grid">
              <div class="percentile-row">
                <span class="pct-name">Min</span>
                <div class="pct-bar-track">
                  <div class="pct-bar-fill min" [style.width.%]="calcBarWidth(res.minLatencyMs, res.maxLatencyMs)"></div>
                </div>
                <span class="pct-val">{{ res.minLatencyMs | number:'1.2-3' }} ms</span>
              </div>

              <div class="percentile-row">
                <span class="pct-name">P50 (Median)</span>
                <div class="pct-bar-track">
                  <div class="pct-bar-fill p50" [style.width.%]="calcBarWidth(res.p50LatencyMs, res.maxLatencyMs)"></div>
                </div>
                <span class="pct-val">{{ res.p50LatencyMs | number:'1.2-3' }} ms</span>
              </div>

              <div class="percentile-row">
                <span class="pct-name">P90</span>
                <div class="pct-bar-track">
                  <div class="pct-bar-fill p90" [style.width.%]="calcBarWidth(res.p90LatencyMs, res.maxLatencyMs)"></div>
                </div>
                <span class="pct-val">{{ res.p90LatencyMs | number:'1.2-3' }} ms</span>
              </div>

              <div class="percentile-row">
                <span class="pct-name">P95</span>
                <div class="pct-bar-track">
                  <div class="pct-bar-fill p95" [style.width.%]="calcBarWidth(res.p95LatencyMs, res.maxLatencyMs)"></div>
                </div>
                <span class="pct-val">{{ res.p95LatencyMs | number:'1.2-3' }} ms</span>
              </div>

              <div class="percentile-row">
                <span class="pct-name">P99</span>
                <div class="pct-bar-track">
                  <div class="pct-bar-fill p99" [style.width.%]="calcBarWidth(res.p99LatencyMs, res.maxLatencyMs)"></div>
                </div>
                <span class="pct-val">{{ res.p99LatencyMs | number:'1.2-3' }} ms</span>
              </div>

              <div class="percentile-row">
                <span class="pct-name">Max</span>
                <div class="pct-bar-track">
                  <div class="pct-bar-fill max" [style.width.%]="100"></div>
                </div>
                <span class="pct-val">{{ res.maxLatencyMs | number:'1.2-3' }} ms</span>
              </div>
            </div>
          </div>
        </div>
      }
    </mat-dialog-content>

    <div class="dialog-actions">
      @if (!isRunning() && !result()) {
        <button mat-stroked-button mat-dialog-close>Cancel</button>
        <button mat-flat-button color="primary" class="start-btn" (click)="startLoadTest()">
          <mat-icon>play_arrow</mat-icon>
          <span>Run Load Test</span>
        </button>
      }

      @if (isRunning()) {
        <button mat-flat-button color="warn" (click)="stopLoadTest()" [disabled]="isStopping()">
          <mat-icon>stop</mat-icon>
          <span>{{ isStopping() ? 'Stopping & Purging...' : 'Stop Benchmark' }}</span>
        </button>
      }

      @if (!isRunning() && result()) {
        <button mat-stroked-button (click)="reset()">
          <mat-icon>tune</mat-icon>
          <span>Configure & Run Again</span>
        </button>
        <button mat-flat-button color="primary" mat-dialog-close>
          Done
        </button>
      }
    </div>
  `,
  styles: [`
    :host {
      display: block;
      color: var(--color-text, #f8fafc);
    }

    .dialog-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 16px 24px;
      border-bottom: 1px solid var(--color-border, #1e293b);
      background: var(--color-card, #0f172a);

      .title-row {
        display: flex;
        align-items: center;
        gap: 14px;
      }

      .header-icon-box {
        width: 40px;
        height: 40px;
        border-radius: 10px;
        background: rgba(99, 102, 241, 0.15);
        border: 1px solid rgba(99, 102, 241, 0.3);
        display: flex;
        align-items: center;
        justify-content: center;

        .header-icon {
          color: var(--color-accent, #818cf8);
          font-size: 22px;
          width: 22px;
          height: 22px;
        }

        &.running {
          background: rgba(239, 68, 68, 0.15);
          border-color: rgba(239, 68, 68, 0.35);
          .header-icon { color: #f87171; }
        }
      }

      .title-text-group {
        display: flex;
        flex-direction: column;
        gap: 2px;

        .title-text {
          font-size: 1.1rem;
          font-weight: 700;
          color: var(--color-text, #f8fafc);
        }

        .title-subtext {
          font-size: 0.8rem;
          color: var(--color-text-muted, #94a3b8);
        }
      }

      .header-actions {
        display: flex;
        align-items: center;
        gap: 8px;
      }

      .header-stop-btn {
        height: 34px;
        font-size: 0.8rem;
        padding: 0 12px;
      }

      .close-btn {
        color: var(--color-text-muted, #94a3b8);
      }
    }

    .dialog-content {
      padding: 20px 24px !important;
      background: var(--color-canvas, #090d16);
      min-width: 560px;
      max-width: 700px;
      max-height: 75vh;
      overflow-y: auto;
    }

    .info-banner {
      display: flex;
      align-items: center;
      gap: 12px;
      background: rgba(16, 185, 129, 0.1);
      border: 1px solid rgba(16, 185, 129, 0.28);
      border-radius: 8px;
      padding: 10px 14px;
      margin-bottom: 16px;
      font-size: 0.82rem;
      color: #34d399;

      .info-icon {
        font-size: 20px;
        width: 20px;
        height: 20px;
        flex-shrink: 0;
      }

      code {
        background: rgba(0, 0, 0, 0.3);
        padding: 2px 6px;
        border-radius: 4px;
        font-family: monospace;
      }
    }

    .form-group {
      display: flex;
      flex-direction: column;
      gap: 8px;
      margin-bottom: 16px;

      .form-label {
        font-size: 0.82rem;
        font-weight: 600;
        color: var(--color-text-muted, #94a3b8);
        text-transform: uppercase;
        letter-spacing: 0.05em;
      }
    }

    .mode-switch-group {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 10px;

      .mode-switch-btn {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 8px;
        background: var(--color-card, #0f172a);
        border: 1px solid var(--color-border, #1e293b);
        border-radius: 8px;
        padding: 10px 14px;
        cursor: pointer;
        color: var(--color-text, #cbd5e1);
        font-weight: 600;
        font-size: 0.84rem;
        transition: all 0.15s ease;

        mat-icon {
          font-size: 18px;
          width: 18px;
          height: 18px;
        }

        &.active {
          background: rgba(99, 102, 241, 0.2);
          border-color: var(--color-accent, #6366f1);
          color: var(--color-accent, #a5b4fc);
        }
      }
    }

    .preset-pill-group {
      display: flex;
      gap: 8px;
      flex-wrap: wrap;

      .preset-pill {
        background: var(--color-card, #0f172a);
        border: 1px solid var(--color-border, #1e293b);
        color: var(--color-text, #cbd5e1);
        padding: 6px 14px;
        border-radius: 6px;
        font-size: 0.85rem;
        font-weight: 500;
        cursor: pointer;
        transition: all 0.15s ease;

        &:hover {
          background: var(--color-card-hover, #1e293b);
          border-color: var(--color-border-light, #475569);
        }

        &.active {
          background: rgba(99, 102, 241, 0.2);
          border-color: var(--color-accent, #6366f1);
          color: var(--color-accent, #a5b4fc);
          font-weight: 700;
        }
      }
    }

    .workload-grid {
      display: flex;
      flex-direction: column;
      gap: 8px;

      .workload-card {
        display: flex;
        align-items: center;
        gap: 12px;
        background: var(--color-card, #0f172a);
        border: 1px solid var(--color-border, #1e293b);
        border-radius: 8px;
        padding: 10px 14px;
        text-align: left;
        cursor: pointer;
        transition: all 0.15s ease;
        color: var(--color-text, #cbd5e1);

        mat-icon {
          color: var(--color-text-muted, #94a3b8);
          font-size: 22px;
          width: 22px;
          height: 22px;
        }

        .workload-info {
          display: flex;
          flex-direction: column;
          gap: 2px;

          .workload-title {
            font-size: 0.88rem;
            font-weight: 600;
            color: var(--color-text, #f8fafc);
          }

          .workload-desc {
            font-size: 0.76rem;
            color: var(--color-text-muted, #64748b);
          }
        }

        &.active {
          background: rgba(99, 102, 241, 0.18);
          border-color: var(--color-accent, #6366f1);
          mat-icon { color: var(--color-accent, #818cf8); }
          .workload-title { color: var(--color-accent, #e0e7ff); }
        }
      }
    }

    .target-db-note {
      font-size: 0.82rem;
      color: var(--color-text-muted, #94a3b8);
      display: flex;
      align-items: center;
      gap: 6px;
      margin-top: 4px;

      .db-chip {
        background: rgba(99, 102, 241, 0.15);
        color: var(--color-accent, #818cf8);
        padding: 2px 8px;
        border-radius: 4px;
        font-weight: 600;
        font-family: monospace;
      }
    }

    .error-banner {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 10px 14px;
      background: rgba(239, 68, 68, 0.12);
      border: 1px solid rgba(239, 68, 68, 0.3);
      border-radius: 8px;
      color: #fca5a5;
      font-size: 0.85rem;
      margin-top: 14px;
    }

    // Live Streaming State
    .running-dashboard {
      display: flex;
      flex-direction: column;
      gap: 16px;
      padding: 12px 0;

      .running-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        background: var(--color-card, #0f172a);
        border: 1px solid var(--color-border, #1e293b);
        border-radius: 8px;
        padding: 10px 16px;

        .live-pill {
          display: flex;
          align-items: center;
          gap: 8px;
          font-size: 0.82rem;
          font-weight: 700;
          color: #ef4444;
          letter-spacing: 0.05em;

          .live-dot {
            width: 10px;
            height: 10px;
            background: #ef4444;
            border-radius: 50%;
            animation: pulse 1s ease-in-out infinite;
          }
        }

        .live-meta {
          font-size: 0.8rem;
          color: var(--color-text-muted, #94a3b8);
        }
      }

      .live-kpi-grid {
        display: grid;
        grid-template-columns: repeat(4, 1fr);
        gap: 12px;

        @media (max-width: 600px) {
          grid-template-columns: repeat(2, 1fr);
        }

        .live-kpi-card {
          background: var(--color-card, #0f172a);
          border: 1px solid var(--color-border, #1e293b);
          border-radius: 10px;
          padding: 12px;
          display: flex;
          flex-direction: column;
          gap: 4px;

          &.ops { border-color: rgba(99, 102, 241, 0.4); }
          &.time { border-color: rgba(59, 130, 246, 0.4); }
          &.lat { border-color: rgba(168, 85, 247, 0.4); }
          &.succ { border-color: rgba(16, 185, 129, 0.4); }

          .live-kpi-label {
            font-size: 0.72rem;
            color: var(--color-text-muted, #94a3b8);
            text-transform: uppercase;
            font-weight: 700;
          }

          .live-kpi-val {
            font-size: 1.25rem;
            font-weight: 800;
            color: var(--color-text, #f8fafc);

            small {
              font-size: 0.72rem;
              color: var(--color-text-muted, #94a3b8);
              font-weight: 600;
            }
          }

          .live-kpi-sub {
            font-size: 0.7rem;
            color: var(--color-text-muted, #64748b);
          }
        }
      }

      .progress-bar-container {
        display: flex;
        flex-direction: column;
        gap: 6px;

        .progress-header {
          display: flex;
          justify-content: space-between;
          font-size: 0.8rem;
          font-weight: 600;
          color: var(--color-text-muted, #94a3b8);
        }
      }

      .cleanup-live-notice {
        display: flex;
        align-items: center;
        gap: 8px;
        background: rgba(16, 185, 129, 0.08);
        border: 1px solid rgba(16, 185, 129, 0.25);
        color: #10b981;
        font-size: 0.78rem;
        padding: 6px 12px;
        border-radius: 6px;

        mat-icon {
          font-size: 16px;
          width: 16px;
          height: 16px;
        }
      }
    }

    // Results state
    .results-container {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 12px;

      @media (max-width: 600px) {
        grid-template-columns: repeat(2, 1fr);
      }

      .kpi-card {
        background: var(--color-card, #0f172a);
        border: 1px solid var(--color-border, #1e293b);
        border-radius: 10px;
        padding: 12px;
        display: flex;
        flex-direction: column;
        gap: 8px;

        &.highlight {
          border-color: rgba(99, 102, 241, 0.4);
          background: rgba(99, 102, 241, 0.08);
        }

        .kpi-icon-wrap {
          width: 32px;
          height: 32px;
          border-radius: 8px;
          display: flex;
          align-items: center;
          justify-content: center;

          mat-icon {
            font-size: 18px;
            width: 18px;
            height: 18px;
          }

          &.ops { background: rgba(99, 102, 241, 0.2); color: #818cf8; }
          &.time { background: rgba(59, 130, 246, 0.2); color: #60a5fa; }
          &.avg { background: rgba(168, 85, 247, 0.2); color: #c084fc; }
          &.success { background: rgba(16, 185, 129, 0.2); color: #34d399; }
        }

        .kpi-body {
          display: flex;
          flex-direction: column;

          .kpi-label {
            font-size: 0.72rem;
            color: var(--color-text-muted, #94a3b8);
            text-transform: uppercase;
            font-weight: 700;
            letter-spacing: 0.03em;
          }

          .kpi-value {
            font-size: 1.25rem;
            font-weight: 800;
            color: var(--color-text, #f8fafc);
            line-height: 1.2;

            small {
              font-size: 0.72rem;
              font-weight: 600;
              color: var(--color-text-muted, #94a3b8);
            }
          }

          .kpi-sub {
            font-size: 0.7rem;
            color: var(--color-text-muted, #64748b);
          }
        }
      }
    }

    .cleanup-success-card {
      display: flex;
      align-items: center;
      gap: 12px;
      background: rgba(16, 185, 129, 0.12);
      border: 1px solid rgba(16, 185, 129, 0.35);
      border-radius: 8px;
      padding: 10px 14px;
      font-size: 0.82rem;

      .cleanup-icon {
        color: #10b981;
        font-size: 22px;
        width: 22px;
        height: 22px;
        flex-shrink: 0;
      }

      .cleanup-text {
        display: flex;
        flex-direction: column;
        color: #d1fae5;

        strong {
          color: #34d399;
          margin-bottom: 2px;
        }

        span {
          color: #d1fae5;
        }
      }
    }

    .percentiles-section {
      background: var(--color-card, #0f172a);
      border: 1px solid var(--color-border, #1e293b);
      border-radius: 10px;
      padding: 16px;

      .section-title {
        margin: 0 0 12px 0;
        font-size: 0.85rem;
        font-weight: 700;
        color: var(--color-text, #cbd5e1);
        text-transform: uppercase;
        letter-spacing: 0.04em;
      }
    }

    .percentile-bars-grid {
      display: flex;
      flex-direction: column;
      gap: 8px;

      .percentile-row {
        display: grid;
        grid-template-columns: 110px 1fr 90px;
        align-items: center;
        gap: 12px;
        font-size: 0.82rem;

        .pct-name {
          color: var(--color-text-muted, #94a3b8);
          font-weight: 600;
        }

        .pct-bar-track {
          height: 8px;
          background: rgba(255, 255, 255, 0.08);
          border-radius: 9999px;
          overflow: hidden;

          .pct-bar-fill {
            height: 100%;
            border-radius: 9999px;
            transition: width 0.3s ease;

            &.min { background: #34d399; }
            &.p50 { background: #60a5fa; }
            &.p90 { background: #818cf8; }
            &.p95 { background: #c084fc; }
            &.p99 { background: #f59e0b; }
            &.max { background: #f87171; }
          }
        }

        .pct-val {
          text-align: right;
          font-family: 'JetBrains Mono', 'Roboto Mono', monospace;
          font-weight: 700;
          color: var(--color-text, #f1f5f9);
        }
      }
    }

    .dialog-actions {
      display: flex;
      justify-content: flex-end;
      align-items: center;
      gap: 12px;
      padding: 14px 24px;
      border-top: 1px solid var(--color-border, #1e293b);
      background: var(--color-card, #0f172a);

      .start-btn {
        background: var(--color-accent, #6366f1);
        color: #ffffff;
        font-weight: 600;

        &:hover {
          background: var(--color-accent-hover, #4f46e5);
        }
      }
    }

    @keyframes pulse {
      0%, 100% { opacity: 0.4; transform: scale(0.9); }
      50% { opacity: 1; transform: scale(1.1); }
    }

    :host-context(.light),
    :host-context([data-theme="clean-light"]) {
      color: #0f172a;

      .dialog-header {
        background: #ffffff;
        border-bottom-color: #e2e8f0;
        .title-text { color: #0f172a; }
        .title-subtext { color: #475569; }
        .close-btn { color: #64748b; }
      }

      .dialog-content {
        background: #f8fafc;
      }

      .info-notice {
        background: #eff6ff;
        border-color: #bfdbfe;
        color: #1e40af;
        .info-icon { color: #2563eb; }
        .info-text {
          color: #1e3a8a;
          strong { color: #1e40af; }
          code { background: #dbeafe; color: #1d4ed8; }
        }
      }

      .config-section {
        .form-label { color: #334155; }
        .form-hint { color: #64748b; }
      }

      .mode-switch-group {
        background: #e2e8f0;
        .mode-switch-btn {
          color: #475569;
          &.active {
            background: #ffffff;
            color: #4f46e5;
            box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
          }
        }
      }

      .preset-pill {
        background: #ffffff;
        border-color: #cbd5e1;
        color: #334155;
        &:hover {
          border-color: #94a3b8;
          color: #0f172a;
          background: #f1f5f9;
        }
        &.active {
          background: #4f46e5;
          border-color: #4f46e5;
          color: #ffffff;
        }
      }

      .custom-number-input {
        background: #ffffff;
        border-color: #cbd5e1;
        color: #0f172a;
      }

      .kpi-card {
        background: #ffffff !important;
        border-color: #cbd5e1 !important;
        &.highlight {
          background: #eef2ff !important;
          border-color: #a5b4fc !important;
        }
        .kpi-label { color: #475569 !important; font-weight: 700 !important; }
        .kpi-value {
          color: #0f172a !important;
          small { color: #475569 !important; }
        }
        .kpi-sub { color: #475569 !important; }
      }

      .cleanup-success-card {
        background: #ecfdf5 !important;
        border-color: #86efac !important;
        .cleanup-icon { color: #059669 !important; }
        .cleanup-text {
          color: #14532d !important;
          strong { color: #15803d !important; font-weight: 700 !important; }
          span { color: #166534 !important; font-weight: 500 !important; }
        }
      }

      .percentiles-section {
        background: #ffffff !important;
        border-color: #cbd5e1 !important;
        .section-title { color: #0f172a !important; font-weight: 700 !important; }
        .pct-name { color: #334155 !important; font-weight: 600 !important; }
        .pct-bar-track { background: #e2e8f0 !important; }
        .pct-val { color: #0f172a !important; font-weight: 700 !important; }
      }

      .dialog-actions {
        background: #ffffff;
        border-top-color: #e2e8f0;
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoadTestDialogComponent {
  readonly data: LoadTestDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<LoadTestDialogComponent>);
  private readonly cliService = inject(RedisCliService);
  private readonly destroyRef = inject(DestroyRef);

  // Form options
  readonly benchmarkMode = signal<'duration' | 'count'>('duration');
  readonly durationSeconds = signal<number>(10);
  readonly operationsCount = signal<number>(1000);
  readonly concurrency = signal<number>(5);
  readonly operationType = signal<'mixed' | 'write' | 'read'>('mixed');
  readonly payloadSize = signal<number>(128);

  // Execution state
  readonly isRunning = signal<boolean>(false);
  readonly isStopping = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly liveProgress = signal<LoadTestProgress | null>(null);
  readonly result = signal<LoadTestResult | null>(null);

  private activeTestId: string | null = null;
  private abortController: AbortController | null = null;

  constructor() {
    this.destroyRef.onDestroy(() => {
      if (this.abortController) {
        this.abortController.abort();
      }
    });
  }

  startLoadTest(): void {
    this.isRunning.set(true);
    this.isStopping.set(false);
    this.errorMessage.set(null);
    this.result.set(null);
    this.liveProgress.set(null);

    const testId = 'bench_' + Date.now();
    this.activeTestId = testId;

    const isDuration = this.benchmarkMode() === 'duration';
    const request: LoadTestRequest = {
      operationsCount: isDuration ? 0 : this.operationsCount(),
      durationSeconds: isDuration ? this.durationSeconds() : 0,
      concurrency: this.concurrency(),
      operationType: this.operationType(),
      payloadSizeBytes: this.payloadSize(),
      database: this.data.database,
      keyPrefix: '__bench_'
    };

    this.abortController = this.cliService.streamLoadTest(
      this.data.connectionId,
      request,
      testId,
      (progress) => {
        this.liveProgress.set(progress);
      },
      (res) => {
        this.isRunning.set(false);
        this.isStopping.set(false);
        this.result.set(res);
        this.activeTestId = null;
        this.abortController = null;
      },
      (err) => {
        this.isRunning.set(false);
        this.isStopping.set(false);
        this.errorMessage.set(err);
        this.activeTestId = null;
        this.abortController = null;
      }
    );
  }

  stopLoadTest(): void {
    if (!this.activeTestId) return;
    this.isStopping.set(true);

    this.cliService.stopLoadTest(this.data.connectionId, this.activeTestId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          // Progress stream will receive completed event and end gracefully
        },
        error: () => {
          this.isStopping.set(false);
        }
      });
  }

  reset(): void {
    this.result.set(null);
    this.liveProgress.set(null);
    this.errorMessage.set(null);
    this.isStopping.set(false);
  }

  calcBarWidth(val: number, max: number): number {
    if (!max || max <= 0) return 10;
    return Math.max(5, Math.min(100, (val / max) * 100));
  }

  getProgressPercent(): number {
    const p = this.liveProgress();
    if (!p) return 0;

    if (this.benchmarkMode() === 'duration') {
      const dur = this.durationSeconds();
      if (!dur || dur <= 0) return 0;
      return Math.min(100, (p.elapsedSeconds / dur) * 100);
    } else {
      const total = this.operationsCount();
      if (!total || total <= 0) return 0;
      return Math.min(100, (p.completedOperations / total) * 100);
    }
  }

  formatSeconds(sec: number): string {
    const s = Math.floor(sec);
    const ms = Math.floor((sec - s) * 10);
    const m = Math.floor(s / 60);
    const remS = s % 60;
    return `${m.toString().padStart(2, '0')}:${remS.toString().padStart(2, '0')}.${ms}`;
  }
}

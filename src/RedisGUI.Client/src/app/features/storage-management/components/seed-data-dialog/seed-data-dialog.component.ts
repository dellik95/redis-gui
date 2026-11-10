import { Component, inject, signal, ChangeDetectionStrategy, DestroyRef } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RedisCliService, SeedSampleDataOptions, SeedDataResult } from '../../../../core/services/redis-cli.service';

export interface SeedDataDialogData {
  connectionId: string;
  database: number;
}

@Component({
  selector: 'app-seed-data-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    DecimalPipe
  ],
  template: `
    <div class="dialog-header">
      <div class="title-row">
        <div class="header-icon-box">
          <mat-icon class="header-icon">auto_awesome</mat-icon>
        </div>
        <div class="title-text-group">
          <span class="title-text">Seed Demo & Realistic Sample Data</span>
          <span class="title-subtext">Populate Redis with configurable keys, realistic payloads, and multi-DB distribution</span>
        </div>
      </div>
      <button mat-icon-button mat-dialog-close matTooltip="Close" class="close-btn" [disabled]="isSeeding()">
        <mat-icon>close</mat-icon>
      </button>
    </div>

    <mat-dialog-content class="dialog-content">
      @if (!isSeeding() && !result()) {
        <div class="seed-form">
          <!-- 1. Key Count Selector -->
          <div class="form-group">
            <label class="form-label">Total Keys to Generate</label>
            <div class="preset-pill-group">
              @for (count of [25, 50, 100, 250, 500, 1000]; track count) {
                <button
                  type="button"
                  class="preset-pill"
                  [class.active]="keyCount() === count"
                  (click)="keyCount.set(count)">
                  {{ count | number }}
                </button>
              }
            </div>
          </div>

          <!-- 2. Target Database Mode -->
          <div class="form-group">
            <label class="form-label">Target Databases</label>
            <div class="db-mode-tabs">
              <button
                type="button"
                class="db-tab-btn"
                [class.active]="dbMode() === 'current'"
                (click)="dbMode.set('current')">
                <mat-icon>dns</mat-icon>
                <span>Current DB only (DB {{ data.database }})</span>
              </button>
              <button
                type="button"
                class="db-tab-btn"
                [class.active]="dbMode() === 'specific'"
                (click)="dbMode.set('specific')">
                <mat-icon>checklist</mat-icon>
                <span>Specific DBs</span>
              </button>
              <button
                type="button"
                class="db-tab-btn"
                [class.active]="dbMode() === 'random'"
                (click)="dbMode.set('random')">
                <mat-icon>shuffle</mat-icon>
                <span>Random Distribution</span>
              </button>
            </div>

            @if (dbMode() === 'specific') {
              <div class="db-chip-grid">
                @for (dbNum of availableDbs; track dbNum) {
                  <button
                    type="button"
                    class="db-chip"
                    [class.selected]="isDbSelected(dbNum)"
                    (click)="toggleDbSelection(dbNum)">
                    DB {{ dbNum }}
                  </button>
                }
              </div>
            }

            @if (dbMode() === 'random') {
              <div class="random-db-row">
                <span>Distribute across</span>
                <div class="preset-pill-group mini">
                  @for (n of [2, 4, 8, 16]; track n) {
                    <button
                      type="button"
                      class="preset-pill"
                      [class.active]="randomDbCount() === n"
                      (click)="randomDbCount.set(n)">
                      {{ n }} DBs
                    </button>
                  }
                </div>
                <span>randomly</span>
              </div>
            }
          </div>

          <!-- 3. Key Naming Complexity -->
          <div class="form-group">
            <label class="form-label">Key Naming Complexity</label>
            <div class="choice-cards-grid">
              <button
                type="button"
                class="choice-card"
                [class.active]="keyComplexity() === 'simple'"
                (click)="keyComplexity.set('simple')">
                <span class="card-title">Simple Flat</span>
                <span class="card-example">demo:user:101</span>
              </button>

              <button
                type="button"
                class="choice-card"
                [class.active]="keyComplexity() === 'hierarchical'"
                (click)="keyComplexity.set('hierarchical')">
                <span class="card-title">Hierarchical Folders</span>
                <span class="card-example">demo:ecommerce:orders:1001</span>
              </button>

              <button
                type="button"
                class="choice-card"
                [class.active]="keyComplexity() === 'uuid'"
                (click)="keyComplexity.set('uuid')">
                <span class="card-title">UUID / Session Tokens</span>
                <span class="card-example">demo:sessions:f47ac10b-...</span>
              </button>

              <button
                type="button"
                class="choice-card"
                [class.active]="keyComplexity() === 'hashtag'"
                (click)="keyComplexity.set('hashtag')">
                <span class="card-title">Cluster HashTags</span>
                <span class="card-example">demo:&#123;tenant_1&#125;:profile</span>
              </button>
            </div>
          </div>

          <!-- 4. Data Payload Complexity -->
          <div class="form-group">
            <label class="form-label">Payload Complexity</label>
            <div class="choice-cards-grid three">
              <button
                type="button"
                class="choice-card"
                [class.active]="payloadComplexity() === 'simple'"
                (click)="payloadComplexity.set('simple')">
                <span class="card-title">Simple Scalars</span>
                <span class="card-example">Primitive numbers & text</span>
              </button>

              <button
                type="button"
                class="choice-card"
                [class.active]="payloadComplexity() === 'json'"
                (click)="payloadComplexity.set('json')">
                <span class="card-title">Realistic JSON</span>
                <span class="card-example">Users, orders & events</span>
              </button>

              <button
                type="button"
                class="choice-card"
                [class.active]="payloadComplexity() === 'complex'"
                (click)="payloadComplexity.set('complex')">
                <span class="card-title">Complex / Nested</span>
                <span class="card-example">Deep nested policies & ACLs</span>
              </button>
            </div>
          </div>

          <!-- 5. Data Types Multi-Select -->
          <div class="form-group">
            <label class="form-label">Data Types to Include</label>
            <div class="types-pill-group">
              @for (type of ['string', 'hash', 'list', 'set', 'zset', 'stream']; track type) {
                <button
                  type="button"
                  class="type-pill"
                  [class.selected]="isTypeSelected(type)"
                  (click)="toggleType(type)">
                  <mat-icon>{{ getTypeIcon(type) }}</mat-icon>
                  <span>{{ getTypeLabel(type) }}</span>
                </button>
              }
            </div>
          </div>

          <!-- 6. TTL Strategy -->
          <div class="form-group">
            <label class="form-label">TTL Expiration Strategy</label>
            <div class="choice-cards-grid three">
              <button
                type="button"
                class="choice-card"
                [class.active]="ttlStrategy() === 'mixed'"
                (click)="ttlStrategy.set('mixed')">
                <span class="card-title">Realistic Mixed</span>
                <span class="card-example">60% permanent, 40% expiring</span>
              </button>

              <button
                type="button"
                class="choice-card"
                [class.active]="ttlStrategy() === 'persistent'"
                (click)="ttlStrategy.set('persistent')">
                <span class="card-title">All Persistent</span>
                <span class="card-example">No expiration (TTL = -1)</span>
              </button>

              <button
                type="button"
                class="choice-card"
                [class.active]="ttlStrategy() === 'expiring'"
                (click)="ttlStrategy.set('expiring')">
                <span class="card-title">All Expiring</span>
                <span class="card-example">100% temporary keys</span>
              </button>
            </div>
          </div>

          <!-- 7. Prefix Input -->
          <div class="prefix-row">
            <label class="prefix-label">Key Prefix:</label>
            <input
              type="text"
              class="prefix-input"
              [(ngModel)]="keyPrefix"
              placeholder="demo"
            />
          </div>

          @if (errorMessage()) {
            <div class="error-banner">
              <mat-icon>error_outline</mat-icon>
              <span>{{ errorMessage() }}</span>
            </div>
          }
        </div>
      }

      <!-- Loading State during Seeding -->
      @if (isSeeding()) {
        <div class="seeding-state">
          <mat-spinner diameter="52"></mat-spinner>
          <h3>Generating & Populating Sample Data...</h3>
          <p class="seeding-desc">
            Generating {{ keyCount() }} keys across {{ getTargetDbDescription() }} with {{ payloadComplexity() }} payload complexity.
          </p>
        </div>
      }

      <!-- Result State -->
      @if (!isSeeding() && result(); as res) {
        <div class="result-summary-card">
          <div class="summary-top">
            <mat-icon class="success-icon">check_circle</mat-icon>
            <div class="summary-headings">
              <h3>Successfully Seeded Sample Data!</h3>
              <p>{{ res.summary }}</p>
            </div>
          </div>

          <div class="stats-grid">
            <div class="stat-box">
              <span class="stat-label">Total Keys Created</span>
              <span class="stat-val">{{ res.totalKeysCreated }}</span>
            </div>
            <div class="stat-box">
              <span class="stat-label">Target Databases</span>
              <span class="stat-val">
                {{ res.targetDatabases.join(', ') }}
              </span>
            </div>
          </div>

          @if (res.keyCountPerType && getObjectKeys(res.keyCountPerType).length > 0) {
            <div class="type-counts-list">
              <span class="type-counts-header">Keys Created by Data Type:</span>
              <div class="type-chips-row">
                @for (item of getObjectEntries(res.keyCountPerType); track item[0]) {
                  <span class="type-result-chip">
                    <mat-icon>{{ getTypeIcon(item[0]) }}</mat-icon>
                    <strong>{{ item[0] | uppercase }}:</strong> {{ item[1] }}
                  </span>
                }
              </div>
            </div>
          }
        </div>
      }
    </mat-dialog-content>

    <div class="dialog-actions">
      @if (!isSeeding() && !result()) {
        <button mat-stroked-button mat-dialog-close>Cancel</button>
        <button mat-flat-button color="primary" class="seed-btn" (click)="startSeed()" [disabled]="selectedTypes().length === 0">
          <mat-icon>cloud_upload</mat-icon>
          <span>Seed {{ keyCount() }} Keys</span>
        </button>
      }

      @if (!isSeeding() && result()) {
        <button mat-flat-button color="primary" mat-dialog-close [mat-dialog-close]="true">
          Close & Refresh Keys
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
        background: rgba(16, 185, 129, 0.15);
        border: 1px solid rgba(16, 185, 129, 0.3);
        display: flex;
        align-items: center;
        justify-content: center;

        .header-icon {
          color: #10b981;
          font-size: 22px;
          width: 22px;
          height: 22px;
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

      .close-btn {
        color: var(--color-text-muted, #94a3b8);
      }
    }

    .dialog-content {
      padding: 20px 24px !important;
      background: var(--color-canvas, #090d16);
      min-width: 580px;
      max-width: 720px;
      max-height: 75vh;
      overflow-y: auto;
    }

    .seed-form {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .form-group {
      display: flex;
      flex-direction: column;
      gap: 8px;

      .form-label {
        font-size: 0.8rem;
        font-weight: 700;
        color: var(--color-text-muted, #94a3b8);
        text-transform: uppercase;
        letter-spacing: 0.05em;
      }
    }

    .preset-pill-group {
      display: flex;
      gap: 8px;
      flex-wrap: wrap;

      &.mini {
        gap: 4px;
        display: inline-flex;
      }

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
          background: rgba(16, 185, 129, 0.2);
          border-color: #10b981;
          color: #34d399;
          font-weight: 700;
        }
      }
    }

    .db-mode-tabs {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 8px;

      .db-tab-btn {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 6px;
        background: var(--color-card, #0f172a);
        border: 1px solid var(--color-border, #1e293b);
        border-radius: 6px;
        padding: 8px 10px;
        cursor: pointer;
        color: var(--color-text, #cbd5e1);
        font-size: 0.78rem;
        font-weight: 600;
        transition: all 0.15s ease;

        mat-icon {
          font-size: 16px;
          width: 16px;
          height: 16px;
        }

        &.active {
          background: rgba(99, 102, 241, 0.2);
          border-color: var(--color-accent, #6366f1);
          color: var(--color-accent, #a5b4fc);
        }
      }
    }

    .db-chip-grid {
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
      margin-top: 6px;
      background: var(--color-card, #0f172a);
      border: 1px solid var(--color-border, #1e293b);
      border-radius: 8px;
      padding: 10px;

      .db-chip {
        padding: 4px 10px;
        border-radius: 4px;
        background: var(--color-canvas, #090d16);
        border: 1px solid var(--color-border, #1e293b);
        color: var(--color-text-muted, #94a3b8);
        font-size: 0.78rem;
        font-family: monospace;
        cursor: pointer;
        transition: all 0.15s ease;

        &.selected {
          background: rgba(99, 102, 241, 0.25);
          border-color: var(--color-accent, #6366f1);
          color: var(--color-accent, #818cf8);
          font-weight: 700;
        }
      }
    }

    .random-db-row {
      display: flex;
      align-items: center;
      gap: 10px;
      font-size: 0.85rem;
      color: var(--color-text-muted, #94a3b8);
      margin-top: 4px;
    }

    .choice-cards-grid {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 8px;

      &.three {
        grid-template-columns: repeat(3, 1fr);
      }

      .choice-card {
        display: flex;
        flex-direction: column;
        gap: 3px;
        background: var(--color-card, #0f172a);
        border: 1px solid var(--color-border, #1e293b);
        border-radius: 8px;
        padding: 10px 12px;
        text-align: left;
        cursor: pointer;
        transition: all 0.15s ease;

        .card-title {
          font-size: 0.84rem;
          font-weight: 600;
          color: var(--color-text, #f8fafc);
        }

        .card-example {
          font-size: 0.74rem;
          color: var(--color-text-muted, #94a3b8);
          font-family: monospace;
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
        }

        &:hover {
          background: var(--color-card-hover, #1e293b);
          border-color: var(--color-border-light, #475569);
        }

        &.active {
          background: rgba(16, 185, 129, 0.15);
          border-color: #10b981;
          .card-title { color: #34d399; }
          .card-example { color: #a7f3d0; }
        }
      }
    }

    .types-pill-group {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;

      .type-pill {
        display: flex;
        align-items: center;
        gap: 6px;
        background: var(--color-card, #0f172a);
        border: 1px solid var(--color-border, #1e293b);
        color: var(--color-text-muted, #94a3b8);
        padding: 6px 12px;
        border-radius: 6px;
        font-size: 0.8rem;
        cursor: pointer;
        transition: all 0.15s ease;

        mat-icon {
          font-size: 16px;
          width: 16px;
          height: 16px;
        }

        &.selected {
          background: rgba(99, 102, 241, 0.2);
          border-color: var(--color-accent, #6366f1);
          color: var(--color-accent, #a5b4fc);
          font-weight: 700;
        }
      }
    }

    .prefix-row {
      display: flex;
      align-items: center;
      gap: 12px;
      background: var(--color-card, #0f172a);
      border: 1px solid var(--color-border, #1e293b);
      border-radius: 8px;
      padding: 8px 14px;

      .prefix-label {
        font-size: 0.82rem;
        font-weight: 700;
        color: var(--color-text-muted, #94a3b8);
      }

      .prefix-input {
        flex: 1;
        background: var(--color-canvas, #090d16);
        border: 1px solid var(--color-border, #1e293b);
        border-radius: 4px;
        color: var(--color-text, #f8fafc);
        padding: 4px 10px;
        font-size: 0.85rem;
        font-family: monospace;

        &:focus {
          outline: none;
          border-color: var(--color-accent, #6366f1);
        }
      }
    }

    .seeding-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 48px 24px;
      text-align: center;
      gap: 16px;

      h3 {
        margin: 0;
        font-size: 1.15rem;
        font-weight: 700;
        color: var(--color-text, #f8fafc);
      }

      .seeding-desc {
        color: var(--color-text-muted, #94a3b8);
        font-size: 0.85rem;
        margin: 0;
      }
    }

    .result-summary-card {
      display: flex;
      flex-direction: column;
      gap: 16px;
      background: var(--color-card, #0f172a);
      border: 1px solid rgba(16, 185, 129, 0.3);
      border-radius: 10px;
      padding: 16px;

      .summary-top {
        display: flex;
        align-items: flex-start;
        gap: 14px;

        .success-icon {
          color: #10b981;
          font-size: 32px;
          width: 32px;
          height: 32px;
        }

        .summary-headings {
          h3 {
            margin: 0 0 4px 0;
            font-size: 1.1rem;
            color: #34d399;
          }
          p {
            margin: 0;
            font-size: 0.85rem;
            color: var(--color-text, #cbd5e1);
          }
        }
      }

      .stats-grid {
        display: grid;
        grid-template-columns: repeat(2, 1fr);
        gap: 12px;

        .stat-box {
          background: var(--color-canvas, #090d16);
          border: 1px solid var(--color-border, #1e293b);
          border-radius: 8px;
          padding: 10px;
          display: flex;
          flex-direction: column;
          gap: 2px;

          .stat-label {
            font-size: 0.72rem;
            color: var(--color-text-muted, #94a3b8);
            text-transform: uppercase;
            font-weight: 700;
          }

          .stat-val {
            font-size: 1.1rem;
            font-weight: 800;
            color: var(--color-text, #f8fafc);
          }
        }
      }

      .type-counts-list {
        display: flex;
        flex-direction: column;
        gap: 8px;

        .type-counts-header {
          font-size: 0.76rem;
          font-weight: 700;
          color: var(--color-text-muted, #94a3b8);
          text-transform: uppercase;
        }

        .type-chips-row {
          display: flex;
          flex-wrap: wrap;
          gap: 6px;

          .type-result-chip {
            display: inline-flex;
            align-items: center;
            gap: 5px;
            background: rgba(99, 102, 241, 0.15);
            border: 1px solid rgba(99, 102, 241, 0.3);
            color: var(--color-accent, #a5b4fc);
            border-radius: 4px;
            padding: 3px 8px;
            font-size: 0.78rem;

            mat-icon {
              font-size: 14px;
              width: 14px;
              height: 14px;
            }
          }
        }
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
    }

    .dialog-actions {
      display: flex;
      justify-content: flex-end;
      align-items: center;
      gap: 12px;
      padding: 14px 24px;
      border-top: 1px solid var(--color-border, #1e293b);
      background: var(--color-card, #0f172a);

      .seed-btn {
        background: #10b981;
        color: #ffffff;
        font-weight: 600;

        &:hover {
          background: #059669;
        }
      }
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

      .form-label { color: #334155; }
      .form-hint { color: #64748b; }

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

      .db-tab-btn {
        background: #ffffff;
        border-color: #cbd5e1;
        color: #334155;
        &:hover {
          border-color: #94a3b8;
          color: #0f172a;
          background: #f1f5f9;
        }
        &.active {
          background: #eef2ff;
          border-color: #4f46e5;
          color: #4f46e5;
        }
      }

      .type-checkbox-btn,
      .option-card {
        background: #ffffff;
        border-color: #cbd5e1;
        color: #334155;
        &:hover {
          border-color: #94a3b8;
          background: #f1f5f9;
        }
        &.active {
          background: #eef2ff;
          border-color: #4f46e5;
          color: #4f46e5;
        }
        .option-name { color: #0f172a; }
        .option-desc { color: #64748b; }
      }

      .result-summary-card {
        background: #ffffff;
        border-color: #cbd5e1;
        .summary-header .summary-title { color: #0f172a; }
        .metric-item .metric-label { color: #475569; }
        .metric-item .metric-value { color: #0f172a; }
      }

      .dialog-actions {
        background: #ffffff;
        border-top-color: #e2e8f0;
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SeedDataDialogComponent {
  readonly data: SeedDataDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<SeedDataDialogComponent>);
  private readonly cliService = inject(RedisCliService);
  private readonly destroyRef = inject(DestroyRef);

  readonly availableDbs = Array.from({ length: 16 }, (_, i) => i);

  // Form Signals
  readonly keyCount = signal<number>(50);
  readonly dbMode = signal<'current' | 'specific' | 'random'>('current');
  readonly selectedDbs = signal<number[]>([this.data.database]);
  readonly randomDbCount = signal<number>(4);
  readonly keyComplexity = signal<'simple' | 'hierarchical' | 'uuid' | 'hashtag'>('hierarchical');
  readonly payloadComplexity = signal<'simple' | 'json' | 'complex'>('json');
  readonly selectedTypes = signal<string[]>(['string', 'hash', 'list', 'set', 'zset', 'stream']);
  readonly ttlStrategy = signal<'mixed' | 'persistent' | 'expiring'>('mixed');
  keyPrefix: string = 'demo';

  // Status Signals
  readonly isSeeding = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly result = signal<SeedDataResult | null>(null);

  isDbSelected(db: number): boolean {
    return this.selectedDbs().includes(db);
  }

  toggleDbSelection(db: number): void {
    const list = [...this.selectedDbs()];
    const idx = list.indexOf(db);
    if (idx >= 0) {
      if (list.length > 1) {
        list.splice(idx, 1);
      }
    } else {
      list.push(db);
    }
    this.selectedDbs.set(list.sort((a, b) => a - b));
  }

  isTypeSelected(type: string): boolean {
    return this.selectedTypes().includes(type);
  }

  toggleType(type: string): void {
    const list = [...this.selectedTypes()];
    const idx = list.indexOf(type);
    if (idx >= 0) {
      if (list.length > 1) {
        list.splice(idx, 1);
      }
    } else {
      list.push(type);
    }
    this.selectedTypes.set(list);
  }

  getTypeIcon(type: string): string {
    switch (type.toLowerCase()) {
      case 'string': return 'text_fields';
      case 'hash': return 'data_object';
      case 'list': return 'format_list_numbered';
      case 'set': return 'category';
      case 'zset':
      case 'sortedset': return 'stacked_bar_chart';
      case 'stream': return 'water';
      default: return 'data_object';
    }
  }

  getTypeLabel(type: string): string {
    switch (type.toLowerCase()) {
      case 'string': return 'Strings';
      case 'hash': return 'Hashes';
      case 'list': return 'Lists';
      case 'set': return 'Sets';
      case 'zset':
      case 'sortedset': return 'Sorted Sets';
      case 'stream': return 'Streams';
      default: return type;
    }
  }

  getTargetDbDescription(): string {
    if (this.dbMode() === 'current') return `DB ${this.data.database}`;
    if (this.dbMode() === 'specific') return `DBs [${this.selectedDbs().join(', ')}]`;
    return `${this.randomDbCount()} random DBs`;
  }

  getObjectKeys(obj: Record<string, any>): string[] {
    return Object.keys(obj);
  }

  getObjectEntries(obj: Record<string, any>): [string, any][] {
    return Object.entries(obj);
  }

  startSeed(): void {
    this.isSeeding.set(true);
    this.errorMessage.set(null);
    this.result.set(null);

    let targetDbs: number[] | undefined;
    let isRandom = false;

    if (this.dbMode() === 'current') {
      targetDbs = [this.data.database];
    } else if (this.dbMode() === 'specific') {
      targetDbs = this.selectedDbs();
    } else {
      isRandom = true;
    }

    const options: SeedSampleDataOptions = {
      keyCount: this.keyCount(),
      targetDatabases: targetDbs,
      distributeRandomlyAcrossDatabases: isRandom,
      randomDbCount: isRandom ? this.randomDbCount() : undefined,
      keyComplexity: this.keyComplexity(),
      payloadComplexity: this.payloadComplexity(),
      dataTypes: this.selectedTypes(),
      ttlStrategy: this.ttlStrategy(),
      keyPrefix: this.keyPrefix.trim() || 'demo'
    };

    this.cliService.seedSampleData(this.data.connectionId, options)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.isSeeding.set(false);
          this.result.set(res);
        },
        error: (err) => {
          this.isSeeding.set(false);
          this.errorMessage.set(err.error?.message || err.message || 'Failed to seed sample data');
        }
      });
  }
}

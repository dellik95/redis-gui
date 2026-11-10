import { Component, ChangeDetectionStrategy, model, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

@Component({
  selector: 'app-string-editor',
  standalone: true,
  imports: [
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule
  ],
  template: `
    <div class="string-editor-container">
      <div class="editor-toolbar">
        <div class="stats-group">
          <span class="stat-pill">
            <mat-icon class="stat-icon">notes</mat-icon>
            {{ lineCount() }} lines
          </span>
          <span class="stat-pill">
            <mat-icon class="stat-icon">text_format</mat-icon>
            {{ charCount() }} chars
          </span>
          @if (isJson()) {
            <span class="stat-pill json-pill">
              <mat-icon class="stat-icon">code</mat-icon>
              Valid JSON
            </span>
          }
        </div>

        <div class="actions-group">
          <button
            mat-stroked-button
            type="button"
            class="action-btn"
            (click)="formatJson()"
            [disabled]="!value().trim()"
            matTooltip="Format JSON with 2 spaces indent">
            <mat-icon>format_align_left</mat-icon>
            Format JSON
          </button>
          <button
            mat-stroked-button
            type="button"
            class="action-btn"
            (click)="minifyJson()"
            [disabled]="!value().trim()"
            matTooltip="Minify JSON into a single line">
            <mat-icon>compress</mat-icon>
            Minify JSON
          </button>
          <button
            mat-icon-button
            type="button"
            (click)="copyToClipboard()"
            [disabled]="!value().trim()"
            [matTooltip]="copied() ? 'Copied!' : 'Copy to clipboard'">
            <mat-icon>{{ copied() ? 'check' : 'content_copy' }}</mat-icon>
          </button>
          <button
            mat-icon-button
            type="button"
            (click)="clear()"
            [disabled]="!value().trim()"
            matTooltip="Clear content">
            <mat-icon>clear</mat-icon>
          </button>
        </div>
      </div>

      <mat-form-field appearance="outline" class="full-width textarea-field">
        <mat-label>String Content</mat-label>
        <textarea
          matInput
          [ngModel]="value()"
          (ngModelChange)="value.set($event)"
          rows="12"
          placeholder="Enter string value (plain text, JSON, or XML)..."
          class="monospace-textarea"
          spellcheck="false">
        </textarea>
      </mat-form-field>
    </div>
  `,
  styles: [`
    .string-editor-container {
      display: flex;
      flex-direction: column;
      gap: 10px;
      width: 100%;
    }

    .editor-toolbar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      flex-wrap: wrap;
      gap: 8px;
      padding: 6px 10px;
      background: var(--color-card, #0f172a);
      border-radius: 8px;
      border: 1px solid var(--color-border, #1e293b);
    }

    .stats-group {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .stat-pill {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      font-size: 0.72rem;
      color: var(--color-text-muted, #94a3b8);
      font-weight: 500;
      background: rgba(255, 255, 255, 0.06);
      padding: 2px 8px;
      border-radius: 6px;
      border: 1px solid var(--color-border, #1e293b);

      .stat-icon {
        font-size: 13px;
        width: 13px;
        height: 13px;
      }

      &.json-pill {
        color: #10b981;
        background: rgba(16, 185, 129, 0.15);
        border-color: rgba(16, 185, 129, 0.35);
      }
    }

    .actions-group {
      display: flex;
      align-items: center;
      gap: 6px;
    }

    .action-btn {
      font-size: 0.8rem;
      height: 32px;
      line-height: 32px;
      padding: 0 10px;

      mat-icon {
        font-size: 16px;
        width: 16px;
        height: 16px;
        margin-right: 4px;
      }
    }

    .textarea-field {
      width: 100%;
    }

    .monospace-textarea {
      font-family: 'JetBrains Mono', 'Fira Code', 'Consolas', monospace;
      font-size: 0.86rem;
      line-height: 1.5;
      tab-size: 2;
      letter-spacing: 0;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StringEditorComponent {
  readonly value = model<string>('');
  readonly copied = signal(false);

  readonly charCount = computed(() => this.value()?.length ?? 0);

  readonly lineCount = computed(() => {
    const val = this.value();
    if (!val) return 0;
    return val.split('\n').length;
  });

  readonly isJson = computed(() => {
    const val = this.value()?.trim();
    if (!val || (!val.startsWith('{') && !val.startsWith('['))) return false;
    try {
      JSON.parse(val);
      return true;
    } catch {
      return false;
    }
  });

  formatJson(): void {
    const raw = this.value()?.trim();
    if (!raw) return;
    try {
      const parsed = JSON.parse(raw);
      this.value.set(JSON.stringify(parsed, null, 2));
    } catch {
      // If not valid JSON, leave as is
    }
  }

  minifyJson(): void {
    const raw = this.value()?.trim();
    if (!raw) return;
    try {
      const parsed = JSON.parse(raw);
      this.value.set(JSON.stringify(parsed));
    } catch {
      // If not valid JSON, leave as is
    }
  }

  copyToClipboard(): void {
    const val = this.value();
    if (!val) return;
    navigator.clipboard.writeText(val).then(() => {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    });
  }

  clear(): void {
    this.value.set('');
  }
}

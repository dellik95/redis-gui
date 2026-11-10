import { Component, ChangeDetectionStrategy, model, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { HashField } from '../../../../../core/value-handlers';
import { UiInputComponent, UiButtonComponent } from '../../../../../shared/ui';

export interface IndexedHashField {
  key: string;
  value: string;
  index: number;
}

@Component({
  selector: 'app-hash-editor',
  standalone: true,
  imports: [
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    UiInputComponent,
    UiButtonComponent
  ],
  template: `
    <div class="hash-editor-container">
      <div class="hash-toolbar">
        <div class="hash-info">
          <span class="count-badge">{{ fields().length }} fields</span>
          @if (searchTerm().trim()) {
            <span class="filtered-badge">{{ filteredFields().length }} matching</span>
          }
        </div>

        <div class="search-wrap">
          <ui-input
            [value]="searchTerm()"
            (valueChange)="searchTerm.set(String($event))"
            icon="search"
            [clearable]="true"
            (cleared)="searchTerm.set('')"
            placeholder="Search field or value..."
          />
        </div>
      </div>

      <!-- Add New Field Bar -->
      <div class="add-field-bar">
        <div class="new-key-field">
          <ui-input
            [value]="newKey()"
            (valueChange)="newKey.set(String($event))"
            label="Field Key"
            placeholder="e.g. username"
          />
        </div>

        <div class="new-val-field">
          <ui-input
            [value]="newVal()"
            (valueChange)="newVal.set(String($event))"
            label="Field Value (String or JSON)"
            placeholder="e.g. alex or { 'role': 'admin' }"
            (enter)="addField()"
          />
        </div>

        <ui-button
          variant="primary"
          icon="add"
          (clicked)="addField()"
          [disabled]="!newKey().trim()">
          Add Field
        </ui-button>
      </div>

      <!-- Hash Fields Table/Grid -->
      <div class="fields-list-container">
        @for (item of filteredFields(); track item.index) {
          <div class="hash-row">
            <div class="row-field-key">
              <span class="field-label">KEY</span>
              <input
                class="field-input key-input"
                [value]="item.key"
                (input)="updateKey(item.index, $any($event.target).value)"
                placeholder="Field key" />
            </div>

            <div class="row-field-val">
              <span class="field-label">VALUE</span>
              <textarea
                class="field-input val-input"
                rows="1"
                [value]="item.value"
                (input)="updateVal(item.index, $any($event.target).value)"
                placeholder="Field value (text or JSON)"></textarea>
            </div>

            <button
              mat-icon-button
              type="button"
              color="warn"
              (click)="removeField(item.index)"
              matTooltip="Remove field">
              <mat-icon>delete_outline</mat-icon>
            </button>
          </div>
        }
        @if (fields().length === 0) {
          <div class="empty-state">
            <mat-icon class="empty-icon">data_object</mat-icon>
            <p>Hash is currently empty. Use the input above to add key-value fields.</p>
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .hash-editor-container {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .hash-toolbar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 12px;
      flex-wrap: wrap;

      .hash-info {
        display: flex;
        gap: 8px;

        .count-badge, .filtered-badge {
          font-size: 12px;
          padding: 3px 8px;
          border-radius: 4px;
          font-weight: 600;
          background: rgba(255, 255, 255, 0.06);
          color: var(--color-text-muted, #94a3b8);
          border: 1px solid var(--color-border, #1e293b);
        }

        .count-badge {
          color: #f59e0b;
          background: rgba(245, 158, 11, 0.15);
          border-color: rgba(245, 158, 11, 0.35);
        }
      }

      .search-field {
        width: 240px;

        .search-icon {
          font-size: 18px;
          width: 18px;
          height: 18px;
          color: var(--color-text-muted, #94a3b8);
        }

        .clear-btn {
          width: 24px;
          height: 24px;
          line-height: 24px;

          mat-icon {
            font-size: 16px;
            width: 16px;
            height: 16px;
          }
        }
      }
    }

    .add-field-bar {
      display: flex;
      gap: 10px;
      align-items: flex-end;
      background: var(--color-card, #0f172a);
      border: 1px dashed var(--color-border-light, #334155);
      padding: 10px 14px;
      border-radius: 8px;

      .new-key-field {
        flex: 1;
      }

      .new-val-field {
        flex: 2;
      }
    }

    .fields-list-container {
      display: flex;
      flex-direction: column;
      gap: 8px;
      max-height: 420px;
      overflow-y: auto;
      padding-right: 4px;
    }

    .hash-row {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 6px 10px;
      background: var(--color-card, #0f172a);
      border: 1px solid var(--color-border, #1e293b);
      border-radius: 8px;
      transition: all 0.15s ease;

      &:hover {
        background: var(--color-card-hover, #1e293b);
        border-color: var(--color-accent, #6366f1);
      }

      .row-field-key {
        flex: 1;
        display: flex;
        align-items: center;
        gap: 6px;
      }

      .row-field-val {
        flex: 2;
        display: flex;
        align-items: center;
        gap: 6px;
      }

      .field-label {
        font-size: 10px;
        font-weight: 700;
        text-transform: uppercase;
        color: var(--mat-sys-on-surface-variant, #888);
        user-select: none;
      }

      .field-input {
        width: 100%;
        padding: 6px 8px;
        border: 1px solid var(--mat-sys-outline-variant, #ccc);
        border-radius: 4px;
        background: var(--mat-sys-surface-container, #f8f9fa);
        color: var(--mat-sys-on-surface, #212121);
        font-family: inherit;
        font-size: 13px;
        outline: none;
        resize: vertical;
        min-height: 28px;

        &:focus {
          border-color: var(--mat-sys-primary, #1976d2);
          background: var(--mat-sys-surface, #ffffff);
        }
      }

      .key-input {
        font-family: 'JetBrains Mono', monospace;
        font-weight: 600;
      }

      .val-input {
        font-family: 'JetBrains Mono', monospace;
      }
    }

    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 32px 16px;
      gap: 8px;
      color: var(--mat-sys-on-surface-variant, #666);
      text-align: center;

      .empty-icon {
        font-size: 32px;
        width: 32px;
        height: 32px;
        opacity: 0.6;
      }

      p {
        margin: 0;
        font-size: 13px;
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HashEditorComponent {
  readonly String = String;
  readonly fields = model<HashField[]>([]);
  readonly newKey = signal('');
  readonly newVal = signal('');
  readonly searchTerm = signal('');

  readonly filteredFields = computed<IndexedHashField[]>(() => {
    const list = this.fields();
    const query = this.searchTerm().trim().toLowerCase();
    const indexed: IndexedHashField[] = list.map((field, index) => ({
      key: field.key,
      value: field.value,
      index
    }));

    if (!query) return indexed;
    return indexed.filter(f =>
      f.key.toLowerCase().includes(query) || f.value.toLowerCase().includes(query)
    );
  });

  addField(): void {
    const key = this.newKey().trim();
    if (!key) return;
    const value = this.newVal();
    this.fields.update(curr => [...curr, { key, value }]);
    this.newKey.set('');
    this.newVal.set('');
  }

  updateKey(index: number, newK: string): void {
    const list = [...this.fields()];
    const item = list[index];
    if (item) {
      list[index] = { key: newK, value: item.value };
      this.fields.set(list);
    }
  }

  updateVal(index: number, newV: string): void {
    const list = [...this.fields()];
    const item = list[index];
    if (item) {
      list[index] = { key: item.key, value: newV };
      this.fields.set(list);
    }
  }

  removeField(index: number): void {
    const list = [...this.fields()];
    list.splice(index, 1);
    this.fields.set(list);
  }
}

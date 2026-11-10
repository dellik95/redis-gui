import { Component, ChangeDetectionStrategy, model, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { UiInputComponent, UiButtonComponent } from '../../../../../shared/ui';

@Component({
  selector: 'app-list-editor',
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
    <div class="list-editor-container">
      <div class="list-toolbar">
        <div class="list-info">
          <span class="count-badge">{{ items().length }} items</span>
          @if (searchTerm().trim()) {
            <span class="filtered-badge">{{ filteredIndices().length }} matching</span>
          }
        </div>

        <div class="search-wrap">
          <ui-input
            [value]="searchTerm()"
            (valueChange)="searchTerm.set(String($event))"
            icon="search"
            [clearable]="true"
            (cleared)="searchTerm.set('')"
            placeholder="Filter items..."
          />
        </div>
      </div>

      <!-- Add New Item Section -->
      <div class="add-item-bar">
        <div class="new-item-field">
          <ui-input
            [value]="newItemText()"
            (valueChange)="newItemText.set(String($event))"
            label="New element value"
            placeholder="Enter item value..."
            (enter)="appendItem()"
          />
        </div>
        <ui-button
          variant="secondary"
          icon="vertical_align_top"
          (clicked)="prependItem()"
          [disabled]="!newItemText().trim()"
          matTooltip="Add to beginning (LPUSH)">
          Prepend
        </ui-button>
        <ui-button
          variant="primary"
          icon="add"
          (clicked)="appendItem()"
          [disabled]="!newItemText().trim()"
          matTooltip="Add to end (RPUSH)">
          Append
        </ui-button>
      </div>

      <!-- Items List -->
      <div class="items-list-container">
        @for (idx of filteredIndices(); track idx) {
          <div class="list-item-row">
            <span class="item-index-badge">#{{ idx }}</span>
            <input
              class="item-inline-input"
              [value]="items()[idx]"
              (change)="updateItem(idx, $any($event.target).value)" />
            <button
              mat-icon-button
              type="button"
              color="warn"
              (click)="removeItem(idx)"
              matTooltip="Remove this element">
              <mat-icon>delete_outline</mat-icon>
            </button>
          </div>
        }
        @if (items().length === 0) {
          <div class="empty-state">
            <mat-icon class="empty-icon">format_list_bulleted</mat-icon>
            <p>List is currently empty. Use the input above to add items.</p>
          </div>
        } @else if (filteredIndices().length === 0) {
          <div class="empty-state">
            <p>No elements match "{{ searchTerm() }}"</p>
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .list-editor-container {
      display: flex;
      flex-direction: column;
      gap: 12px;
      width: 100%;
    }

    .list-toolbar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      flex-wrap: wrap;
      gap: 8px;
    }

    .list-info {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .count-badge {
      font-size: 0.75rem;
      font-weight: 600;
      color: #38bdf8;
      background: rgba(56, 189, 248, 0.15);
      border: 1px solid rgba(56, 189, 248, 0.35);
      padding: 3px 10px;
      border-radius: 9999px;
    }

    .filtered-badge {
      font-size: 0.75rem;
      font-weight: 500;
      color: var(--color-text-muted, #94a3b8);
      background: rgba(255, 255, 255, 0.06);
      border: 1px solid var(--color-border, #1e293b);
      padding: 3px 8px;
      border-radius: 9999px;
    }

    .search-field {
      width: 200px;
      font-size: 0.85rem;
      .search-icon {
        font-size: 16px;
        width: 16px;
        height: 16px;
        opacity: 0.6;
        margin-right: 4px;
      }
      .clear-btn {
        width: 24px;
        height: 24px;
        line-height: 24px;
        mat-icon { font-size: 16px; width: 16px; height: 16px; }
      }
    }

    .add-item-bar {
      display: flex;
      align-items: flex-end;
      gap: 8px;
      padding: 10px;
      background: var(--color-card, #0f172a);
      border: 1px solid var(--color-border, #1e293b);
      border-radius: 8px;
    }

    .new-item-field {
      flex: 1;
    }

    .items-list-container {
      max-height: 380px;
      overflow-y: auto;
      border: 1px solid var(--color-border, #1e293b);
      background: var(--color-canvas, #090d16);
      border-radius: 8px;
      padding: 6px;
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .list-item-row {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 6px 10px;
      border-radius: 6px;
      background: var(--color-card, #0f172a);
      border: 1px solid var(--color-border, #1e293b);
      color: var(--color-text, #f8fafc);
      transition: all 0.12s ease;

      &:hover {
        background: var(--color-card-hover, #1e293b);
        border-color: var(--color-border-light, #334155);
      }
    }

    .item-index-badge {
      font-family: monospace;
      font-size: 0.75rem;
      font-weight: 700;
      color: var(--color-text-muted, #94a3b8);
      background: rgba(255, 255, 255, 0.06);
      border: 1px solid var(--color-border, #1e293b);
      padding: 2px 6px;
      border-radius: 4px;
      min-width: 32px;
      text-align: center;
    }

    .item-inline-input {
      flex: 1;
      border: none;
      outline: none;
      background: transparent;
      font-family: 'JetBrains Mono', monospace;
      font-size: 0.88rem;
      color: var(--color-text, #f8fafc);
      padding: 4px 6px;
      border-radius: 4px;

      &:focus {
        background: #ffffff;
        box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.3);
      }
    }

    .empty-state {
      padding: 32px 16px;
      text-align: center;
      color: #94a3b8;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 8px;

      .empty-icon {
        font-size: 32px;
        width: 32px;
        height: 32px;
        opacity: 0.4;
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ListEditorComponent {
  readonly String = String;
  readonly items = model<string[]>([]);
  readonly newItemText = signal('');
  readonly searchTerm = signal('');

  readonly filteredIndices = computed(() => {
    const list = this.items();
    const query = this.searchTerm().trim().toLowerCase();
    if (!query) return list.map((_, i) => i);
    return list
      .map((item, idx) => ({ item, idx }))
      .filter(({ item }) => item.toLowerCase().includes(query))
      .map(({ idx }) => idx);
  });

  appendItem(): void {
    const text = this.newItemText().trim();
    if (!text) return;
    this.items.set([...this.items(), text]);
    this.newItemText.set('');
  }

  prependItem(): void {
    const text = this.newItemText().trim();
    if (!text) return;
    this.items.set([text, ...this.items()]);
    this.newItemText.set('');
  }

  updateItem(index: number, newVal: string): void {
    const list = [...this.items()];
    if (index >= 0 && index < list.length) {
      list[index] = newVal;
      this.items.set(list);
    }
  }

  removeItem(index: number): void {
    const list = [...this.items()];
    list.splice(index, 1);
    this.items.set(list);
  }
}

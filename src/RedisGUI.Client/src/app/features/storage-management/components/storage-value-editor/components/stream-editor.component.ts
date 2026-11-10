import { Component, ChangeDetectionStrategy, model } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTooltipModule } from '@angular/material/tooltip';
import { StreamVisualEntry } from '../../../../../core/value-handlers';
import { UiInputComponent, UiButtonComponent } from '../../../../../shared/ui';

@Component({
  selector: 'app-stream-editor',
  standalone: true,
  imports: [
    FormsModule,
    MatButtonModule,
    MatIconModule,
    MatExpansionModule,
    MatTooltipModule,
    UiInputComponent,
    UiButtonComponent
  ],
  template: `
    <div class="stream-editor-container">
      <div class="stream-toolbar">
        <div class="stream-info">
          <span class="count-badge">{{ entries().length }} entries</span>
        </div>

        <ui-button
          variant="primary"
          size="sm"
          icon="add"
          (clicked)="addEntry()">
          Add Stream Entry
        </ui-button>
      </div>

      <!-- Stream Entries Accordion -->
      <mat-accordion multi class="entries-accordion">
        @for (entry of entries(); track $index; let entryIdx = $index) {
          <mat-expansion-panel
            #panel
            [expanded]="entries().length <= 3 || entryIdx === 0"
            (opened)="onPanelOpened(panel)"
            class="entry-panel">
            <mat-expansion-panel-header>
              <mat-panel-title class="panel-title">
                <mat-icon class="stream-icon">water</mat-icon>
                <span>Entry #{{ entryIdx + 1 }}</span>
              </mat-panel-title>
              <mat-panel-description class="panel-desc">
                ID: {{ entry.id || '*' }} ({{ entry.fields.length }} fields)
              </mat-panel-description>
            </mat-expansion-panel-header>

            <div class="entry-content">
              <div class="entry-meta-row">
                <ui-input
                  [value]="entry.id"
                  (valueChange)="updateEntryId(entryIdx, String($event))"
                  label="Entry ID (* for auto-generated ID)"
                  placeholder="*"
                  hint="Use * to let Redis auto-assign milliseconds-sequence ID"
                  class="entry-id-field"
                />

                <ui-button
                  variant="danger"
                  size="sm"
                  icon="delete"
                  (clicked)="removeEntry(entryIdx)">
                  Remove Entry
                </ui-button>
              </div>

              <!-- Entry Fields -->
              <div class="entry-fields-section">
                <div class="fields-header">
                  <span class="fields-title">Fields</span>
                  <ui-button
                    variant="ghost"
                    size="sm"
                    icon="add"
                    (clicked)="addField(entryIdx)">
                    Add Field
                  </ui-button>
                </div>

                <div class="fields-grid">
                  @for (field of entry.fields; track $index; let fieldIdx = $index) {
                    <div class="field-item-row">
                      <ui-input
                        [value]="field.key"
                        (valueChange)="updateField(entryIdx, fieldIdx, String($event), field.value)"
                        placeholder="Key"
                        class="field-key"
                      />

                      <ui-input
                        [value]="field.value"
                        (valueChange)="updateField(entryIdx, fieldIdx, field.key, String($event))"
                        placeholder="Value"
                        class="field-val"
                      />

                      <button
                        mat-icon-button
                        type="button"
                        color="warn"
                        (click)="removeField(entryIdx, fieldIdx)"
                        matTooltip="Remove field">
                        <mat-icon>delete_outline</mat-icon>
                      </button>
                    </div>
                  }
                  @if (entry.fields.length === 0) {
                    <div class="no-fields-notice">
                      No fields in this entry. Click "+ Add Field" above.
                    </div>
                  }
                </div>
              </div>
            </div>
          </mat-expansion-panel>
        }

        @if (entries().length === 0) {
          <div class="empty-state">
            <mat-icon class="empty-icon">water</mat-icon>
            <p>Stream is currently empty. Click "Add Stream Entry" to create a new message entry.</p>
          </div>
        }
      </mat-accordion>
    </div>
  `,
  styles: [`
    .stream-editor-container {
      display: flex;
      flex-direction: column;
      gap: 12px;
      width: 100%;
      max-width: 100%;
      box-sizing: border-box;
      overflow-x: hidden;
    }

    .stream-toolbar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 8px;
    }

    .count-badge {
      font-size: 0.75rem;
      font-weight: 600;
      color: #4f46e5;
      background: #eef2ff;
      border: 1px solid #c7d2fe;
      padding: 3px 10px;
      border-radius: 9999px;
    }

    .entries-accordion {
      display: flex;
      flex-direction: column;
      gap: 10px;
      width: 100%;
      box-sizing: border-box;
      padding-bottom: 180px;
    }

    .entry-panel {
      border: 1px solid var(--mat-sys-outline-variant, #e2e8f0) !important;
      border-radius: 8px !important;
      box-shadow: none !important;
      width: 100%;
      box-sizing: border-box;
      overflow: hidden;

      .panel-title {
        display: flex;
        align-items: center;
        gap: 8px;
        font-weight: 600;
        font-size: 0.9rem;
      }

      .stream-icon {
        color: #6366f1;
        font-size: 18px;
        width: 18px;
        height: 18px;
      }

      .panel-desc {
        font-family: monospace;
        font-size: 0.8rem;
        color: #64748b;
      }
    }

    .entry-content {
      display: flex;
      flex-direction: column;
      gap: 12px;
      padding-top: 8px;
      width: 100%;
      box-sizing: border-box;
    }

    .entry-meta-row {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 12px;
      flex-wrap: wrap;
      width: 100%;
      box-sizing: border-box;

      ui-button {
        margin-top: 22px;
      }
    }

    .entry-id-field {
      flex: 1 1 200px;
      min-width: 160px;
    }

    .entry-fields-section {
      background: var(--color-card, #0f172a);
      border: 1px solid var(--color-border, #1e293b);
      border-radius: 6px;
      padding: 10px;
      display: flex;
      flex-direction: column;
      gap: 8px;
      width: 100%;
      box-sizing: border-box;
    }

    .fields-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .fields-title {
      font-size: 0.75rem;
      font-weight: 700;
      color: var(--color-text-muted, #94a3b8);
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .fields-grid {
      display: flex;
      flex-direction: column;
      gap: 6px;
      width: 100%;
      box-sizing: border-box;
    }

    .field-item-row {
      display: flex;
      align-items: center;
      gap: 8px;
      background: var(--color-canvas, #090d16);
      padding: 6px;
      border-radius: 6px;
      border: 1px solid var(--color-border, #1e293b);
      width: 100%;
      box-sizing: border-box;
      min-width: 0;

      .field-key {
        flex: 1 1 110px;
        min-width: 80px;
      }

      .field-val {
        flex: 2 1 150px;
        min-width: 100px;
      }
    }

    .no-fields-notice {
      padding: 12px;
      font-size: 0.82rem;
      color: #94a3b8;
      text-align: center;
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
export class StreamEditorComponent {
  readonly String = String;
  readonly entries = model<StreamVisualEntry[]>([]);

  onPanelOpened(panel: any): void {
    try {
      setTimeout(() => {
        const bodyEl = panel?._body?.nativeElement;
        const panelEl = panel?._elementRef?.nativeElement;
        const target = bodyEl || panelEl;
        if (target) {
          target.scrollIntoView({ behavior: 'smooth', block: 'end' });
        }
      }, 180);
    } catch {
      // Fallback
    }
  }

  addEntry(): void {
    const newEntry: StreamVisualEntry = {
      id: '*',
      fields: [{ key: 'field1', value: 'value1' }]
    };
    this.entries.set([...this.entries(), newEntry]);
  }

  updateEntryId(entryIndex: number, newId: string): void {
    const list = [...this.entries()];
    const item = list[entryIndex];
    if (item) {
      list[entryIndex] = { id: newId, fields: item.fields };
      this.entries.set(list);
    }
  }

  removeEntry(entryIndex: number): void {
    const list = [...this.entries()];
    list.splice(entryIndex, 1);
    this.entries.set(list);
  }

  addField(entryIndex: number): void {
    const list = [...this.entries()];
    const current = list[entryIndex];
    if (current) {
      const newFields = [...current.fields, { key: '', value: '' }];
      list[entryIndex] = { id: current.id, fields: newFields };
      this.entries.set(list);
    }
  }

  updateField(entryIndex: number, fieldIndex: number, key: string, value: string): void {
    const list = [...this.entries()];
    const current = list[entryIndex];
    if (current) {
      const newFields = [...current.fields];
      newFields[fieldIndex] = { key, value };
      list[entryIndex] = { id: current.id, fields: newFields };
      this.entries.set(list);
    }
  }

  removeField(entryIndex: number, fieldIndex: number): void {
    const list = [...this.entries()];
    const current = list[entryIndex];
    if (current) {
      const newFields = [...current.fields];
      newFields.splice(fieldIndex, 1);
      list[entryIndex] = { id: current.id, fields: newFields };
      this.entries.set(list);
    }
  }
}

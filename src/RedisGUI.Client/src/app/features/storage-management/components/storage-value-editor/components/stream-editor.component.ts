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
  templateUrl: './stream-editor.component.html',
  styleUrl: './stream-editor.component.scss',
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

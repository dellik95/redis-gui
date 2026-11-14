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
  templateUrl: './hash-editor.component.html',
  styleUrl: './hash-editor.component.scss',
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
    return indexed.filter(
      f => f.key.toLowerCase().includes(query) || f.value.toLowerCase().includes(query)
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

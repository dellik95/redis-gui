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
  templateUrl: './list-editor.component.html',
  styleUrl: './list-editor.component.scss',
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

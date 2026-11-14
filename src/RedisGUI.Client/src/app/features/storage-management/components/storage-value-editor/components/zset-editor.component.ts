import { Component, ChangeDetectionStrategy, model, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { SortedSetMember } from '../../../../../core/value-handlers';
import { UiInputComponent, UiButtonComponent } from '../../../../../shared/ui';

export interface IndexedSortedSetMember {
  member: string;
  score: number;
  index: number;
}

@Component({
  selector: 'app-zset-editor',
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
  templateUrl: './zset-editor.component.html',
  styleUrl: './zset-editor.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ZsetEditorComponent {
  readonly String = String;
  readonly members = model<SortedSetMember[]>([]);
  readonly newMember = signal('');
  readonly newScore = signal<number | string>(0);
  readonly searchTerm = signal('');
  readonly sortOrder = signal<'asc' | 'desc'>('asc');

  readonly filteredMembers = computed<IndexedSortedSetMember[]>(() => {
    const list = this.members();
    const query = this.searchTerm().trim().toLowerCase();
    const indexed: IndexedSortedSetMember[] = list.map((item, index) => ({
      member: item.member,
      score: item.score,
      index
    }));

    if (!query) return indexed;
    return indexed.filter(item => item.member.toLowerCase().includes(query));
  });

  addMember(): void {
    const member = this.newMember().trim();
    if (!member) return;
    const score = Number(this.newScore()) || 0;

    const existingIdx = this.members().findIndex(m => m.member === member);
    if (existingIdx >= 0) {
      const updated = [...this.members()];
      updated[existingIdx] = { member, score };
      this.members.set(updated);
    } else {
      this.members.set([...this.members(), { member, score }]);
    }
    this.newMember.set('');
    this.newScore.set(0);
  }

  updateScore(index: number, newScore: number): void {
    const list = [...this.members()];
    const item = list[index];
    if (item) {
      list[index] = { member: item.member, score: newScore };
      this.members.set(list);
    }
  }

  removeMember(index: number): void {
    const list = [...this.members()];
    list.splice(index, 1);
    this.members.set(list);
  }

  toggleSort(): void {
    const current = this.sortOrder();
    const next = current === 'asc' ? 'desc' : 'asc';
    this.sortOrder.set(next);

    const sorted = [...this.members()].sort((a, b) =>
      next === 'asc' ? a.score - b.score : b.score - a.score
    );
    this.members.set(sorted);
  }
}

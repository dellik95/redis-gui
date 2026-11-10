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
  template: `
    <div class="zset-editor-container">
      <div class="zset-toolbar">
        <div class="zset-info">
          <span class="count-badge">{{ members().length }} members</span>
          @if (searchTerm().trim()) {
            <span class="filtered-badge">{{ filteredMembers().length }} matching</span>
          }
        </div>

        <div class="toolbar-actions">
          <button
            mat-stroked-button
            type="button"
            class="sort-btn"
            (click)="toggleSort()"
            matTooltip="Sort members by score">
            <mat-icon>{{ sortOrder() === 'asc' ? 'arrow_upward' : 'arrow_downward' }}</mat-icon>
            Sort {{ sortOrder() === 'asc' ? 'Ascending' : 'Descending' }}
          </button>

          <div class="search-wrap">
            <ui-input
              [value]="searchTerm()"
              (valueChange)="searchTerm.set(String($event))"
              icon="search"
              [clearable]="true"
              (cleared)="searchTerm.set('')"
              placeholder="Search member..."
            />
          </div>
        </div>
      </div>

      <!-- Add Member & Score Bar -->
      <div class="add-member-bar">
        <div class="new-member-field">
          <ui-input
            [value]="newMember()"
            (valueChange)="newMember.set(String($event))"
            label="Member Name"
            placeholder="e.g. player_1"
          />
        </div>

        <div class="new-score-field">
          <ui-input
            type="number"
            [value]="newScore()"
            (valueChange)="newScore.set($any($event))"
            label="Score (numeric)"
            placeholder="0"
            (enter)="addMember()"
          />
        </div>

        <ui-button
          variant="primary"
          icon="add"
          (clicked)="addMember()"
          [disabled]="!newMember().trim()">
          Add Member
        </ui-button>
      </div>

      <!-- Members Table -->
      <div class="members-table-container">
        @for (item of filteredMembers(); track item.index) {
          <div class="zset-row">
            <div class="row-member">
              <span class="field-label">MEMBER</span>
              <span class="member-name">{{ item.member }}</span>
            </div>

            <div class="row-score">
              <span class="field-label">SCORE</span>
              <input
                type="number"
                class="score-input"
                [value]="item.score"
                (change)="updateScore(item.index, +$any($event.target).value)" />
            </div>

            <button
              mat-icon-button
              type="button"
              color="warn"
              (click)="removeMember(item.index)"
              matTooltip="Remove member">
              <mat-icon>delete_outline</mat-icon>
            </button>
          </div>
        }
        @if (members().length === 0) {
          <div class="empty-state">
            <mat-icon class="empty-icon">stacked_bar_chart</mat-icon>
            <p>Sorted Set is currently empty. Add members with numeric scores above.</p>
          </div>
        } @else if (filteredMembers().length === 0) {
          <div class="empty-state">
            <p>No members match "{{ searchTerm() }}"</p>
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .zset-editor-container {
      display: flex;
      flex-direction: column;
      gap: 12px;
      width: 100%;
    }

    .zset-toolbar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      flex-wrap: wrap;
      gap: 8px;
    }

    .zset-info {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .toolbar-actions {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .sort-btn {
      font-size: 0.8rem;
      height: 36px;
      line-height: 36px;
      mat-icon { font-size: 16px; width: 16px; height: 16px; margin-right: 4px; }
    }

    .count-badge {
      font-size: 0.75rem;
      font-weight: 600;
      color: #f43f5e;
      background: rgba(244, 63, 94, 0.15);
      border: 1px solid rgba(244, 63, 94, 0.35);
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
      width: 180px;
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

    .add-member-bar {
      display: flex;
      align-items: flex-end;
      gap: 8px;
      padding: 10px;
      background: var(--color-card, #0f172a);
      border: 1px solid var(--color-border, #1e293b);
      border-radius: 8px;
      flex-wrap: wrap;
    }

    .new-member-field {
      flex: 2;
      min-width: 160px;
    }

    .new-score-field {
      flex: 1;
      min-width: 110px;
    }

    .members-table-container {
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

    .zset-row {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 6px 12px;
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

    .row-member {
      flex: 2;
      display: flex;
      flex-direction: column;
      gap: 2px;
      min-width: 140px;
    }

    .row-score {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 2px;
      min-width: 100px;
    }

    .field-label {
      font-size: 0.65rem;
      font-weight: 700;
      color: #94a3b8;
      letter-spacing: 0.05em;
    }

    .member-name {
      font-family: inherit;
      font-weight: 600;
      font-size: 0.88rem;
      color: #1e293b;
      word-break: break-all;
    }

    .score-input {
      border: 1px solid #e2e8f0;
      outline: none;
      background: #f8fafc;
      font-family: monospace;
      font-size: 0.86rem;
      font-weight: 600;
      color: #0369a1;
      padding: 6px 8px;
      border-radius: 4px;
      width: 100%;
      box-sizing: border-box;

      &:focus {
        background: #ffffff;
        border-color: #3b82f6;
        box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.2);
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

    const existingIdx = this.members().findIndex((m) => m.member === member);
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

import { Component, ChangeDetectionStrategy, model, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { UiInputComponent, UiButtonComponent, UiTagComponent } from '../../../../../shared/ui';

@Component({
  selector: 'app-set-editor',
  standalone: true,
  imports: [
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatTooltipModule,
    UiInputComponent,
    UiButtonComponent,
    UiTagComponent
  ],
  template: `
    <div class="set-editor-container">
      <div class="set-toolbar">
        <div class="set-info">
          <span class="count-badge">{{ members().length }} members</span>
          @if (searchTerm().trim()) {
            <span class="filtered-badge">{{ filteredMembers().length }} matching</span>
          }
        </div>

        <div class="search-wrap">
          <ui-input
            [value]="searchTerm()"
            (valueChange)="searchTerm.set(String($event))"
            icon="search"
            [clearable]="true"
            (cleared)="searchTerm.set('')"
            placeholder="Filter members..."
          />
        </div>
      </div>

      <!-- Add Member Section -->
      <div class="add-member-bar">
        <div class="new-member-field">
          <ui-input
            [value]="newMemberText()"
            (valueChange)="newMemberText.set(String($event))"
            placeholder="Add new member..."
            (enter)="addMember()"
            [error]="isDuplicate() ? 'Member already exists in this set!' : undefined"
          />
        </div>
        <ui-button
          variant="primary"
          icon="add"
          (clicked)="addMember()"
          [disabled]="!newMemberText().trim() || isDuplicate()">
          Add Member
        </ui-button>
      </div>

      <!-- Members Chip Grid -->
      <div class="members-chip-container">
        @for (member of filteredMembers(); track member) {
          <ui-tag
            [label]="member"
            [removable]="true"
            variant="set"
            (removed)="removeMember(member)"
          />
        }
        @if (members().length === 0) {
          <div class="empty-state">
            <mat-icon class="empty-icon">category</mat-icon>
            <p>Set is currently empty. Add unique members above.</p>
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
    .set-editor-container {
      display: flex;
      flex-direction: column;
      gap: 12px;
      width: 100%;
    }

    .set-toolbar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      flex-wrap: wrap;
      gap: 8px;
    }

    .set-info {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .count-badge {
      font-size: 0.75rem;
      font-weight: 600;
      color: var(--color-accent, #a855f7);
      background: rgba(168, 85, 247, 0.15);
      border: 1px solid rgba(168, 85, 247, 0.35);
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

    .add-member-bar {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 10px;
      background: var(--color-card, #0f172a);
      border: 1px solid var(--color-border, #1e293b);
      border-radius: 8px;
    }

    .new-member-input {
      flex: 1;
    }

    .duplicate-warning {
      display: flex;
      align-items: center;
      gap: 6px;
      font-size: 0.8rem;
      color: #ef4444;
      background: rgba(239, 68, 68, 0.12);
      border: 1px solid rgba(239, 68, 68, 0.3);
      padding: 6px 12px;
      border-radius: 6px;

      .warning-icon {
        font-size: 16px;
        width: 16px;
        height: 16px;
      }
    }

    .members-chip-container {
      max-height: 380px;
      overflow-y: auto;
      border: 1px solid var(--color-border, #1e293b);
      background: var(--color-canvas, #090d16);
      border-radius: 8px;
      padding: 12px;
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      align-content: flex-start;
      min-height: 120px;
    }

    .member-chip {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 5px 10px;
      background: var(--color-card, #0f172a);
      border: 1px solid var(--color-border, #1e293b);
      border-radius: 8px;
      font-size: 0.85rem;
      font-family: 'JetBrains Mono', monospace;
      color: var(--color-text, #f8fafc);
      transition: all 0.15s ease;

      &:hover {
        background: var(--color-card-hover, #1e293b);
        border-color: var(--color-border-light, #334155);
      }
    }

    .member-text {
      word-break: break-all;
    }

    .chip-remove-btn {
      background: transparent;
      border: none;
      padding: 0;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      color: var(--color-text-muted, #94a3b8);
      border-radius: 50%;
      width: 18px;
      height: 18px;
      transition: all 0.12s;

      &:hover {
        color: #ef4444;
        background: rgba(239, 68, 68, 0.15);
      }

      mat-icon {
        font-size: 14px;
        width: 14px;
        height: 14px;
      }

      &:hover {
        color: #ef4444;
        background: rgba(239, 68, 68, 0.1);
      }
    }

    .empty-state {
      width: 100%;
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
export class SetEditorComponent {
  readonly String = String;
  readonly members = model<string[]>([]);
  readonly newMemberText = signal('');
  readonly searchTerm = signal('');

  readonly isDuplicate = computed(() => {
    const text = this.newMemberText().trim();
    if (!text) return false;
    return this.members().includes(text);
  });

  readonly filteredMembers = computed(() => {
    const list = this.members();
    const query = this.searchTerm().trim().toLowerCase();
    if (!query) return list;
    return list.filter((m) => m.toLowerCase().includes(query));
  });

  addMember(): void {
    const text = this.newMemberText().trim();
    if (!text || this.isDuplicate()) return;
    this.members.set([...this.members(), text]);
    this.newMemberText.set('');
  }

  removeMember(member: string): void {
    this.members.set(this.members().filter((m) => m !== member));
  }
}

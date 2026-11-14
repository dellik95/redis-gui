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
  templateUrl: './set-editor.component.html',
  styleUrl: './set-editor.component.scss',
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
    return list.filter(m => m.toLowerCase().includes(query));
  });

  addMember(): void {
    const text = this.newMemberText().trim();
    if (!text || this.isDuplicate()) return;
    this.members.set([...this.members(), text]);
    this.newMemberText.set('');
  }

  removeMember(member: string): void {
    this.members.set(this.members().filter(m => m !== member));
  }
}

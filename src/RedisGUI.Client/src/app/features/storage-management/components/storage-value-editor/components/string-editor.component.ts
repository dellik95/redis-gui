import { Component, ChangeDetectionStrategy, model, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

@Component({
  selector: 'app-string-editor',
  standalone: true,
  imports: [
    FormsModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule
  ],
  templateUrl: './string-editor.component.html',
  styleUrl: './string-editor.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StringEditorComponent {
  readonly value = model<string>('');
  readonly copied = signal(false);

  readonly charCount = computed(() => this.value()?.length ?? 0);

  readonly lineCount = computed(() => {
    const val = this.value();
    if (!val) return 0;
    return val.split('\n').length;
  });

  readonly isJson = computed(() => {
    const val = this.value()?.trim();
    if (!val || (!val.startsWith('{') && !val.startsWith('['))) return false;
    try {
      JSON.parse(val);
      return true;
    } catch {
      return false;
    }
  });

  formatJson(): void {
    const raw = this.value()?.trim();
    if (!raw) return;
    try {
      const parsed = JSON.parse(raw);
      this.value.set(JSON.stringify(parsed, null, 2));
    } catch {
      // If not valid JSON, leave as is
    }
  }

  minifyJson(): void {
    const raw = this.value()?.trim();
    if (!raw) return;
    try {
      const parsed = JSON.parse(raw);
      this.value.set(JSON.stringify(parsed));
    } catch {
      // If not valid JSON, leave as is
    }
  }

  copyToClipboard(): void {
    const val = this.value();
    if (!val) return;
    navigator.clipboard.writeText(val).then(() => {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    });
  }

  clear(): void {
    this.value.set('');
  }
}

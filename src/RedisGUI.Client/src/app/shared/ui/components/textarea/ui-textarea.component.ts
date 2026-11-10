import { ChangeDetectionStrategy, Component, computed, forwardRef, input, model, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { FormValueControl } from '@angular/forms/signals';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'ui-textarea',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => UiTextareaComponent),
      multi: true
    }
  ],
  template: `
    <div class="ui-textarea-wrapper" [class.full-width]="fullWidth()">
      @if (label()) {
        <label [for]="textareaId" class="ui-textarea-label">
          {{ label() }}
          @if (required()) {
            <span class="required-asterisk">*</span>
          }
        </label>
      }

      <div class="textarea-container" [class.is-focused]="isFocused()" [class.has-error]="!!computedError()" [class.is-disabled]="disabled()">
        <textarea
          [id]="textareaId"
          [rows]="rows()"
          [placeholder]="placeholder()"
          [disabled]="disabled()"
          [readonly]="readonly()"
          [value]="value()"
          (input)="onInput($event)"
          (focus)="onFocus()"
          (blur)="onBlur()"
          [class.monospace]="monospace()"
          class="native-textarea"
        ></textarea>
      </div>

      @if (computedError()) {
        <div class="textarea-error">
          <mat-icon class="error-icon">error_outline</mat-icon>
          <span>{{ computedError() }}</span>
        </div>
      } @else if (hint()) {
        <div class="textarea-hint">{{ hint() }}</div>
      }
    </div>
  `,
  styles: [`
    :host {
      display: inline-block;
      vertical-align: middle;
    }

    .ui-textarea-wrapper {
      display: flex;
      flex-direction: column;
      gap: 5px;
      font-family: inherit;

      &.full-width {
        width: 100%;
      }
    }

    .ui-textarea-label {
      font-size: 12px;
      font-weight: 600;
      color: var(--color-text-muted, #94a3b8);
      letter-spacing: 0.2px;

      .required-asterisk {
        color: #ef4444;
        margin-left: 2px;
      }
    }

    .textarea-container {
      background: var(--color-canvas-subtle, #0d1322);
      border: 1px solid var(--color-border, #1e293b);
      border-radius: 8px;
      padding: 10px 12px;
      transition: all 0.15s ease-in-out;
      box-sizing: border-box;

      &:hover:not(.is-disabled) {
        border-color: var(--color-border-light, #334155);
      }

      &.is-focused {
        border-color: var(--color-accent, #6366f1);
        box-shadow: 0 0 0 3px var(--color-accent-glow, rgba(99, 102, 241, 0.2));
      }

      &.has-error {
        border-color: #ef4444 !important;
        box-shadow: 0 0 0 2px rgba(239, 68, 68, 0.2) !important;
      }

      &.is-disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    }

    .native-textarea {
      width: 100%;
      background: transparent;
      border: none;
      outline: none;
      color: var(--color-text, #f8fafc);
      font-size: 13px;
      font-family: inherit;
      resize: vertical;
      min-height: 60px;
      line-height: 1.5;
      display: block;
      box-sizing: border-box;

      &::placeholder {
        color: var(--color-text-muted, #64748b);
      }

      &.monospace {
        font-family: 'JetBrains Mono', 'Fira Code', monospace;
        font-size: 12.5px;
      }
    }

    .textarea-error {
      display: flex;
      align-items: center;
      gap: 5px;
      font-size: 11.5px;
      color: #ef4444;

      .error-icon {
        font-size: 14px;
        width: 14px;
        height: 14px;
      }
    }

    .textarea-hint {
      font-size: 11.5px;
      color: var(--color-text-muted, #94a3b8);
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiTextareaComponent implements FormValueControl<string>, ControlValueAccessor {
  private static nextId = 0;
  readonly textareaId = `ui-textarea-${++UiTextareaComponent.nextId}`;

  readonly value = model<string>('');
  readonly label = input<string>();
  readonly placeholder = input<string>('');
  readonly rows = input<number>(4);
  readonly disabled = input<boolean>(false);
  readonly readonly = input<boolean>(false);
  readonly required = input<boolean>(false);
  readonly monospace = input<boolean>(false);
  readonly error = input<string>();
  readonly hint = input<string>();
  readonly fullWidth = input<boolean>(true);

  // Signal Forms & Control Integration
  readonly name = input<string>('');
  readonly invalid = input<boolean>(false);
  readonly touched = input<boolean>(false);
  readonly dirty = input<boolean>(false);
  readonly errors = input<readonly any[]>([]);
  readonly touch = output<void>();

  readonly blurred = output<void>();
  readonly focused = output<void>();

  readonly isFocused = model(false);

  private onChange: (val: any) => void = () => {};
  private onTouched: () => void = () => {};

  readonly computedError = computed(() => {
    if (this.error()) return this.error();
    if (this.touched() && this.invalid()) {
      const errs = this.errors();
      if (errs && errs.length > 0) {
        const first = errs[0];
        return first.message || (typeof first === 'string' ? first : 'Invalid field');
      }
    }
    return '';
  });

  onInput(event: Event): void {
    const target = event.target as HTMLTextAreaElement;
    this.value.set(target.value);
    this.onChange(target.value);
  }

  onFocus(): void {
    this.isFocused.set(true);
    this.focused.emit();
  }

  onBlur(): void {
    this.isFocused.set(false);
    this.onTouched();
    this.touch.emit();
    this.blurred.emit();
  }

  // ControlValueAccessor
  writeValue(val: any): void {
    this.value.set(val ?? '');
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState?(isDisabled: boolean): void {
    // Handled
  }
}

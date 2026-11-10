import { Component, ChangeDetectionStrategy, model, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { UiInputComponent } from '../../../../../shared/ui';

export interface TtlPreset {
  label: string;
  value: number;
  tooltip: string;
}

@Component({
  selector: 'app-ttl-selector',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatTooltipModule,
    UiInputComponent
  ],
  template: `
    <div class="ttl-selector-container">
      <div class="ttl-input-row">
        <div class="ttl-field-wrap">
          <ui-input
            [value]="ttl() ?? -1"
            (valueChange)="onTtlChange($event)"
            type="number"
            label="TTL (seconds)"
            placeholder="-1 (Persistent)"
            icon="timer"
            [hint]="expirationDescription()"
          />
        </div>

        <div class="ttl-preset-pills">
          @for (preset of presets; track preset.value) {
            <button
              type="button"
              class="preset-pill"
              [class.active]="ttl() === preset.value"
              [matTooltip]="preset.tooltip"
              (click)="applyPreset(preset.value)">
              {{ preset.label }}
            </button>
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    .ttl-selector-container {
      display: flex;
      flex-direction: column;
      gap: 6px;
      width: 100%;
    }

    .ttl-input-row {
      display: flex;
      align-items: flex-start;
      gap: 16px;
      flex-wrap: wrap;
    }

    .ttl-field-wrap {
      flex: 1;
      min-width: 200px;
    }

    .ttl-preset-pills {
      display: flex;
      align-items: center;
      gap: 6px;
      flex-wrap: wrap;
      padding-top: 22px; // Align with input container below label
    }

    .preset-pill {
      background: var(--color-canvas, #090d16);
      color: var(--color-text-muted, #94a3b8);
      border: 1px solid var(--color-border, #1e293b);
      border-radius: 9999px;
      padding: 4px 12px;
      font-size: 0.75rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.15s ease-in-out;

      &:hover:not(.active) {
        border-color: var(--color-accent, #6366f1);
        color: var(--color-text, #f8fafc);
      }

      &.active {
        background: var(--color-accent, #6366f1);
        color: #ffffff;
        border-color: var(--color-accent, #6366f1);
        box-shadow: 0 0 10px var(--color-accent-glow, rgba(99, 102, 241, 0.4));
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TtlSelectorComponent {
  readonly ttl = model<number | null>(-1);

  readonly presets: TtlPreset[] = [
    { label: 'No Expiry', value: -1, tooltip: 'Never expire (persistent key)' },
    { label: '5m', value: 300, tooltip: 'Expires in 5 minutes' },
    { label: '1h', value: 3600, tooltip: 'Expires in 1 hour' },
    { label: '1d', value: 86400, tooltip: 'Expires in 1 day' },
    { label: '7d', value: 604800, tooltip: 'Expires in 7 days' }
  ];

  readonly expirationDescription = computed(() => {
    const val = this.ttl();
    if (val === null || val === undefined || val < 0) {
      return 'Persistent (never expires)';
    }
    if (val === 0) {
      return 'Key will expire immediately!';
    }
    if (val < 60) {
      return `Expires in ${val} second${val === 1 ? '' : 's'}`;
    }
    if (val < 3600) {
      const mins = Math.floor(val / 60);
      const secs = val % 60;
      return `Expires in ~${mins}m ${secs > 0 ? secs + 's' : ''}`;
    }
    if (val < 86400) {
      const hours = (val / 3600).toFixed(1);
      return `Expires in ~${hours} hours`;
    }
    const days = (val / 86400).toFixed(1);
    return `Expires in ~${days} days`;
  });

  onTtlChange(val: string | number): void {
    const parsed = val === '' || val === null || val === undefined ? -1 : Number(val);
    this.ttl.set(isNaN(parsed) ? -1 : parsed);
  }

  applyPreset(seconds: number): void {
    this.ttl.set(seconds);
  }
}

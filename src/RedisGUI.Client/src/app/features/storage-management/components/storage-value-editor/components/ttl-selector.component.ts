import { Component, ChangeDetectionStrategy, model, computed } from '@angular/core';
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
  imports: [FormsModule, MatIconModule, MatTooltipModule, UiInputComponent],
  templateUrl: './ttl-selector.component.html',
  styleUrl: './ttl-selector.component.scss',
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

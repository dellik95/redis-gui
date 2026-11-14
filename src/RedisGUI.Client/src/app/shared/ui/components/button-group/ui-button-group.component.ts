import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

export interface UiButtonGroupOption {
  label: string;
  value: any;
  icon?: string;
  badge?: string | number;
}

@Component({
  selector: 'ui-button-group',
  standalone: true,
  imports: [MatIconModule],
  templateUrl: './ui-button-group.component.html',
  styleUrl: './ui-button-group.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiButtonGroupComponent {
  readonly value = model<any>();
  readonly options = input<UiButtonGroupOption[]>([]);
  readonly size = input<'sm' | 'md'>('md');

  select(val: any): void {
    this.value.set(val);
  }
}

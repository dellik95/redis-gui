import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';

export interface NumberPresetOption {
  readonly value: number;
  readonly label: string;
}

/**
 * Selects one numeric preset without duplicating pill-button markup in feature dialogs.
 * The component is intentionally limited to numbers: text and object selections have
 * different accessibility and comparison requirements and should use dedicated controls.
 */
@Component({
  selector: 'app-number-preset-picker',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './number-preset-picker.component.html',
  styleUrl: './number-preset-picker.component.scss'
})
export class NumberPresetPickerComponent {
  readonly label = input.required<string>();
  readonly options = input.required<readonly NumberPresetOption[]>();
  readonly value = model.required<number>();
}

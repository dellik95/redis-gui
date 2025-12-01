import { Component, input, Input, output } from '@angular/core';
import { MatButtonToggleChange, MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatIconModule } from '@angular/material/icon';

export type TogglerConfig = {
  defaultValue: string | null;
  items: Array<{
    value: string;
    icon: string;
  }>;
};

@Component({
  selector: 'app-toggler',
  imports: [MatButtonToggleModule, MatIconModule],
  templateUrl: './toggler.component.html',
  styleUrl: './toggler.component.scss'
})
export class TogglerComponent {
  public config = input.required<TogglerConfig>();
  toggleChanged = output<string>();


  onToggle(event: MatButtonToggleChange) {
    this.toggleChanged.emit(event.value);
  }
}

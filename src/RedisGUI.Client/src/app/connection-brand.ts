import { Component, input } from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { MatTooltip } from '@angular/material/tooltip';
import type { Connection } from './models/connection.type';

@Component({
  selector: 'app-connection-brand',
  imports: [MatIcon, MatTooltip],
  templateUrl: './connection-brand.html',
  styleUrl: './connection-brand.scss'
})
export class ConnectionBrand {
  readonly connection = input.required<Connection | null>();
}

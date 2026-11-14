import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { FractalAnimationComponent } from './components/fractal-animation/fractal-animation.component';

@Component({
  selector: 'app-auth',
  standalone: true,
  imports: [RouterOutlet, MatIconModule, FractalAnimationComponent],
  templateUrl: './auth.component.html',
  styleUrl: './auth.component.scss'
})
export class AuthComponent {}

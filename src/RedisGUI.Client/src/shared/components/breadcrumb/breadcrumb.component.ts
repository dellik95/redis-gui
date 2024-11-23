import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BreadcrumbService } from '../../services/breadcrumb.service';
import { Observable } from 'rxjs';
import { BreadcrumbType } from '../../types/breadcrumb.type';

@Component({
  selector: 'app-breadcrumb',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink
  ],
  templateUrl: './breadcrumb.component.html',
  styleUrl: './breadcrumb.component.scss'
})
export class BreadcrumbComponent {
  private breadcrumbService = inject(BreadcrumbService);
  breadcrumbs$: Observable<Array<BreadcrumbType>> = this.breadcrumbService.breadcrumbs$;

  constructor() {}
}

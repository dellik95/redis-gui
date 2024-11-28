import { Component, OnInit, input } from '@angular/core';
import { LoadingService } from './loading.service';
import { NavigationCancel, NavigationEnd, NavigationError, NavigationStart, RouteConfigLoadEnd, RouteConfigLoadStart, Router } from "@angular/router";
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { NgIf, AsyncPipe } from '@angular/common';

@Component({
  selector: 'loading',
  templateUrl: './loading.component.html',
  styleUrls: ['./loading.component.css'],
  imports: [NgIf, MatProgressSpinner, AsyncPipe]
})
export class LoadingComponent implements OnInit {

  readonly routing = input<boolean>(false);

  readonly detectRoutingOngoing = input<boolean>(false);

  constructor(public loadingService: LoadingService, private router: Router) {

  }

  ngOnInit() {

    if (this.detectRoutingOngoing()) {
      this.router.events.subscribe(event => {
        if (
          event instanceof NavigationStart
          || event instanceof RouteConfigLoadStart) {
          this.loadingService.loadingOn();
        } else if (
          event instanceof NavigationEnd
          || event instanceof NavigationError
          || event instanceof NavigationCancel
          || event instanceof RouteConfigLoadEnd) {
          this.loadingService.loadingOff();
        }
      });
    }
  }
}

import { Injectable, inject } from '@angular/core';
import { HttpEvent, HttpInterceptor, HttpHandler, HttpRequest, HttpErrorResponse } from '@angular/common/http';
import { EMPTY, Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import {
  MatSnackBar,
  MatSnackBarAction,
  MatSnackBarActions,
  MatSnackBarLabel,
  MatSnackBarRef,
} from '@angular/material/snack-bar';
import { NotificationsComponent } from '../components/notifications/notifications.component';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {

  private snackBar = inject(MatSnackBar);
  durationInSeconds = 5;

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        this.openSnackBar();
        return EMPTY;
      })
    );
  }

  openSnackBar() {
    this.snackBar.openFromComponent(NotificationsComponent, {
      duration: this.durationInSeconds * 1000,
    });
  }
}
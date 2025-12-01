import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { LoadingService } from "../../shared/components/loading/loading.service";

/**
 * HTTP Interceptor that shows/hides loading indicator during HTTP requests
 */
@Injectable()
export class LoaderInterceptor implements HttpInterceptor {

    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        const loadingService = inject(LoadingService);
        return loadingService.showLoaderUntilCompleted(next.handle(req));
    }
}

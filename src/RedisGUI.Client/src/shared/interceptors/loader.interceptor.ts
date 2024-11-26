import { HttpEvent, HttpHandler, HttpRequest } from "@angular/common/http";
import { inject } from "@angular/core";
import { Observable } from "rxjs";
import { LoadingService } from "../components/loading/loading.service";

export class LoaderInterceptor {

    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        var loadingService = inject(LoadingService);
        return loadingService.showLoaderUntilCompleted(next.handle(req))
    }
}
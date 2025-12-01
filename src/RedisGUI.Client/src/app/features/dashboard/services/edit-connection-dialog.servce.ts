import { inject, Injectable } from "@angular/core";
import { MatDialog } from "@angular/material/dialog";
import { Subject, takeLast } from "rxjs";
import { StorageConnectionsService } from "../../../services/api/storage-connections.service";
import { EditConnectionComponent } from "../components/edit-connection/edit-connection.component";
import { ConnectionDialogMode } from "../models/connection-dialog-mode.enum";
import { Connection as ConnectionType } from "../../../models/connection.type";

@Injectable({
    providedIn: "root"
})
export class EditConnectionDialogService {
    readonly connectionService = inject(StorageConnectionsService);
    readonly dialog = inject(MatDialog);

    // Subject to notify when connections are updated
    private connectionUpdatedSubject = new Subject<void>();
    public connectionUpdated$ = this.connectionUpdatedSubject.asObservable();


    showEditConnectionDialog(connection: ConnectionType) {
        this.openDialog('300ms', '150ms', {
            mode: ConnectionDialogMode.EditMode,
            connection
        }); 
    }

    showAddConnectionDialog() {
        this.openDialog('300ms', '150ms', {
            mode: ConnectionDialogMode.AddMode
        });
    }


    private openDialog(
        enterAnimationDuration: string,
        exitAnimationDuration: string,
        data: any): void {
        let dialogRef = this.dialog.open(EditConnectionComponent, {
            enterAnimationDuration,
            exitAnimationDuration,
            data: data
        });

        dialogRef.afterClosed().pipe(takeLast(1)).subscribe(result => {
            if (result === true) {
                // Notify subscribers that connections were updated
                this.connectionUpdatedSubject.next();
            }
        });
    }
}

import { inject, Injectable } from "@angular/core";
import { MatDialog } from "@angular/material/dialog";
import { takeLast } from "rxjs";
import { RedisConnectionsService } from "../../../shared/services/redis-connections.service";
import { EditConnectionComponent } from "../edit-connection/edit-connection.component";
import { ConnectionDialogMode } from "../types/connection-dialog-mode.enum";
import { Connection as ConnectionType } from "../../../shared/types/connection.type";

@Injectable({
    providedIn: "root"
})
export class EditConnectionDialogService {
    readonly connectionService = inject(RedisConnectionsService);
    readonly dialog = inject(MatDialog);


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
            // TODO: Add reload for table
        });
    }
}

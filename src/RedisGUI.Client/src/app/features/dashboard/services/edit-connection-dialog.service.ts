import { inject, Injectable } from "@angular/core";
import { MatDialog } from "@angular/material/dialog";
import { take } from "rxjs/operators";
import { EditConnectionComponent } from "../components/edit-connection/edit-connection.component";
import { ConnectionDialogMode } from "../models/connection-dialog-mode.enum";
import { ConnectionDialogData } from "../models/connection-dialog-data.type";
import { Connection as ConnectionType } from "../../../models/connection.type";
import { ConnectionsStore } from "../../../core/stores/connections.store";

/**
 * Service for managing Edit/Add connection dialogs
 * 
 * Features:
 * - Opens edit connection dialog
 * - Opens add connection dialog
 * - Handles dialog result and updates connections store
 * 
 * Uses ConnectionsStore for state notifications (replaces Subject pattern)
 * 
 * @example
 * ```typescript
 * private dialogService = inject(EditConnectionDialogService);
 * 
 * // Open edit dialog
 * this.dialogService.showEditConnectionDialog(connection);
 * 
 * // Open add dialog
 * this.dialogService.showAddConnectionDialog();
 * ```
 */
@Injectable({
    providedIn: "root"
})
export class EditConnectionDialogService {
    // 1. Injected dependencies
    private readonly dialog = inject(MatDialog);
    private readonly connectionsStore = inject(ConnectionsStore);

    // 2. Constants
    private readonly ENTER_ANIMATION_DURATION = '300ms';
    private readonly EXIT_ANIMATION_DURATION = '150ms';

    // 3. Public API methods
    /**
     * Opens edit connection dialog
     * 
     * @param connection - Connection to edit
     */
    showEditConnectionDialog(connection: ConnectionType): void {
        this.openDialog({
            mode: ConnectionDialogMode.EditMode,
            connection
        }); 
    }

    /**
     * Opens add new connection dialog
     */
    showAddConnectionDialog(): void {
        this.openDialog({
            mode: ConnectionDialogMode.AddMode
        });
    }

    // 4. Private helper methods
    /**
     * Opens dialog with specified configuration
     * 
     * @param data - Dialog data configuration
     */
    private openDialog(data: ConnectionDialogData): void {
        const dialogRef = this.dialog.open(EditConnectionComponent, {
            enterAnimationDuration: this.ENTER_ANIMATION_DURATION,
            exitAnimationDuration: this.EXIT_ANIMATION_DURATION,
            data
        });

        dialogRef.afterClosed()
            .pipe(take(1))
            .subscribe(result => {
                if (result === true) {
                    // Notify via store that connections were updated
                    this.connectionsStore.notifyUpdate();
                    // Refresh connections list
                    this.connectionsStore.loadConnections();
                }
            });
    }
}


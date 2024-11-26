import { Connection as ConnectionType } from "../../../shared/types/connection.type";
import { ConnectionDialogMode } from "./connection-dialog-mode.enum";

export type ConnectionDialogData = {
    mode: ConnectionDialogMode,
    connection: ConnectionType
};

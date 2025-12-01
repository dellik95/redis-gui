import { Connection as ConnectionType } from "../../../models/connection.type";
import { ConnectionDialogMode } from "./connection-dialog-mode.enum";

export type ConnectionDialogData = {
    mode: ConnectionDialogMode,
    connection: ConnectionType
};

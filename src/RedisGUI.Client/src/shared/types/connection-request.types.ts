
export type CreateConnectionRequest = {
    name: string;
    host: string;
    port: number;
    username?: string | null;
    password?: string | null;
    database: number;
};


export type CheckConnectionRequest = {
    host: string;
    port: number;
    username?: string | null;
    password?: string | null;
    database: number;
}

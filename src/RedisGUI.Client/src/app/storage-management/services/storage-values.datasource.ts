import { DataSource } from "@angular/cdk/collections";
import { BehaviorSubject, Observable, Subscription } from "rxjs";

export enum StorageValueType {
    HASH,
    LIST,
    SET,
    SORTED_SET,
    STRING,
    STREAM
};

export type StorageValue = {
    type: StorageValueType;
    key: string;
    ttl: number; 
    size: number;
}

export function randomEnum<T extends object>(anEnum: T): T[keyof T] {
    const enumValues = Object.keys(anEnum)
        .map(n => Number.parseInt(n))
        .filter(n => !Number.isNaN(n)) as unknown as T[keyof T][];

    const randomIndex = Math.floor(Math.random() * enumValues.length);
    const randomEnumValue = enumValues[randomIndex];
    return randomEnumValue;
};


export class StorageValuesDataSource extends DataSource<StorageValue> {
    data: StorageValue[] = [];

    private dataSubject = new BehaviorSubject<StorageValue[]>([]);
    subscription: Subscription | null = null;

    constructor() {
        super();
        this.reloadData();
    }

    dec2hex(dec: number) {
        return dec.toString(16).padStart(2, "0")
    }

    generateId(len: number) {
        var arr = new Uint8Array((len || 40) / 2)
        window.crypto.getRandomValues(arr)
        return Array.from(arr, this.dec2hex).join('')
    }

    connect(): Observable<StorageValue[]> {
        return this.dataSubject.asObservable();
    }

    disconnect(): void {
        this.dataSubject.complete();
    }

    reloadData() {
        this.data = Array.from(Array(150).keys()).map(i => {
            return {
                key: this.generateId(10),
                size: Math.floor(Math.random() * i),
                ttl: 0,
                type: randomEnum(StorageValueType)
            }
        });

        this.dataSubject.next(this.data);
    }
}
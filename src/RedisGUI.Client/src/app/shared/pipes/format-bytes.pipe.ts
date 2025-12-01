
import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'formatBytes'
})
export class FormatBytesPipe implements PipeTransform {

  transform(bytes: number, decimalPlaces: number = 2): string {
    if (bytes === 0) {
      return '0 Bytes';
    }

    const k = 1024; // Factor to convert units
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));

    // Convert bytes to the appropriate unit
    const convertedValue = bytes / Math.pow(k, i);

    return `${convertedValue.toFixed(decimalPlaces)} ${sizes[i]}`;
  }
}

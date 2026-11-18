/** Scale up to 60 samples into a fixed SVG viewport, with a zero baseline. */
export function chartPoints(values: number[]): string {
  if (!values.length) return '';
  const maximum = Math.max(1, ...values);
  return values.map((value, index) => {
    const x = values.length === 1 ? 0 : index * 600 / (values.length - 1);
    const y = 140 - Math.max(0, value) * 130 / maximum;
    return `${x.toFixed(1)},${y.toFixed(1)}`;
  }).join(' ');
}

import { chartPoints } from './messaging-metrics-chart';

describe('messaging metric chart', () => {
  it('scales samples against a zero baseline and handles empty and flat data', () => {
    expect(chartPoints([])).toBe('');
    expect(chartPoints([0, 5, 10])).toBe('0.0,140.0 300.0,75.0 600.0,10.0');
    expect(chartPoints([0])).toBe('0.0,140.0');
  });
});

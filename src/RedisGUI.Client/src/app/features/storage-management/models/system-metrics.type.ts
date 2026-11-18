export type SystemMetrics = {
  networkSpeedIn: number;
  networkSpeedOut: number;
  keyCount: number;
  databaseKeyCounts?: Record<number, number> | undefined;
  memoryUsage: number;
  cpuUsageSystem: number;
  cpuUsageUser: number;
  cpuUsageInPercent: number;
};

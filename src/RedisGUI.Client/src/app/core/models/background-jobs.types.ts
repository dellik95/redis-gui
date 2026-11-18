import {
  LoadTestProgress,
  LoadTestRequest,
  LoadTestResult,
  SeedDataResult,
  SeedSampleDataOptions
} from '../services/redis-cli.service';
import { ImportResult } from '../../features/storage-management/services/import.service';

export type BackgroundJobStatus =
  | 'Queued'
  | 'Running'
  | 'Cancelling'
  | 'Cancelled'
  | 'Completed'
  | 'Failed';

export type BackgroundJobKind =
  | 'bulk-keys'
  | 'redis-export'
  | 'redis-import'
  | 'load-test'
  | 'seed-data';

export interface BulkKeyJobParameters {
  operation: 'delete-pattern' | 'delete' | 'ttl';
  database: number;
  pattern?: string | undefined;
  keys?: string[] | undefined;
  keysBase64?: (string | null)[] | undefined;
  ttlSeconds?: number | null | undefined;
}

export interface BulkKeyJobProgress {
  processed: number;
  total: number | null;
  changed: number;
  unchanged: number;
  failed: number;
}

export interface BulkKeyJobResult {
  processed: number;
  total: number | null;
  changed: number;
  unchanged: number;
  failed: number;
  failedKeys: string[];
  cancelled: boolean;
  mayHaveUnconfirmedChanges: boolean;
  error: string | null;
}

export interface ExportJobParameters {
  pattern?: string | undefined;
  format: 'json' | 'csv';
  database: number;
  keys?: string[] | undefined;
  keysBase64?: (string | null)[] | undefined;
}

export interface ExportJobProgress {
  exportedCount: number;
  total?: number | null | undefined;
}

export interface ImportJobParameters {
  data: string;
  format: 'json' | 'csv';
  overwrite: boolean;
  database: number;
}

export interface ImportJobProgress {
  processed: number;
  total: number;
  imported: number;
  skipped: number;
  failed: number;
}

export type LoadTestJobParameters = LoadTestRequest;
export type LoadTestJobProgress = LoadTestProgress;
export type LoadTestJobResult = LoadTestResult;

export type SeedDataJobParameters = SeedSampleDataOptions;

export interface SeedDataJobProgress {
  created: number;
  total: number;
  requested?: number | undefined;
  processed?: number | undefined;
  persisted?: number | undefined;
  failed?: number | undefined;
}

export type SeedDataJobResult = SeedDataResult;
export type ImportJobResult = ImportResult;

export interface BackgroundJobBase<TKind extends string = string, TMetrics = unknown> {
  jobId: string;
  connectionId: string;
  kind: TKind;
  status: BackgroundJobStatus;
  revision: number;
  progressPercent: number | null;
  metrics: TMetrics | null;
  error: string | null;
  hasResult: boolean;
  updatedAt: string;
  createdAt?: string | null | undefined;
  startedAt?: string | null | undefined;
  completedAt?: string | null | undefined;
  cancellationRequested?: boolean | undefined;
  mayHaveUnconfirmedChanges?: boolean | undefined;
  displayName?: string | null | undefined;
  connectionName?: string | null | undefined;
}

export type BulkKeysBackgroundJob = BackgroundJobBase<'bulk-keys', BulkKeyJobProgress>;
export type ExportBackgroundJob = BackgroundJobBase<'redis-export', ExportJobProgress>;
export type ImportBackgroundJob = BackgroundJobBase<'redis-import', ImportJobProgress>;
export type LoadTestBackgroundJob = BackgroundJobBase<'load-test', LoadTestJobProgress>;
export type SeedDataBackgroundJob = BackgroundJobBase<'seed-data', SeedDataJobProgress>;

export type TypedBackgroundJob =
  | BulkKeysBackgroundJob
  | ExportBackgroundJob
  | ImportBackgroundJob
  | LoadTestBackgroundJob
  | SeedDataBackgroundJob;

export type BackgroundJob = BackgroundJobBase<string, unknown>;

export interface BackgroundJobKindInfo {
  kind: string;
  displayName: string;
  requiresWriteAccess: boolean;
  maxParameterCharacters: number;
}

export interface JobEvent {
  job?: BackgroundJob | undefined;
  connected: boolean;
  revoked?: boolean | undefined;
  error?: string | undefined;
}

export interface TrackedJobEntry {
  job: BackgroundJob;
  seenRevision: number;
  dismissed: boolean;
  expiredReason: string | null;
  resultLoaded: boolean;
  resultError: string | null;
  resultData: unknown | null;
  format?: 'json' | 'csv' | undefined;
  database?: number | undefined;
}

export function isJobActive(status: BackgroundJobStatus): boolean {
  return status === 'Queued' || status === 'Running' || status === 'Cancelling';
}

export function isJobTerminal(status: BackgroundJobStatus): boolean {
  return status === 'Completed' || status === 'Failed' || status === 'Cancelled';
}

export function isBulkKeysJob(job: BackgroundJob): job is BulkKeysBackgroundJob {
  return job.kind === 'bulk-keys';
}

export function isExportJob(job: BackgroundJob): job is ExportBackgroundJob {
  return job.kind === 'redis-export';
}

export function isImportJob(job: BackgroundJob): job is ImportBackgroundJob {
  return job.kind === 'redis-import';
}

export function isLoadTestJob(job: BackgroundJob): job is LoadTestBackgroundJob {
  return job.kind === 'load-test';
}

export function isSeedDataJob(job: BackgroundJob): job is SeedDataBackgroundJob {
  return job.kind === 'seed-data';
}

export function describeJobDisplayName(job: Pick<BackgroundJob, 'kind' | 'displayName'>): string {
  if (job.displayName) return job.displayName;
  switch (job.kind) {
    case 'bulk-keys':
      return 'Bulk Key Operation';
    case 'redis-export':
      return 'Redis Export';
    case 'redis-import':
      return 'Redis Import';
    case 'load-test':
      return 'Redis Load Test';
    case 'seed-data':
      return 'Seed Sample Data';
    default:
      return job.kind;
  }
}

export function describeExpiredJobReason(): string {
  return 'This background job expired or was removed after server restart/retention cleanup. Refresh the workspace before retrying.';
}

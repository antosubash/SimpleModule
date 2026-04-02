// Auto-generated from [Dto] types — do not edit
export interface ModuleJobRegistration {
  jobType: any;
}

export interface JobDetailDto {
  id: string;
  jobType: string;
  moduleName: string;
  state: any;
  progressPercentage: number;
  progressMessage: string;
  error: string;
  data: string;
  logs: JobLogEntry[];
  retryCount: number;
  createdAt: string;
  startedAt: string | null;
  completedAt: string | null;
}

export interface JobLogEntry {
  message: string;
  timestamp: string;
}

export interface JobFilter {
  state: any | null;
  jobType: string;
  page: number;
  pageSize: number;
}

export interface JobStatusDto {
  id: string;
  jobType: string;
  state: any;
  progressPercentage: number;
  progressMessage: string;
  error: string;
  createdAt: string;
  startedAt: string | null;
  completedAt: string | null;
  retryCount: number;
}

export interface JobSummaryDto {
  id: string;
  jobType: string;
  state: any;
  progressPercentage: number;
  progressMessage: string;
  createdAt: string;
  completedAt: string | null;
}

export interface RecurringJobDto {
  id: string;
  name: string;
  jobType: string;
  cronExpression: string;
  isEnabled: boolean;
  lastRunAt: string | null;
  nextRunAt: string | null;
  createdAt: string;
}


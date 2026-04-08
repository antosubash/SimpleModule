// Auto-generated from [Dto] types — do not edit
export interface DashboardStats {
  totalEntries: number;
  uniqueUsers: number;
  averageDurationMs: number;
  errorRate: number;
  bySource: Record<string, number>;
  byAction: Record<string, number>;
  byModule: Record<string, number>;
  byStatusCategory: Record<string, number>;
  byEntityType: Record<string, number>;
  topUsers: NamedCount[];
  topPaths: NamedCount[];
  timeline: TimelinePoint[];
  hourlyDistribution: NamedCount[];
}

export interface NamedCount {
  name: string;
  count: number;
}

export interface TimelinePoint {
  date: string;
  http: number;
  domain: number;
  changes: number;
}

export interface AuditEntry {
  id: number;
  correlationId: string;
  source: any;
  timestamp: string;
  userId: string;
  userName: string;
  ipAddress: string;
  userAgent: string;
  httpMethod: string;
  path: string;
  queryString: string;
  statusCode: number | null;
  durationMs: number | null;
  requestBody: string;
  module: string;
  entityType: string;
  entityId: string;
  action: any | null;
  changes: string;
  metadata: string;
}

export interface AuditExportRequest {
  format: string;
  effectiveFormat: string;
  from: string | null;
  to: string | null;
  userId: string;
  module: string;
  entityType: string;
  entityId: string;
  source: any | null;
  action: any | null;
  statusCode: number | null;
  searchText: string;
  page: number | null;
  pageSize: number | null;
  sortBy: string;
  sortDescending: boolean | null;
  effectivePage: number;
  effectivePageSize: number;
  effectiveSortBy: string;
  effectiveSortDescending: boolean;
}

export interface AuditQueryRequest {
  from: string | null;
  to: string | null;
  userId: string;
  module: string;
  entityType: string;
  entityId: string;
  source: any | null;
  action: any | null;
  statusCode: number | null;
  searchText: string;
  page: number | null;
  pageSize: number | null;
  sortBy: string;
  sortDescending: boolean | null;
  effectivePage: number;
  effectivePageSize: number;
  effectiveSortBy: string;
  effectiveSortDescending: boolean;
}

export interface AuditStats {
  totalEntries: number;
  uniqueUsers: number;
  byModule: Record<string, number>;
  byAction: Record<string, number>;
  byStatusCode: Record<string, number>;
}


// Auto-generated from [Dto] types — do not edit
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
  statusCode: any;
  durationMs: any;
  requestBody: string;
  module: string;
  entityType: string;
  entityId: string;
  action: any;
  changes: string;
  metadata: string;
}

export interface AuditExportRequest {
  format: string;
  effectiveFormat: string;
}

export interface AuditQueryRequest {
  from: any;
  to: any;
  userId: string;
  module: string;
  entityType: string;
  entityId: string;
  source: any;
  action: any;
  statusCode: any;
  searchText: string;
  page: any;
  pageSize: any;
  sortBy: string;
  sortDescending: any;
  effectivePage: number;
  effectivePageSize: number;
  effectiveSortBy: string;
  effectiveSortDescending: boolean;
}

export interface AuditStats {
  totalEntries: number;
  uniqueUsers: number;
  byModule: any;
  byAction: any;
  byStatusCode: any;
}


// Auto-generated from [Dto] types — do not edit
export interface CreateEmailTemplateRequest {
  name: string;
  slug: string;
  subject: string;
  body: string;
  isHtml: boolean;
  defaultReplyTo: string;
}

export interface EmailMessage {
  to: string;
  cc: string;
  bcc: string;
  replyTo: string;
  subject: string;
  body: string;
  isHtml: boolean;
  status: any;
  errorMessage: string;
  retryCount: number;
  templateSlug: string;
  provider: string;
  sentAt: string | null;
  id: number;
  createdAt: string;
  updatedAt: string;
  concurrencyStamp: string;
}

export interface EmailStats {
  totalSent: number;
  totalFailed: number;
  totalQueued: number;
  totalRetrying: number;
  sentLast24Hours: number;
  failedLast24Hours: number;
  failureRateLast7Days: number;
  topErrors: ErrorSummary[];
  dailyVolume: DailyCount[];
}

export interface ErrorSummary {
  errorMessage: string;
  count: number;
}

export interface DailyCount {
  date: string;
  sent: number;
  failed: number;
}

export interface EmailTemplate {
  name: string;
  slug: string;
  subject: string;
  body: string;
  isHtml: boolean;
  defaultReplyTo: string;
  id: number;
  createdAt: string;
  updatedAt: string;
  concurrencyStamp: string;
}

export interface QueryEmailMessagesRequest {
  page: number | null;
  pageSize: number | null;
  status: any | null;
  to: string;
  subject: string;
  dateFrom: string | null;
  dateTo: string | null;
  sortBy: string;
  sortDescending: boolean | null;
  effectivePage: number;
  effectivePageSize: number;
  effectiveSortBy: string;
  effectiveSortDescending: boolean;
}

export interface QueryEmailTemplatesRequest {
  page: number | null;
  pageSize: number | null;
  search: string;
  effectivePage: number;
  effectivePageSize: number;
}

export interface SendEmailRequest {
  to: string;
  cc: string;
  bcc: string;
  replyTo: string;
  subject: string;
  body: string;
  isHtml: boolean;
}

export interface UpdateEmailTemplateRequest {
  name: string;
  subject: string;
  body: string;
  isHtml: boolean;
  defaultReplyTo: string;
}


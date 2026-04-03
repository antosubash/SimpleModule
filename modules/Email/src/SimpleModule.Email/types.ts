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
  id: number;
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
  createdAt: string;
  sentAt: string | null;
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
  id: number;
  name: string;
  slug: string;
  subject: string;
  body: string;
  isHtml: boolean;
  defaultReplyTo: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface QueryEmailMessagesRequest {
  page: number;
  pageSize: number;
  status: any | null;
  to: string;
  subject: string;
  dateFrom: string | null;
  dateTo: string | null;
  sortBy: string;
  sortDescending: boolean;
}

export interface QueryEmailTemplatesRequest {
  page: number;
  pageSize: number;
  search: string;
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


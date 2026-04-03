// Auto-generated from [Dto] types — do not edit
export interface CreateEmailTemplateRequest {
  name: string;
  slug: string;
  subject: string;
  body: string;
  isHtml: boolean;
}

export interface EmailMessage {
  id: number;
  to: string;
  cc: string;
  bcc: string;
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

export interface EmailTemplate {
  id: number;
  name: string;
  slug: string;
  subject: string;
  body: string;
  isHtml: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface SendEmailRequest {
  to: string;
  cc: string;
  bcc: string;
  subject: string;
  body: string;
  isHtml: boolean;
}

export interface UpdateEmailTemplateRequest {
  name: string;
  subject: string;
  body: string;
  isHtml: boolean;
}


// Auto-generated from [Dto] types — do not edit
export interface Notification {
  userId: string;
  type: string;
  channel: string;
  title: string;
  body: string;
  dataJson: string;
  readAt: string | null;
  isRead: boolean;
  id: string;
  createdAt: string;
  updatedAt: string;
  concurrencyStamp: string;
}

export interface QueryNotificationsRequest {
  page: number | null;
  pageSize: number | null;
  unreadOnly: boolean | null;
  channel: string;
  type: string;
  effectivePage: number;
  effectivePageSize: number;
}


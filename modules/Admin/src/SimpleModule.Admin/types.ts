// Auto-generated from [Dto] types — do not edit
export type AdminPermissions = {};

export interface AuditLogEntryDto {
  id: number;
  userId: string;
  performedByUserId: string;
  performedByName: string;
  action: string;
  details: string;
  timestamp: string;
}

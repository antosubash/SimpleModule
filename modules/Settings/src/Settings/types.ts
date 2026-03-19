// Auto-generated from [Dto] types — do not edit
export interface Setting {
  key: string;
  value: string;
  scope: any;
  userId: string;
  updatedAt: string;
}

export interface SettingsFilter {
  scope: any;
  group: string;
}

export interface UpdateSettingRequest {
  key: string;
  value: string;
  scope: any;
}


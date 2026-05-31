// Auto-generated from [Dto] types — do not edit
export interface BulkSettingUpdate {
  key: string;
  scope: any;
  value: any;
}

export interface BulkUpdateSettingsRequest {
  updates: BulkSettingUpdate[];
}

export interface SettingValueDto {
  key: string;
  scope: any;
  value: any | null;
  isOverridden: boolean;
  userId: string;
  updatedAt: string | null;
}

export interface UserSettingValueDto {
  key: string;
  value: any | null;
  resolvedValue: any | null;
  isOverridden: boolean;
}

export interface UpdateSettingRequest {
  key: string;
  scope: any;
  value: any;
}

export interface CreateMenuItemFormRequest {
  parentId: any | null;
  label: string;
  url: string;
  pageRoute: string;
  icon: string;
  cssClass: string;
  openInNewTab: boolean;
  isVisible: boolean;
  isHomePage: boolean;
}

export interface UpdateMenuItemFormRequest {
  label: string;
  url: string;
  pageRoute: string;
  icon: string;
  cssClass: string;
  openInNewTab: boolean;
  isVisible: boolean;
  isHomePage: boolean;
}

export interface UpdateMySettingFormRequest {
  key: string;
  value: any;
}

export interface UpdateSettingFormRequest {
  key: string;
  value: any;
  scope: any;
}

export interface CreateMenuItemRequest {
  parentId: any | null;
  label: string;
  url: string;
  pageRoute: string;
  icon: string;
  cssClass: string;
  openInNewTab: boolean;
  isVisible: boolean;
  isHomePage: boolean;
}

export interface PublicMenuItemDto {
  id: number;
  parentId: any | null;
  label: string;
  url: string;
  pageRoute: string;
  icon: string;
  cssClass: string;
  openInNewTab: boolean;
  isVisible: boolean;
  isHomePage: boolean;
  sortOrder: number;
  children: PublicMenuItemDto[];
}

export interface ReorderMenuItemsRequest {
  items: ReorderItem[];
}

export interface ReorderItem {
  id: number;
  parentId: any | null;
  sortOrder: number;
}

export interface SettingsFilter {
  scope: any | null;
  group: string;
}

export interface UpdateMenuItemRequest {
  label: string;
  url: string;
  pageRoute: string;
  icon: string;
  cssClass: string;
  openInNewTab: boolean;
  isVisible: boolean;
  isHomePage: boolean;
}


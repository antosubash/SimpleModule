// Auto-generated from [Dto] types — do not edit
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
  value: string;
}

export interface UpdateSettingFormRequest {
  key: string;
  value: string;
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

export interface Setting {
  key: string;
  value: string;
  scope: any;
  userId: string;
  updatedAt: string;
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

export interface UpdateSettingRequest {
  key: string;
  value: string;
  scope: any;
}


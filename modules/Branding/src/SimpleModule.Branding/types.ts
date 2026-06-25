// Auto-generated from [Dto] types — do not edit
export interface BrandingDto {
  appName: string;
  logoUrl: string;
  faviconUrl: string;
  colorPrimary: string;
  colorPrimaryDark: string;
  topBar: TopBarConfig;
  footer: FooterConfig;
}

export interface BrandingEditModel {
  appName: string;
  logoFileId: string;
  logoUrl: string;
  faviconFileId: string;
  faviconUrl: string;
  colorPrimary: string;
  colorPrimaryDark: string;
  customCss: string;
  topBar: TopBarConfig;
  footer: FooterConfig;
}

export interface BrandingLink {
  label: string;
  url: string;
}

export interface FooterConfig {
  enabled: boolean;
  text: string;
  links: BrandingLink[];
  showCopyright: boolean;
}

export interface TopBarConfig {
  enabled: boolean;
  message: string;
  backgroundColor: string;
  textColor: string;
  links: BrandingLink[];
  dismissible: boolean;
}


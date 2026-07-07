export interface MenuItem {
  label: string;
  url: string;
  icon: string;
  order: number;
  section: number;
  requiresAuth: boolean;
  group: string | null;
}

export interface PublicMenuItem {
  label: string;
  url: string;
  openInNewTab: boolean;
  cssClass: string;
  children: PublicMenuItem[];
}

export interface BrandingLink {
  label: string;
  url: string;
}

export interface TopBarConfig {
  enabled: boolean;
  message: string;
  backgroundColor: string;
  textColor: string;
  links: BrandingLink[];
  dismissible: boolean;
}

export interface FooterConfig {
  enabled: boolean;
  text: string;
  links: BrandingLink[];
  showCopyright: boolean;
}

export interface BrandingProps {
  appName: string;
  logoUrl: string | null;
  faviconUrl: string | null;
  colorPrimary: string;
  colorPrimaryDark: string;
  topBar: TopBarConfig;
  footer: FooterConfig;
}

export interface SharedProps {
  auth: {
    isAuthenticated: boolean;
    userName: string | null;
    roles: string[];
  };
  menus: {
    sidebar: MenuItem[];
    adminSidebar: MenuItem[];
    userDropdown: MenuItem[];
    navbar: MenuItem[];
  };
  publicMenu: PublicMenuItem[];
  csrfToken: string;
  branding?: BrandingProps;
}

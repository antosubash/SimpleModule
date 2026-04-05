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
}

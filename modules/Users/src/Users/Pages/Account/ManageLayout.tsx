import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
  Card,
  CardContent,
  PageHeader,
} from '@simplemodule/ui';

interface ManageLayoutProps {
  activePage: string;
  children: React.ReactNode;
}

const navItems = [
  {
    href: '/Identity/Account/Manage',
    page: 'Index',
    label: 'Profile',
    icon: 'M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z',
  },
  {
    href: '/Identity/Account/Manage/Email',
    page: 'Email',
    label: 'Email',
    icon: 'M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z',
  },
  {
    href: '/Identity/Account/Manage/ChangePassword',
    page: 'ChangePassword',
    label: 'Password',
    icon: 'M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z',
  },
  {
    href: '/Identity/Account/Manage/TwoFactorAuthentication',
    page: 'TwoFactorAuthentication',
    label: 'Two-factor auth',
    icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z',
  },
  {
    href: '/Identity/Account/Manage/PersonalData',
    page: 'PersonalData',
    label: 'Personal data',
    icon: 'M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4',
  },
];

function NavLink({
  href,
  active,
  icon,
  children,
}: {
  href: string;
  active: boolean;
  icon: string;
  children: React.ReactNode;
}) {
  return (
    <a
      href={href}
      className={`flex items-center gap-3 ${active ? 'nav-link-active' : 'nav-link-inactive'}`}
    >
      <svg
        className="w-4 h-4 shrink-0"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        viewBox="0 0 24 24"
        aria-hidden="true"
      >
        <path d={icon} />
      </svg>
      {children}
    </a>
  );
}

export default function ManageLayout({ activePage, children }: ManageLayoutProps) {
  return (
    <div className="space-y-6">
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/">Home</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>Account Settings</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>

      <PageHeader
        className="mb-0"
        title="Account Settings"
        description="Manage your profile, security, and preferences"
      />

      <div className="flex flex-col sm:flex-row gap-6">
        <aside className="sm:w-56 shrink-0">
          <Card className="sm:sticky sm:top-20">
            <CardContent className="p-3">
              <nav className="space-y-1">
                {navItems.map((item) => (
                  <NavLink
                    key={item.page}
                    href={item.href}
                    active={activePage === item.page}
                    icon={item.icon}
                  >
                    {item.label}
                  </NavLink>
                ))}
              </nav>
            </CardContent>
          </Card>
        </aside>
        <div className="flex-1 min-w-0">
          <Card>
            <CardContent className="p-6 sm:p-8">{children}</CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}

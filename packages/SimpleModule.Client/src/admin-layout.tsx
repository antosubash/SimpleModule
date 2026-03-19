import { Link, usePage } from '@inertiajs/react';
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
  SidebarTrigger,
  useSidebar,
} from '@simplemodule/ui';
import type { ReactNode } from 'react';

interface AdminMenuItem {
  label: string;
  url: string;
  icon: string;
  order: number;
}

interface AdminLayoutProps {
  children: ReactNode;
}

function TopBar() {
  return (
    <header
      className="sticky top-0 z-50 flex items-center justify-between border-b border-border bg-surface-overlay px-4 py-3"
      style={{ backdropFilter: 'blur(20px)' }}
    >
      <div className="flex items-center gap-4">
        <SidebarTrigger />
        <a
          href="/"
          className="flex items-center gap-2.5 no-underline font-bold text-text group"
          style={{ fontFamily: "'Sora', sans-serif" }}
        >
          <span
            className="w-8 h-8 rounded-lg flex items-center justify-center text-white text-sm font-bold shadow-md"
            style={{
              background: 'linear-gradient(135deg, var(--color-primary), var(--color-accent))',
            }}
          >
            S
          </span>
          <span className="text-base">SimpleModule</span>
        </a>
      </div>
      <div className="flex items-center gap-3">
        <a
          href="/"
          className="text-sm text-text-muted no-underline hover:text-primary transition-colors"
        >
          &larr; Back to app
        </a>
      </div>
    </header>
  );
}

function SidebarNav() {
  const { props } = usePage<{ adminSidebarMenu?: AdminMenuItem[] }>();
  const menuItems = props.adminSidebarMenu ?? [];
  const currentPath = window.location.pathname;
  const { open } = useSidebar();

  return (
    <>
      <SidebarHeader>
        {open && (
          <span className="text-xs font-semibold text-text-muted uppercase tracking-wider">
            Administration
          </span>
        )}
      </SidebarHeader>
      <SidebarContent>
        <SidebarMenu>
          {menuItems.map((item) => {
            const isActive = currentPath.startsWith(item.url);
            return (
              <SidebarMenuItem key={item.url}>
                <Link href={item.url} className="no-underline">
                  <SidebarMenuButton active={isActive}>
                    <span
                      className="flex-shrink-0 [&>svg]:w-5 [&>svg]:h-5"
                      // icon values are trusted SVG constants from C# module source code, not user input
                      dangerouslySetInnerHTML={{ __html: item.icon }}
                    />
                    {open && <span>{item.label}</span>}
                  </SidebarMenuButton>
                </Link>
              </SidebarMenuItem>
            );
          })}
        </SidebarMenu>
      </SidebarContent>
      <SidebarFooter>
        <SidebarTrigger />
      </SidebarFooter>
    </>
  );
}

export function AdminLayout({ children }: AdminLayoutProps) {
  return (
    <div className="fixed inset-0 z-40 flex bg-surface">
      <SidebarProvider>
        <Sidebar>
          <SidebarNav />
        </Sidebar>
        <div className="flex-1 flex flex-col min-h-screen overflow-auto">
          <TopBar />
          <main className="flex-1 p-6">{children}</main>
        </div>
      </SidebarProvider>
    </div>
  );
}

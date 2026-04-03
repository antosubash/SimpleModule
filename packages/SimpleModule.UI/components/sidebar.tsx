import * as React from 'react';
import { cn } from '../lib/utils';

interface SidebarContextValue {
  open: boolean;
  setOpen: (open: boolean) => void;
  toggle: () => void;
  isMobile: boolean;
}

const SidebarContext = React.createContext<SidebarContextValue>({
  open: true,
  setOpen: () => {},
  toggle: () => {},
  isMobile: false,
});

function useSidebar() {
  return React.useContext(SidebarContext);
}

function useIsMobile(breakpoint = 768) {
  const [isMobile, setIsMobile] = React.useState(
    typeof window !== 'undefined' ? window.innerWidth < breakpoint : false,
  );
  React.useEffect(() => {
    const mq = window.matchMedia(`(max-width: ${breakpoint - 1}px)`);
    const handler = (e: MediaQueryListEvent) => setIsMobile(e.matches);
    setIsMobile(mq.matches);
    mq.addEventListener('change', handler);
    return () => mq.removeEventListener('change', handler);
  }, [breakpoint]);
  return isMobile;
}

interface SidebarProviderProps extends React.HTMLAttributes<HTMLDivElement> {
  defaultOpen?: boolean;
}

const SidebarProvider = React.forwardRef<HTMLDivElement, SidebarProviderProps>(
  ({ defaultOpen = true, className, children, ...props }, ref) => {
    const isMobile = useIsMobile();
    const [open, setOpen] = React.useState(isMobile ? false : defaultOpen);
    const toggle = React.useCallback(() => setOpen((prev) => !prev), []);

    // Close sidebar when switching to mobile
    React.useEffect(() => {
      if (isMobile) setOpen(false);
      else setOpen(defaultOpen);
    }, [isMobile, defaultOpen]);

    return (
      <SidebarContext.Provider value={{ open, setOpen, toggle, isMobile }}>
        <div ref={ref} className={cn('flex min-h-screen', className)} {...props}>
          {children}
        </div>
      </SidebarContext.Provider>
    );
  },
);
SidebarProvider.displayName = 'SidebarProvider';

const Sidebar = React.forwardRef<HTMLElement, React.HTMLAttributes<HTMLElement>>(
  ({ className, children, ...props }, ref) => {
    const { open, isMobile } = useSidebar();
    return (
      <aside
        ref={ref}
        data-state={open ? 'open' : 'closed'}
        className={cn(
          'flex h-screen flex-col border-r border-border bg-surface transition-all duration-300',
          isMobile ? 'fixed inset-y-0 left-0 z-40 w-64' : open ? 'w-64' : 'w-16',
          isMobile && !open && '-translate-x-full',
          isMobile && open && 'translate-x-0',
          className,
        )}
        {...props}
      >
        {children}
      </aside>
    );
  },
);
Sidebar.displayName = 'Sidebar';

function SidebarBackdrop() {
  const { open, setOpen, isMobile } = useSidebar();
  if (!isMobile) return null;
  return (
    <div
      className={cn(
        'fixed inset-0 z-30 bg-black/50 transition-opacity duration-300',
        open ? 'opacity-100' : 'pointer-events-none opacity-0',
      )}
      onClick={() => setOpen(false)}
      aria-hidden="true"
    />
  );
}

const SidebarHeader = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => (
    <div
      ref={ref}
      className={cn('flex items-center gap-2 px-4 py-4 border-b border-border', className)}
      {...props}
    />
  ),
);
SidebarHeader.displayName = 'SidebarHeader';

const SidebarContent = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => (
    <div ref={ref} className={cn('flex-1 overflow-y-auto px-3 py-2', className)} {...props} />
  ),
);
SidebarContent.displayName = 'SidebarContent';

const SidebarFooter = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => (
    <div ref={ref} className={cn('px-4 py-4 border-t border-border', className)} {...props} />
  ),
);
SidebarFooter.displayName = 'SidebarFooter';

const SidebarGroup = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => <div ref={ref} className={cn('py-2', className)} {...props} />,
);
SidebarGroup.displayName = 'SidebarGroup';

const SidebarGroupLabel = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => {
    const { open } = useSidebar();
    return (
      <div
        ref={ref}
        className={cn(
          'px-3 py-1.5 text-xs font-semibold text-text-muted uppercase tracking-wider transition-opacity',
          !open && 'opacity-0',
          className,
        )}
        {...props}
      />
    );
  },
);
SidebarGroupLabel.displayName = 'SidebarGroupLabel';

const SidebarMenu = React.forwardRef<HTMLUListElement, React.HTMLAttributes<HTMLUListElement>>(
  ({ className, ...props }, ref) => (
    <ul ref={ref} className={cn('flex flex-col gap-0.5', className)} {...props} />
  ),
);
SidebarMenu.displayName = 'SidebarMenu';

const SidebarMenuItem = React.forwardRef<HTMLLIElement, React.HTMLAttributes<HTMLLIElement>>(
  ({ className, ...props }, ref) => <li ref={ref} className={cn('', className)} {...props} />,
);
SidebarMenuItem.displayName = 'SidebarMenuItem';

interface SidebarMenuButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  active?: boolean;
}

const SidebarMenuButton = React.forwardRef<HTMLButtonElement, SidebarMenuButtonProps>(
  ({ className, active, children, ...props }, ref) => {
    const { open } = useSidebar();
    return (
      <button
        ref={ref}
        type="button"
        data-active={active || undefined}
        className={cn(
          'flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium no-underline transition-all duration-150 cursor-pointer border-none bg-transparent',
          'text-text-secondary hover:bg-surface-raised hover:text-text',
          'data-[active]:bg-primary-subtle data-[active]:text-primary data-[active]:font-semibold',
          !open && 'justify-center px-0',
          className,
        )}
        {...props}
      >
        {children}
      </button>
    );
  },
);
SidebarMenuButton.displayName = 'SidebarMenuButton';

const SidebarTrigger = React.forwardRef<
  HTMLButtonElement,
  React.ButtonHTMLAttributes<HTMLButtonElement>
>(({ className, ...props }, ref) => {
  const { toggle } = useSidebar();
  return (
    <button
      ref={ref}
      type="button"
      onClick={toggle}
      className={cn(
        'inline-flex items-center justify-center rounded-lg p-1.5 text-text-muted hover:text-text hover:bg-surface-raised transition-colors cursor-pointer border-none bg-transparent',
        className,
      )}
      {...props}
    >
      <svg
        className="h-5 w-5"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        viewBox="0 0 24 24"
        aria-hidden="true"
      >
        <path d="M3 12h18M3 6h18M3 18h18" />
      </svg>
      <span className="sr-only">Toggle sidebar</span>
    </button>
  );
});
SidebarTrigger.displayName = 'SidebarTrigger';

export {
  Sidebar,
  SidebarBackdrop,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
  SidebarTrigger,
  useSidebar,
};

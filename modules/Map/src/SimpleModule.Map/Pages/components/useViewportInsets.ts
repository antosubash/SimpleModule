import { useEffect, useState } from 'react';

/**
 * Measures the viewport insets (top nav + left sidebar) so the map can fill
 * the remaining space. Re-measures on window resize and sidebar transitions.
 */
export function useViewportInsets() {
  const [insets, setInsets] = useState({ top: 0, left: 0 });

  useEffect(() => {
    const measure = () => {
      const nav = document.querySelector('nav.sticky') as HTMLElement | null;
      const mobileHeader = document.querySelector('.app-mobile-header') as HTMLElement | null;
      const sidebar = document.querySelector('.app-sidebar') as HTMLElement | null;
      const sidebarRect = sidebar?.getBoundingClientRect();
      setInsets((prev) => {
        const next = {
          top: nav?.offsetHeight ?? mobileHeader?.offsetHeight ?? 0,
          left: sidebarRect && sidebarRect.right > 0 ? sidebarRect.right : 0,
        };
        return prev.top === next.top && prev.left === next.left ? prev : next;
      });
    };
    measure();
    window.addEventListener('resize', measure);
    const sidebar = document.querySelector('.app-sidebar');
    const observer = sidebar ? new ResizeObserver(measure) : null;
    if (sidebar && observer) observer.observe(sidebar);
    return () => {
      window.removeEventListener('resize', measure);
      observer?.disconnect();
    };
  }, []);

  return insets;
}

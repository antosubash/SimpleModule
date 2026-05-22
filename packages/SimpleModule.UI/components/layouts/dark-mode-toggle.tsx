import * as React from 'react';

declare global {
  interface Window {
    toggleTheme?: () => void;
  }
}

export function DarkModeToggle() {
  const [isDark, setIsDark] = React.useState<boolean>(() =>
    typeof document !== 'undefined' ? document.documentElement.classList.contains('dark') : false,
  );

  React.useEffect(() => {
    if (typeof document === 'undefined') return;
    const observer = new MutationObserver(() => {
      setIsDark(document.documentElement.classList.contains('dark'));
    });
    observer.observe(document.documentElement, {
      attributes: true,
      attributeFilter: ['class'],
    });
    return () => observer.disconnect();
  }, []);

  return (
    <button
      type="button"
      onClick={() => window.toggleTheme?.()}
      className="relative inline-flex h-7 w-7 items-center justify-center rounded-lg text-text-secondary transition-all duration-200 hover:bg-surface-raised hover:text-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-ring"
      aria-label={isDark ? 'Switch to light mode' : 'Switch to dark mode'}
      aria-pressed={isDark}
    >
      <svg
        aria-hidden="true"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth={1.8}
        strokeLinecap="round"
        strokeLinejoin="round"
        className="absolute h-4 w-4 transition-all duration-500 ease-out"
        style={{
          transform: isDark ? 'rotate(-90deg) scale(0)' : 'rotate(0) scale(1)',
          opacity: isDark ? 0 : 1,
        }}
      >
        {/* Sun */}
        <circle cx="12" cy="12" r="4" />
        <path d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41" />
      </svg>
      <svg
        aria-hidden="true"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth={1.8}
        strokeLinecap="round"
        strokeLinejoin="round"
        className="absolute h-4 w-4 transition-all duration-500 ease-out"
        style={{
          transform: isDark ? 'rotate(0) scale(1)' : 'rotate(90deg) scale(0)',
          opacity: isDark ? 1 : 0,
        }}
      >
        {/* Moon */}
        <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z" />
      </svg>
    </button>
  );
}

declare global {
  interface Window {
    toggleTheme?: () => void;
  }
}

export function DarkModeToggle() {
  return (
    <button
      type="button"
      onClick={() => window.toggleTheme?.()}
      className="p-1.5 rounded-lg text-text-muted hover:text-text hover:bg-surface-raised transition-colors"
      aria-label="Toggle theme"
    >
      <svg
        className="w-4 h-4"
        fill="none"
        stroke="currentColor"
        strokeWidth={2}
        viewBox="0 0 24 24"
      >
        <path d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z" />
      </svg>
    </button>
  );
}

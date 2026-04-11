import type * as React from 'react';
import { DarkModeToggle } from '../layouts/dark-mode-toggle';

interface ErrorPageLayoutProps {
  statusCode: number;
  icon: React.ReactNode;
  title: string;
  description: string;
}

export function ErrorPageLayout({ statusCode, icon, title, description }: ErrorPageLayoutProps) {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center bg-surface px-4">
      <div className="absolute top-4 right-4">
        <DarkModeToggle />
      </div>
      <div className="w-full max-w-md text-center">
        <p className="text-[8rem] leading-none font-bold text-text-muted/20">{statusCode}</p>
        <div className="mx-auto -mt-4 mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-danger-bg">
          {icon}
        </div>
        <h1 className="text-2xl font-semibold text-text">{title}</h1>
        <p className="mt-2 text-sm text-text-muted">{description}</p>
        <div className="mt-8 flex justify-center gap-3">
          <a
            href="/"
            className="inline-flex items-center justify-center rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
          >
            Go home
          </a>
          <button
            type="button"
            onClick={() => window.history.back()}
            className="inline-flex items-center justify-center rounded-lg border border-border bg-surface px-4 py-2 text-sm font-medium text-text hover:bg-surface-raised"
          >
            Go back
          </button>
        </div>
      </div>
    </div>
  );
}

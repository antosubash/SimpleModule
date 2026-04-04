import { router } from '@inertiajs/react';
import type * as React from 'react';
import { Component } from 'react';
import { Button } from '../button';

interface Props {
  children: React.ReactNode;
}

interface State {
  hasError: boolean;
  errorMessage: string | null;
}

export class PageErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, errorMessage: null };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, errorMessage: error.message };
  }

  componentDidCatch(error: Error) {
    console.error('[PageErrorBoundary]', error);
  }

  override render() {
    if (this.state.hasError) {
      return (
        <div className="flex min-h-[60vh] flex-col items-center justify-center px-4">
          <div className="w-full max-w-md text-center">
            <div className="mx-auto mb-6 flex h-16 w-16 items-center justify-center rounded-full bg-danger-bg">
              <svg
                className="h-8 w-8 text-danger-text"
                fill="none"
                stroke="currentColor"
                strokeWidth="1.5"
                viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z"
                />
              </svg>
            </div>

            <h2 className="text-xl font-semibold text-text">Something went wrong</h2>
            <p className="mt-2 text-sm text-text-muted">
              This page encountered an unexpected error. You can try again or go back to the home
              page.
            </p>

            {this.state.errorMessage && (
              <details className="mt-4 rounded-lg border border-border bg-surface-sunken p-3 text-left text-xs">
                <summary className="cursor-pointer font-medium text-text-muted">
                  Error details
                </summary>
                <pre className="mt-2 overflow-auto whitespace-pre-wrap text-danger-text">
                  {this.state.errorMessage}
                </pre>
              </details>
            )}

            <div className="mt-6 flex justify-center gap-3">
              <Button onClick={() => this.setState({ hasError: false, errorMessage: null })}>
                Try again
              </Button>
              <Button variant="secondary" onClick={() => router.visit('/')}>
                Go home
              </Button>
            </div>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}

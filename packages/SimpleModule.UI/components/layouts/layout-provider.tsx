import { usePage } from '@inertiajs/react';
import type * as React from 'react';
import { AppLayout } from './app-layout';
import { PageErrorBoundary } from './page-error-boundary';
import { PublicLayout } from './public-layout';
import type { SharedProps } from './types';

function AutoLayout({ children }: { children: React.ReactNode }) {
  const { props } = usePage<SharedProps>();
  const { auth } = props;

  if (auth?.isAuthenticated) {
    return (
      <AppLayout>
        <PageErrorBoundary>{children}</PageErrorBoundary>
      </AppLayout>
    );
  }
  return (
    <PublicLayout>
      <PageErrorBoundary>{children}</PageErrorBoundary>
    </PublicLayout>
  );
}

export function resolveLayout(page: any) {
  if (page.default?.layout) return page;
  page.default.layout = (pageContent: React.ReactNode) => <AutoLayout>{pageContent}</AutoLayout>;
  return page;
}

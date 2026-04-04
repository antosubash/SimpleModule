import { usePage } from '@inertiajs/react';
import type * as React from 'react';
import { AppLayout } from './app-layout';
import { PublicLayout } from './public-layout';
import type { SharedProps } from './types';

function AutoLayout({ children }: { children: React.ReactNode }) {
  const { props } = usePage<SharedProps>();
  const { auth } = props;

  if (auth?.isAuthenticated) {
    return <AppLayout>{children}</AppLayout>;
  }
  return <PublicLayout>{children}</PublicLayout>;
}

export function resolveLayout(page: any) {
  if (page.default?.layout) return page;
  page.default.layout = (pageContent: React.ReactNode) => <AutoLayout>{pageContent}</AutoLayout>;
  return page;
}

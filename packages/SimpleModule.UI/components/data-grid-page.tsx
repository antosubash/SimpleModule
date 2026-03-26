import type * as React from 'react';
import { Card, CardContent } from './card';
import { DataGrid } from './data-grid';
import { PageShell, type PageShellProps } from './page-shell';

interface DataGridPageProps<T> extends Omit<PageShellProps, 'children' | 'className'> {
  filterBar?: React.ReactNode;
  data: T[];
  pageSize?: number;
  pageSizeOptions?: number[];
  children: (pageData: T[]) => React.ReactNode;
  emptyTitle?: string;
  emptyDescription?: string;
  emptyIcon?: React.ReactNode;
  emptyAction?: React.ReactNode;
}

const defaultEmptyIcon = (
  <svg
    aria-hidden="true"
    className="mb-4 h-12 w-12 text-text-muted/50"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.5"
    viewBox="0 0 24 24"
  >
    <path
      strokeLinecap="round"
      strokeLinejoin="round"
      d="M20.25 7.5l-.625 10.632a2.25 2.25 0 01-2.247 2.118H6.622a2.25 2.25 0 01-2.247-2.118L3.75 7.5M10 11.25h4M3.375 7.5h17.25c.621 0 1.125-.504 1.125-1.125v-1.5c0-.621-.504-1.125-1.125-1.125H3.375c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125z"
    />
  </svg>
);

function DataGridPage<T>({
  filterBar,
  data,
  pageSize,
  pageSizeOptions,
  children,
  emptyTitle = 'No items found',
  emptyDescription = 'Get started by creating your first item.',
  emptyIcon = defaultEmptyIcon,
  emptyAction,
  ...shellProps
}: DataGridPageProps<T>) {
  return (
    <PageShell {...shellProps}>
      {filterBar}
      {data.length === 0 ? (
        <Card>
          <CardContent>
            <div className="flex flex-col items-center justify-center py-12 text-center">
              {emptyIcon}
              <h3 className="text-sm font-medium">{emptyTitle}</h3>
              <p className="mt-1 text-sm text-text-muted">{emptyDescription}</p>
              {emptyAction && <div className="mt-4">{emptyAction}</div>}
            </div>
          </CardContent>
        </Card>
      ) : (
        <DataGrid data={data} pageSize={pageSize} pageSizeOptions={pageSizeOptions}>
          {children}
        </DataGrid>
      )}
    </PageShell>
  );
}

export type { DataGridPageProps };
export { DataGridPage };

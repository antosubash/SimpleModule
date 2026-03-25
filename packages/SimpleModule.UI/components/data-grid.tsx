import * as React from 'react';
import { cn } from '../lib/utils';
import { Button } from './button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from './select';

interface DataGridProps<T> {
  /** The full data array */
  data: T[];
  /** Render function for the table — receives the current page's slice of data */
  children: (pageData: T[]) => React.ReactNode;
  /** Items per page — default 10 */
  pageSize?: number;
  /** Available page size options */
  pageSizeOptions?: number[];
  /** Additional className for the wrapper */
  className?: string;
}

function DataGrid<T>({
  data,
  children,
  pageSize: initialPageSize = 10,
  pageSizeOptions = [10, 25, 50],
  className,
}: DataGridProps<T>) {
  const [currentPage, setCurrentPage] = React.useState(1);
  const [pageSize, setPageSize] = React.useState(initialPageSize);

  // Reset to page 1 when data or pageSize changes
  // biome-ignore lint/correctness/useExhaustiveDependencies: intentional reset triggers
  React.useEffect(() => {
    setCurrentPage(1);
  }, [data.length, pageSize]);

  const totalPages = Math.max(1, Math.ceil(data.length / pageSize));
  const startIndex = (currentPage - 1) * pageSize;
  const endIndex = Math.min(startIndex + pageSize, data.length);
  const pageData = data.slice(startIndex, endIndex);

  // Generate page numbers with ellipsis markers
  const getPageNumbers = (): (number | string)[] => {
    if (totalPages <= 7) {
      return Array.from({ length: totalPages }, (_, i) => i + 1);
    }

    const pages: (number | string)[] = [1];

    if (currentPage > 3) {
      pages.push('ellipsis-start');
    }

    const start = Math.max(2, currentPage - 1);
    const end = Math.min(totalPages - 1, currentPage + 1);

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }

    if (currentPage < totalPages - 2) {
      pages.push('ellipsis-end');
    }

    pages.push(totalPages);
    return pages;
  };

  return (
    <div className={cn('space-y-4', className)}>
      {children(pageData)}

      {data.length > 0 && (
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2 text-sm text-text-muted">
            <span>
              Showing {startIndex + 1}–{endIndex} of {data.length}
            </span>
            <Select value={String(pageSize)} onValueChange={(v) => setPageSize(Number(v))}>
              <SelectTrigger className="h-8 w-[70px]" data-testid="datagrid-page-size">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {pageSizeOptions.map((size) => (
                  <SelectItem key={size} value={String(size)}>
                    {size}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <span>per page</span>
          </div>

          {totalPages > 1 && (
            <div className="flex items-center gap-1">
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                disabled={currentPage === 1}
                data-testid="datagrid-prev"
              >
                <svg
                  className="h-4 w-4"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path d="m15 18-6-6 6-6" />
                </svg>
              </Button>

              {getPageNumbers().map((page) =>
                typeof page === 'string' ? (
                  <span
                    key={page}
                    className="flex h-8 w-8 items-center justify-center text-sm text-text-muted"
                  >
                    ...
                  </span>
                ) : (
                  <Button
                    key={page}
                    variant={page === currentPage ? 'secondary' : 'ghost'}
                    size="sm"
                    className="h-8 w-8 p-0"
                    onClick={() => setCurrentPage(page)}
                  >
                    {page}
                  </Button>
                ),
              )}

              <Button
                variant="ghost"
                size="sm"
                onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                disabled={currentPage === totalPages}
                data-testid="datagrid-next"
              >
                <svg
                  className="h-4 w-4"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path d="m9 18 6-6-6-6" />
                </svg>
              </Button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

export type { DataGridProps };
export { DataGrid };

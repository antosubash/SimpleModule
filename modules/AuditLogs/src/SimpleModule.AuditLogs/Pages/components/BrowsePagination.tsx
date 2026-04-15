import { Button } from '@simplemodule/ui';

/** Build a compact pagination range: 1 ... 4 5 [6] 7 8 ... 20 */
function paginationRange(
  current: number,
  total: number,
): (number | 'ellipsis-start' | 'ellipsis-end')[] {
  if (total <= 7) {
    return Array.from({ length: total }, (_, i) => i + 1);
  }
  const pages: (number | 'ellipsis-start' | 'ellipsis-end')[] = [];
  pages.push(1);
  if (current > 3) pages.push('ellipsis-start');
  const start = Math.max(2, current - 1);
  const end = Math.min(total - 1, current + 1);
  for (let i = start; i <= end; i++) pages.push(i);
  if (current < total - 2) pages.push('ellipsis-end');
  pages.push(total);
  return pages;
}

function ChevronLeft() {
  return (
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
  );
}

function ChevronRight() {
  return (
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
  );
}

interface Props {
  currentPage: number;
  totalPages: number;
  onGoToPage: (page: number) => void;
}

export function BrowsePagination({ currentPage, totalPages, onGoToPage }: Props) {
  if (totalPages <= 1) return null;

  return (
    <div className="flex items-center gap-1">
      <Button
        variant="ghost"
        size="sm"
        disabled={currentPage <= 1}
        onClick={() => onGoToPage(currentPage - 1)}
      >
        <ChevronLeft />
      </Button>
      {paginationRange(currentPage, totalPages).map((p) =>
        p === 'ellipsis-start' || p === 'ellipsis-end' ? (
          <span key={p} className="px-1 text-sm text-text-muted">
            ...
          </span>
        ) : (
          <Button
            key={p}
            variant={p === currentPage ? 'secondary' : 'ghost'}
            size="sm"
            className="h-8 w-8 p-0"
            onClick={() => onGoToPage(p as number)}
          >
            {p}
          </Button>
        ),
      )}
      <Button
        variant="ghost"
        size="sm"
        disabled={currentPage >= totalPages}
        onClick={() => onGoToPage(currentPage + 1)}
      >
        <ChevronRight />
      </Button>
    </div>
  );
}

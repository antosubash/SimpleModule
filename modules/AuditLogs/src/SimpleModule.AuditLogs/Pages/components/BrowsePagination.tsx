import { Button } from '@simplemodule/ui';

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
  /** Whether a previous (newer) page exists in the cursor trail. */
  canPrev: boolean;
  /** Whether the current page is full, implying more (older) rows exist. */
  canNext: boolean;
  /** Show the "jump to newest" reset action (hidden on the first page). */
  showNewest: boolean;
  newestLabel: string;
  prevLabel: string;
  nextLabel: string;
  onPrev: () => void;
  onNext: () => void;
  onNewest: () => void;
}

/**
 * Keyset (cursor) pagination control. Unlike numbered pagination, keyset paging is
 * sequential — it walks newest → older via a `before` cursor — so the UI exposes
 * Newest / Previous / Next rather than arbitrary page jumps. This avoids the
 * per-request COUNT(*) and deep-OFFSET row-skip on large tables.
 */
export function BrowsePagination({
  canPrev,
  canNext,
  showNewest,
  newestLabel,
  prevLabel,
  nextLabel,
  onPrev,
  onNext,
  onNewest,
}: Props) {
  if (!canPrev && !canNext) return null;

  return (
    <div className="flex items-center gap-1">
      {showNewest && (
        <Button variant="ghost" size="sm" onClick={onNewest}>
          {newestLabel}
        </Button>
      )}
      <Button variant="ghost" size="sm" disabled={!canPrev} onClick={onPrev} aria-label={prevLabel}>
        <ChevronLeft />
      </Button>
      <Button variant="ghost" size="sm" disabled={!canNext} onClick={onNext} aria-label={nextLabel}>
        <ChevronRight />
      </Button>
    </div>
  );
}

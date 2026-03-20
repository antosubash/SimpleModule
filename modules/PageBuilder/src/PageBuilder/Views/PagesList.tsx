import { Link } from '@inertiajs/react';
import { Card, CardContent } from '@simplemodule/ui';
import type { PageSummary } from '../types';

interface Props {
  pages: PageSummary[];
}

export default function PagesList({ pages }: Props) {
  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <h1 className="text-2xl font-bold tracking-tight">Pages</h1>
      {pages.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-12 text-center">
          <svg
            className="mb-4 h-12 w-12 text-muted-foreground/50"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.5"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              d="M12 6.042A8.967 8.967 0 006 3.75c-1.052 0-2.062.18-3 .512v14.25A8.987 8.987 0 016 18c2.305 0 4.408.867 6 2.292m0-14.25a8.966 8.966 0 016-2.292c1.052 0 2.062.18 3 .512v14.25A8.987 8.987 0 0018 18a8.967 8.967 0 00-6 2.292m0-14.25v14.25"
            />
          </svg>
          <h3 className="text-sm font-medium text-foreground">No published pages yet</h3>
          <p className="mt-1 text-sm text-muted-foreground">
            Published pages will appear here for visitors to read.
          </p>
        </div>
      ) : (
        <div className="space-y-3">
          {pages.map((page) => (
            <Link key={page.id} href={`/p/${page.slug}`}>
              <Card className="hover:bg-surface-elevated transition-colors cursor-pointer">
                <CardContent className="p-4">
                  <span className="font-medium">{page.title}</span>
                </CardContent>
              </Card>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}

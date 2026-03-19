import { Link } from '@inertiajs/react';
import { Card, CardContent } from '@simplemodule/ui';
import type { PageSummary } from '../types';

interface Props {
  pages: PageSummary[];
}

export default function PagesList({ pages }: Props) {
  return (
    <div className="max-w-4xl mx-auto py-8">
      <h1 className="text-2xl font-extrabold tracking-tight mb-6">Pages</h1>
      {pages.length === 0 ? (
        <p className="text-text-muted">No published pages yet.</p>
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

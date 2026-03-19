import { Link } from '@inertiajs/react';

interface PageSummary {
  id: number;
  title: string;
  slug: string;
}

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
        <ul className="space-y-3">
          {pages.map((page) => (
            <li key={page.id}>
              <Link
                href={`/p/${page.slug}`}
                className="block p-4 border border-border rounded-lg hover:bg-surface-elevated transition-colors"
              >
                <span className="font-medium">{page.title}</span>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

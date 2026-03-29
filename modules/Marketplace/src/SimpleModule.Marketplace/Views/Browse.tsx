import { router } from '@inertiajs/react';
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Input,
  PageShell,
} from '@simplemodule/ui';
import { useState } from 'react';
import type { MarketplacePackage } from '../types';
import { formatDownloads } from './utils';

const categories = [
  'All',
  'Auth',
  'Storage',
  'UI',
  'Analytics',
  'Integration',
  'Communication',
  'Monitoring',
  'Other',
];
const sortOptions = [
  { value: 'Relevance', label: 'Relevance' },
  { value: 'Downloads', label: 'Most Downloads' },
  { value: 'Alphabetical', label: 'A-Z' },
];

interface Props {
  packages: MarketplacePackage[];
  totalHits: number;
  query: string;
  selectedCategory: string;
  selectedSort: string;
}

export default function Browse({
  packages,
  totalHits,
  query,
  selectedCategory,
  selectedSort,
}: Props) {
  const [search, setSearch] = useState(query);

  function navigate(params: Record<string, string>) {
    const current = new URLSearchParams();
    if (search) current.set('q', search);
    if (selectedCategory !== 'All') current.set('category', selectedCategory);
    if (selectedSort !== 'Relevance') current.set('sort', selectedSort);

    for (const [key, value] of Object.entries(params)) {
      if (value && value !== 'All' && value !== 'Relevance') {
        current.set(key, value);
      } else {
        current.delete(key);
      }
    }

    router.get(`/marketplace/browse?${current.toString()}`);
  }

  function handleSearch(e: React.FormEvent) {
    e.preventDefault();
    navigate({ q: search });
  }

  return (
    <PageShell title="Module Marketplace" description={`${totalHits} modules available`}>
      <div className="space-y-6">
        <form onSubmit={handleSearch} className="flex gap-3">
          <Input
            placeholder="Search modules..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="flex-1"
          />
          <Button type="submit">Search</Button>
        </form>

        <div className="flex flex-wrap items-center gap-3">
          <div className="flex flex-wrap gap-2">
            {categories.map((cat) => (
              <Button
                key={cat}
                variant={selectedCategory === cat ? 'default' : 'secondary'}
                size="sm"
                onClick={() => navigate({ category: cat })}
              >
                {cat}
              </Button>
            ))}
          </div>

          <select
            value={selectedSort}
            onChange={(e) => navigate({ sort: e.target.value })}
            className="ml-auto rounded-md border border-border bg-surface px-3 py-1.5 text-sm text-text"
          >
            {sortOptions.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
        </div>

        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {packages.map((pkg) => (
            <Card
              key={pkg.id}
              className="cursor-pointer transition-shadow hover:shadow-md"
              onClick={() => router.get(`/marketplace/${pkg.id}`)}
            >
              <CardHeader className="pb-3">
                <div className="flex items-start gap-3">
                  {pkg.icon ? (
                    <img src={pkg.icon} alt="" className="h-10 w-10 rounded" />
                  ) : (
                    <div className="flex h-10 w-10 items-center justify-center rounded bg-muted text-text-muted">
                      <svg
                        className="h-5 w-5"
                        fill="none"
                        stroke="currentColor"
                        strokeWidth="2"
                        viewBox="0 0 24 24"
                      >
                        <path d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
                      </svg>
                    </div>
                  )}
                  <div className="min-w-0 flex-1">
                    <CardTitle className="truncate text-base">{pkg.title}</CardTitle>
                    <p className="text-xs text-text-muted">v{pkg.latestVersion}</p>
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                <p className="mb-3 line-clamp-2 text-sm text-text-muted">{pkg.description}</p>
                <div className="flex flex-wrap items-center gap-2">
                  <Badge variant="secondary">{pkg.category}</Badge>
                  {pkg.isInstalled && <Badge variant="default">Installed</Badge>}
                  <span className="ml-auto text-xs text-text-muted">
                    {formatDownloads(pkg.totalDownloads)} downloads
                  </span>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>

        {packages.length === 0 && (
          <div className="py-12 text-center text-text-muted">
            <p className="text-lg font-medium">No modules found</p>
            <p className="mt-1 text-sm">Try adjusting your search or filters.</p>
          </div>
        )}
      </div>
    </PageShell>
  );
}

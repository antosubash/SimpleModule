import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import { Badge, Button, Card, CardContent, CardFooter, Input, PageShell } from '@simplemodule/ui';
import { useState } from 'react';
import { MarketplaceKeys } from '../Locales/keys';
import type { MarketplacePackage } from '../types';
import { categoryLabel, categoryNames, formatDownloads } from './utils';

const PAGE_SIZE = 24;

interface Props {
  packages: MarketplacePackage[];
  totalHits: number;
  query: string;
  selectedCategory: string;
  selectedSort: string;
  skip: number;
  hasMore: boolean;
}

export default function Browse({
  packages,
  totalHits,
  query,
  selectedCategory,
  selectedSort,
  skip,
  hasMore,
}: Props) {
  const { t } = useTranslation('Marketplace');
  const [search, setSearch] = useState(query);

  const sortOptions = [
    { value: 'Relevance', label: t(MarketplaceKeys.Browse.SortRelevance) },
    { value: 'Downloads', label: t(MarketplaceKeys.Browse.SortDownloads) },
    { value: 'Alphabetical', label: t(MarketplaceKeys.Browse.SortAlphabetical) },
  ];

  function buildParams(overrides: Record<string, string> = {}) {
    const params = new URLSearchParams();
    const merged = {
      q: search,
      category: selectedCategory,
      sort: selectedSort,
      ...overrides,
    };
    for (const [key, value] of Object.entries(merged)) {
      if (value && value !== 'All' && value !== 'Relevance') {
        params.set(key, value);
      }
    }
    return params;
  }

  function navigate(overrides: Record<string, string>) {
    router.get(`/marketplace/browse?${buildParams(overrides).toString()}`);
  }

  function handleSearch(e: React.FormEvent) {
    e.preventDefault();
    navigate({ q: search });
  }

  function handleLoadMore() {
    const params = buildParams({ q: query });
    params.set('skip', String(skip + PAGE_SIZE));
    router.get(`/marketplace/browse?${params.toString()}`);
  }

  return (
    <PageShell
      title={t(MarketplaceKeys.Browse.Title)}
      description={t(MarketplaceKeys.Browse.Description, { totalHits: String(totalHits) })}
    >
      <div className="space-y-6">
        <form onSubmit={handleSearch} className="flex gap-3">
          <Input
            placeholder={t(MarketplaceKeys.Browse.SearchPlaceholder)}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="flex-1"
          />
          <Button type="submit">{t(MarketplaceKeys.Browse.SearchButton)}</Button>
        </form>

        <div className="flex flex-wrap items-center gap-3">
          <div className="flex flex-wrap gap-2">
            {categoryNames.map((cat) => (
              <Button
                key={cat}
                variant={selectedCategory === cat ? 'primary' : 'secondary'}
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
              className={`cursor-pointer transition-all duration-200 hover:shadow-md hover:-translate-y-0.5 ${
                pkg.isInstalled ? 'border-l-2 border-l-primary' : ''
              }`}
              onClick={() => router.get(`/marketplace/${pkg.id}`)}
            >
              <CardContent className="pt-5">
                <div className="flex items-start gap-4">
                  {pkg.icon ? (
                    <img src={pkg.icon} alt="" className="h-12 w-12 shrink-0 rounded-xl" />
                  ) : (
                    <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-surface-sunken text-text-muted">
                      <svg
                        className="h-6 w-6"
                        fill="none"
                        stroke="currentColor"
                        strokeWidth="1.5"
                        viewBox="0 0 24 24"
                      >
                        <path d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
                      </svg>
                    </div>
                  )}
                  <div className="min-w-0 flex-1">
                    <h3 className="truncate text-base font-semibold text-text">{pkg.title}</h3>
                    <p className="text-xs text-text-muted">{pkg.authors}</p>
                  </div>
                </div>
                <p className="mt-3 line-clamp-2 text-sm text-text-secondary">{pkg.description}</p>
              </CardContent>
              <CardFooter className="flex items-center gap-2 text-xs">
                <Badge variant="default">{categoryLabel(pkg.category)}</Badge>
                {pkg.isInstalled && (
                  <Badge variant="success">{t(MarketplaceKeys.Browse.BadgeInstalled)}</Badge>
                )}
                <span className="ml-auto flex items-center gap-1 text-text-muted">
                  <svg
                    className="h-3.5 w-3.5"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    viewBox="0 0 24 24"
                  >
                    <path d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
                  </svg>
                  {formatDownloads(pkg.totalDownloads)}
                </span>
                <span className="text-text-muted">v{pkg.latestVersion}</span>
              </CardFooter>
            </Card>
          ))}
        </div>

        {hasMore && (
          <div className="flex justify-center pt-4">
            <Button variant="secondary" onClick={handleLoadMore}>
              {t(MarketplaceKeys.Browse.LoadMore)}
            </Button>
          </div>
        )}

        {packages.length === 0 && (
          <div className="py-16 text-center text-text-muted">
            <svg
              className="mx-auto mb-4 h-12 w-12"
              fill="none"
              stroke="currentColor"
              strokeWidth="1.5"
              viewBox="0 0 24 24"
            >
              <path d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
            </svg>
            <p className="text-lg font-medium">{t(MarketplaceKeys.Browse.EmptyTitle)}</p>
            <p className="mt-1 text-sm">{t(MarketplaceKeys.Browse.EmptyBody)}</p>
            {(query || selectedCategory !== 'All') && (
              <Button
                variant="secondary"
                className="mt-4"
                onClick={() => router.get('/marketplace/browse')}
              >
                {t(MarketplaceKeys.Browse.ClearFilters)}
              </Button>
            )}
          </div>
        )}
      </div>
    </PageShell>
  );
}

import { router } from '@inertiajs/react';
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  PageShell,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { useState } from 'react';
import type { MarketplacePackageDetail } from '../types';
import { formatDownloads } from './utils';

interface Props {
  package: MarketplacePackageDetail;
}

export default function Detail({ package: pkg }: Props) {
  const [copied, setCopied] = useState(false);
  const installCommand = `sm install ${pkg.id}`;

  function handleCopy() {
    navigator.clipboard.writeText(installCommand);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  return (
    <PageShell
      title={pkg.title}
      description={pkg.description}
      breadcrumbs={[{ label: 'Marketplace', href: '/marketplace/browse' }, { label: pkg.title }]}
    >
      <div className="grid gap-6 lg:grid-cols-3">
        <div className="space-y-6 lg:col-span-2">
          <Card>
            <CardHeader>
              <div className="flex items-start gap-4">
                {pkg.icon ? (
                  <img src={pkg.icon} alt="" className="h-16 w-16 rounded-lg" />
                ) : (
                  <div className="flex h-16 w-16 items-center justify-center rounded-lg bg-muted text-text-muted">
                    <svg
                      className="h-8 w-8"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="2"
                      viewBox="0 0 24 24"
                    >
                      <path d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
                    </svg>
                  </div>
                )}
                <div className="flex-1">
                  <CardTitle className="text-xl">{pkg.title}</CardTitle>
                  <p className="mt-1 text-sm text-text-muted">by {pkg.authors}</p>
                  <div className="mt-2 flex flex-wrap gap-2">
                    <Badge variant="default">{pkg.category}</Badge>
                    <Badge variant="default">v{pkg.latestVersion}</Badge>
                    {pkg.isInstalled && <Badge variant="default">Installed</Badge>}
                  </div>
                </div>
              </div>
            </CardHeader>
            <CardContent>
              <p className="text-text-muted">{pkg.description}</p>
            </CardContent>
          </Card>

          {pkg.versions.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>Versions</CardTitle>
              </CardHeader>
              <CardContent>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Version</TableHead>
                      <TableHead>Downloads</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {pkg.versions.slice(0, 10).map((v) => (
                      <TableRow key={v.version}>
                        <TableCell className="font-mono text-sm">{v.version}</TableCell>
                        <TableCell>{formatDownloads(v.downloads)}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </CardContent>
            </Card>
          )}
        </div>

        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Install</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex items-center gap-2 rounded-md bg-muted p-3 font-mono text-sm">
                <code className="flex-1 truncate">{installCommand}</code>
                <Button variant="ghost" size="sm" onClick={handleCopy}>
                  {copied ? 'Copied!' : 'Copy'}
                </Button>
              </div>
              <p className="text-xs text-text-muted">
                Run this command in your SimpleModule project root.
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Info</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2 text-sm">
              <div className="flex justify-between">
                <span className="text-text-muted">Downloads</span>
                <span className="font-medium">{formatDownloads(pkg.totalDownloads)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-text-muted">Latest version</span>
                <span className="font-mono">{pkg.latestVersion}</span>
              </div>
              {pkg.projectLink && (
                <div className="flex justify-between">
                  <span className="text-text-muted">Project</span>
                  <a
                    href={pkg.projectLink}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-primary hover:underline"
                  >
                    View
                  </a>
                </div>
              )}
              {pkg.licenseLink && (
                <div className="flex justify-between">
                  <span className="text-text-muted">License</span>
                  <a
                    href={pkg.licenseLink}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-primary hover:underline"
                  >
                    View
                  </a>
                </div>
              )}
            </CardContent>
          </Card>

          {pkg.tags.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>Tags</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="flex flex-wrap gap-1.5">
                  {pkg.tags.map((tag: string) => (
                    <Badge key={tag} variant="default">
                      {tag}
                    </Badge>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}

          <Button
            variant="secondary"
            className="w-full"
            onClick={() => router.get('/marketplace/browse')}
          >
            Back to Marketplace
          </Button>
        </div>
      </div>
    </PageShell>
  );
}

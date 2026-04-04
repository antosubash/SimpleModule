import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  PageShell,
  Separator,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@simplemodule/ui';
import { useState } from 'react';
import Markdown from 'react-markdown';
import { MarketplaceKeys } from '@/Locales/keys';
import type { MarketplacePackageDetail } from '@/types';
import { categoryLabel, formatDownloads } from './utils';

interface Props {
  package: MarketplacePackageDetail;
}

function CopyButton({ text }: { text: string }) {
  const { t } = useTranslation('Marketplace');
  const [copied, setCopied] = useState(false);

  function handleCopy() {
    navigator.clipboard.writeText(text).catch(() => {});
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  return (
    <Button variant="ghost" size="sm" onClick={handleCopy}>
      {copied ? t(MarketplaceKeys.Detail.CopiedButton) : t(MarketplaceKeys.Detail.CopyButton)}
    </Button>
  );
}

function CommandBlock({ command }: { command: string }) {
  return (
    <div className="flex items-center gap-2 rounded-md bg-surface-sunken p-3 font-mono text-sm">
      <code className="flex-1 text-xs">{command}</code>
      <CopyButton text={command} />
    </div>
  );
}

interface InfoRowProps {
  icon: string;
  label: string;
  children: React.ReactNode;
}

function InfoRow({ icon, label, children }: InfoRowProps) {
  return (
    <div className="flex items-center justify-between">
      <span className="flex items-center gap-2 text-text-muted">
        <svg
          className="h-4 w-4"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <path d={icon} />
        </svg>
        {label}
      </span>
      {children}
    </div>
  );
}

const icons = {
  download: 'M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4',
  tag: 'M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A2 2 0 013 12V7a4 4 0 014-4z',
  externalLink: 'M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14',
  document:
    'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z',
};

export default function Detail({ package: pkg }: Props) {
  const { t } = useTranslation('Marketplace');
  const smCommand = `sm install ${pkg.id}`;
  const dotnetCommand = `dotnet add package ${pkg.id}`;

  const infoItems = [
    {
      icon: icons.download,
      label: t(MarketplaceKeys.Detail.DownloadsLabel),
      value: <span className="font-medium text-text">{formatDownloads(pkg.totalDownloads)}</span>,
    },
    {
      icon: icons.tag,
      label: t(MarketplaceKeys.Detail.VersionLabel),
      value: <span className="font-mono text-text">{pkg.latestVersion}</span>,
    },
    ...(pkg.projectLink
      ? [
          {
            icon: icons.externalLink,
            label: t(MarketplaceKeys.Detail.ProjectLabel),
            value: (
              <a
                href={pkg.projectLink}
                target="_blank"
                rel="noopener noreferrer"
                className="text-primary hover:underline"
              >
                {t(MarketplaceKeys.Detail.ProjectView)}
              </a>
            ),
          },
        ]
      : []),
    ...(pkg.licenseLink
      ? [
          {
            icon: icons.document,
            label: t(MarketplaceKeys.Detail.LicenseLabel),
            value: (
              <a
                href={pkg.licenseLink}
                target="_blank"
                rel="noopener noreferrer"
                className="text-primary hover:underline"
              >
                {t(MarketplaceKeys.Detail.LicenseView)}
              </a>
            ),
          },
        ]
      : []),
  ];

  return (
    <PageShell
      title={pkg.title}
      description={pkg.description}
      breadcrumbs={[
        { label: t(MarketplaceKeys.Detail.BreadcrumbMarketplace), href: '/marketplace/browse' },
        { label: pkg.title },
      ]}
    >
      <div className="grid gap-4 lg:gap-6 lg:grid-cols-3">
        <div className="space-y-4 sm:space-y-6 lg:col-span-2">
          <Card>
            <CardContent className="pt-4 sm:pt-6">
              <div className="flex flex-col items-start gap-3 sm:flex-row sm:gap-5">
                {pkg.icon ? (
                  <img src={pkg.icon} alt="" className="h-20 w-20 rounded-xl" />
                ) : (
                  <div className="flex h-20 w-20 items-center justify-center rounded-xl bg-surface-sunken text-text-muted">
                    <svg
                      className="h-10 w-10"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="1.5"
                      viewBox="0 0 24 24"
                      aria-hidden="true"
                    >
                      <path d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
                    </svg>
                  </div>
                )}
                <div className="flex-1">
                  <h1 className="text-2xl font-bold text-text">{pkg.title}</h1>
                  <div className="mt-1.5 flex flex-wrap items-center gap-2 text-sm text-text-muted">
                    <span>{pkg.authors}</span>
                    <Separator orientation="vertical" className="h-4" />
                    <span className="flex items-center gap-1">
                      <svg
                        className="h-3.5 w-3.5"
                        fill="none"
                        stroke="currentColor"
                        strokeWidth="2"
                        viewBox="0 0 24 24"
                        aria-hidden="true"
                      >
                        <path d={icons.download} />
                      </svg>
                      {formatDownloads(pkg.totalDownloads)}{' '}
                      {t(MarketplaceKeys.Detail.DownloadsSuffix)}
                    </span>
                    <Separator orientation="vertical" className="h-4" />
                    <span className="font-mono">v{pkg.latestVersion}</span>
                  </div>
                  <div className="mt-3 flex flex-wrap gap-2">
                    <Badge variant="default">{categoryLabel(pkg.category)}</Badge>
                    {pkg.isInstalled && (
                      <Badge variant="success">{t(MarketplaceKeys.Detail.BadgeInstalled)}</Badge>
                    )}
                  </div>
                </div>
              </div>
              <p className="mt-4 text-text-secondary leading-relaxed">{pkg.description}</p>

              <div className="mt-5">
                <Tabs defaultValue="sm">
                  <TabsList>
                    <TabsTrigger value="sm">{t(MarketplaceKeys.Detail.TabSmCli)}</TabsTrigger>
                    <TabsTrigger value="dotnet">
                      {t(MarketplaceKeys.Detail.TabDotnetCli)}
                    </TabsTrigger>
                  </TabsList>
                  <TabsContent value="sm">
                    <CommandBlock command={smCommand} />
                  </TabsContent>
                  <TabsContent value="dotnet">
                    <CommandBlock command={dotnetCommand} />
                  </TabsContent>
                </Tabs>
              </div>
            </CardContent>
          </Card>

          {pkg.readme && (
            <Card>
              <CardContent className="pt-6">
                <div className="markdown-content">
                  <Markdown>{pkg.readme}</Markdown>
                </div>
              </CardContent>
            </Card>
          )}
        </div>

        <div className="space-y-4 sm:space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>{t(MarketplaceKeys.Detail.PackageInfoTitle)}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              {infoItems.map((item, i) => (
                <div key={item.label}>
                  {i > 0 && <Separator className="mb-3" />}
                  <InfoRow icon={item.icon} label={item.label}>
                    {item.value}
                  </InfoRow>
                </div>
              ))}
            </CardContent>
          </Card>

          {pkg.versions?.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>{t(MarketplaceKeys.Detail.RecentVersionsTitle)}</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2 text-sm">
                {pkg.versions
                  .slice(-3)
                  .reverse()
                  .map((v) => (
                    <div key={v.version} className="flex items-center justify-between">
                      <span className="font-mono text-text">{v.version}</span>
                      <span className="text-text-muted">
                        {formatDownloads(v.downloads)} {t(MarketplaceKeys.Detail.DownloadsSuffix)}
                      </span>
                    </div>
                  ))}
              </CardContent>
            </Card>
          )}

          {pkg.tags?.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>{t(MarketplaceKeys.Detail.TagsTitle)}</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="flex flex-wrap gap-1.5">
                  {pkg.tags.map((tag) => (
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
            {t(MarketplaceKeys.Detail.BackToMarketplace)}
          </Button>
        </div>
      </div>
    </PageShell>
  );
}

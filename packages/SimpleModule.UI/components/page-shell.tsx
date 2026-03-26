import * as React from 'react';
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from './breadcrumb';
import { Container, type ContainerProps } from './container';
import { PageHeader } from './page-header';

interface BreadcrumbEntry {
  label: string;
  href?: string;
}

interface PageShellProps {
  title: string;
  description?: string;
  actions?: React.ReactNode;
  breadcrumbs?: BreadcrumbEntry[];
  children: React.ReactNode;
  className?: string;
  size?: ContainerProps['size'];
}

function PageShell({
  title,
  description,
  actions,
  breadcrumbs,
  children,
  className,
  size,
}: PageShellProps) {
  return (
    <Container className={className ?? 'space-y-6'} size={size}>
      {breadcrumbs && breadcrumbs.length > 0 && (
        <Breadcrumb>
          <BreadcrumbList>
            {breadcrumbs.map((crumb, index) => (
              <React.Fragment key={crumb.label}>
                {index > 0 && <BreadcrumbSeparator />}
                <BreadcrumbItem>
                  {crumb.href ? (
                    <BreadcrumbLink href={crumb.href}>{crumb.label}</BreadcrumbLink>
                  ) : (
                    <BreadcrumbPage>{crumb.label}</BreadcrumbPage>
                  )}
                </BreadcrumbItem>
              </React.Fragment>
            ))}
          </BreadcrumbList>
        </Breadcrumb>
      )}
      <PageHeader className="mb-0" title={title} description={description} actions={actions} />
      {children}
    </Container>
  );
}

export type { BreadcrumbEntry, PageShellProps };
export { PageShell };

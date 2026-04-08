import { Link } from '@inertiajs/react';
import { PageShell } from '@simplemodule/ui';

interface AdminCard {
  title: string;
  description: string;
  url: string;
  icon: string;
}

interface AdminGroup {
  title: string;
  items: AdminCard[];
}

const groups: AdminGroup[] = [
  {
    title: 'Identity',
    items: [
      {
        title: 'Users',
        description: 'Manage user accounts, roles, and permissions',
        url: '/admin/users',
        icon: 'M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z',
      },
      {
        title: 'Roles',
        description: 'Configure roles and assign permissions',
        url: '/admin/roles',
        icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z',
      },
      {
        title: 'OAuth Clients',
        description: 'Manage OAuth/OIDC applications',
        url: '/openiddict/clients',
        icon: 'M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z',
      },
      {
        title: 'Tenants',
        description: 'Manage tenants and host mappings',
        url: '/tenants/manage',
        icon: 'M3.75 21h16.5M4.5 3h15M5.25 3v18m13.5-18v18M9 6.75h1.5m-1.5 3h1.5m-1.5 3h1.5m3-6H15m-1.5 3H15m-1.5 3H15M9 21v-3.375c0-.621.504-1.125 1.125-1.125h3.75c.621 0 1.125.504 1.125 1.125V21',
      },
    ],
  },
  {
    title: 'Content',
    items: [
      {
        title: 'Pages',
        description: 'Manage and publish content pages',
        url: '/pages/manage',
        icon: 'M19 20H5a2 2 0 01-2-2V6a2 2 0 012-2h10a2 2 0 012 2v1m2 13a2 2 0 01-2-2V7m2 13a2 2 0 002-2V9a2 2 0 00-2-2h-2m-4-3H9M7 16h6M7 8h6v4H7V8z',
      },
      {
        title: 'Email Templates',
        description: 'Create and edit email templates',
        url: '/email/templates',
        icon: 'M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z',
      },
      {
        title: 'Email History',
        description: 'View sent emails and delivery status',
        url: '/email/history',
        icon: 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z',
      },
      {
        title: 'Menus',
        description: 'Configure navigation menus',
        url: '/settings/menus',
        icon: 'M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5',
      },
    ],
  },
  {
    title: 'System',
    items: [
      {
        title: 'Feature Flags',
        description: 'Toggle features on and off',
        url: '/feature-flags/manage',
        icon: 'M3 3v1.5M3 21v-6m0 0 2.77-.693a9 9 0 0 1 6.208.682l.108.054a9 9 0 0 0 6.086.71l3.114-.732a48.524 48.524 0 0 1-.005-10.499l-3.11.732a9 9 0 0 1-6.085-.711l-.108-.054a9 9 0 0 0-6.208-.682L3 4.5M3 15V4.5',
      },
      {
        title: 'Rate Limiting',
        description: 'Configure API rate limit policies',
        url: '/rate-limiting/manage',
        icon: 'M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z',
      },
      {
        title: 'Background Jobs',
        description: 'Monitor and manage job queues',
        url: '/admin/jobs',
        icon: 'M5.636 18.364a9 9 0 010-12.728m12.728 0a9 9 0 010 12.728M12 12v.01M8.464 15.536a5 5 0 010-7.072m7.072 0a5 5 0 010 7.072',
      },
      {
        title: 'Audit Logs',
        description: 'Review activity and change history',
        url: '/audit-logs/browse',
        icon: 'M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 002.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 00-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 00.75-.75 2.25 2.25 0 00-.1-.664m-5.8 0A2.251 2.251 0 0113.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25zM6.75 12h.008v.008H6.75V12zm0 3h.008v.008H6.75V15zm0 3h.008v.008H6.75V18z',
      },
      {
        title: 'App Settings',
        description: 'Configure application-wide settings',
        url: '/settings/manage',
        icon: 'M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z',
      },
    ],
  },
];

function CardIcon({ icon }: { icon: string }) {
  return (
    <svg
      className="w-6 h-6 text-primary"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.5}
      viewBox="0 0 24 24"
      aria-hidden="true"
    >
      <path strokeLinecap="round" strokeLinejoin="round" d={icon} />
    </svg>
  );
}

export default function Hub() {
  return (
    <PageShell
      title="Administration"
      description="Manage your application settings, users, and system configuration"
      breadcrumbs={[{ label: 'Home', href: '/' }, { label: 'Admin' }]}
    >
      <div className="space-y-8">
        {groups.map((group) => (
          <section key={group.title}>
            <h2 className="text-sm font-semibold uppercase tracking-wider text-text-muted mb-3">
              {group.title}
            </h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
              {group.items.map((item) => (
                <Link
                  key={item.url}
                  href={item.url}
                  className="group flex items-start gap-4 p-4 rounded-xl border border-border bg-surface hover:bg-surface-raised hover:border-primary/30 transition-all duration-150 no-underline"
                >
                  <div className="mt-0.5 p-2 rounded-lg bg-primary-subtle group-hover:bg-primary/10 transition-colors">
                    <CardIcon icon={item.icon} />
                  </div>
                  <div>
                    <div className="text-sm font-semibold text-text group-hover:text-primary transition-colors">
                      {item.title}
                    </div>
                    <div className="text-xs text-text-muted mt-0.5">{item.description}</div>
                  </div>
                </Link>
              ))}
            </div>
          </section>
        ))}
      </div>
    </PageShell>
  );
}

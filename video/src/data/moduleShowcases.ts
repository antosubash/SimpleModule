export type ModuleShowcase = {
  id: string;
  name: string;
  tagline: string;
  accent: string;
  features: readonly [string, string, string];
  narration: string;
};

export const moduleShowcases: readonly ModuleShowcase[] = [
  {
    id: 'Users',
    name: 'Users',
    tagline: 'Identity & accounts, done right.',
    accent: '#60a5fa',
    features: [
      'Registration, login, and profiles',
      'Roles, claims, and tokens',
      'OAuth 2.1 via OpenIddict',
    ],
    narration:
      'The Users module handles identity — registration, login, profiles, and roles. Backed by OpenIddict, so OAuth and OpenID Connect work out of the box.',
  },
  {
    id: 'Permissions',
    name: 'Permissions',
    tagline: 'Authorization the compiler can verify.',
    accent: '#a78bfa',
    features: [
      'Declarative [Authorize] per endpoint',
      'Roles and permission claims',
      'Enforced by the source generator',
    ],
    narration:
      'Permissions is the authorization layer. Define rules once, and every endpoint enforces them — checked at compile time by the source generator.',
  },
  {
    id: 'Settings',
    name: 'Settings',
    tagline: 'Typed configuration you can change at runtime.',
    accent: '#94a3b8',
    features: [
      'Strongly-typed setting classes',
      'Edit live from the admin UI',
      'Cached via FusionCache',
    ],
    narration:
      'Settings gives every module typed, cached configuration that you can edit at runtime. No redeploy needed to flip a flag or tune a limit.',
  },
  {
    id: 'Admin',
    name: 'Admin',
    tagline: 'Your built-in control panel.',
    accent: '#fbbf24',
    features: [
      'Users, roles, and permissions',
      'Per-module admin pages',
      'React + Inertia, no setup',
    ],
    narration:
      'Admin is the built-in control panel. Manage users, roles, and every module from one React dashboard — no extra setup required.',
  },
  {
    id: 'Dashboard',
    name: 'Dashboard',
    tagline: 'Home for your application.',
    accent: '#22d3ee',
    features: [
      'Real-time health widgets',
      'Per-module status tiles',
      'Extend with your own cards',
    ],
    narration:
      'Dashboard is your home screen. It shows health, metrics, and every module active in your application, updated in real time.',
  },
  {
    id: 'AuditLogs',
    name: 'AuditLogs',
    tagline: 'Every change, permanently recorded.',
    accent: '#f87171',
    features: [
      'Captured automatically via events',
      'Query by user, entity, or time',
      'Structured JSON, ready to export',
    ],
    narration:
      'AuditLogs captures every significant change — who did what, when, to which entity. Fully queryable and ready for compliance.',
  },
  {
    id: 'FileStorage',
    name: 'FileStorage',
    tagline: 'One interface, any blob store.',
    accent: '#34d399',
    features: [
      'Local disk, S3, or Azure Blob',
      'Stream uploads and downloads',
      'Swap providers in one line',
    ],
    narration:
      'FileStorage abstracts local disk, Azure Blob, and S3 behind one interface. Swap providers with a single line of configuration.',
  },
  {
    id: 'Email',
    name: 'Email',
    tagline: 'Send mail the easy way.',
    accent: '#fb7185',
    features: [
      'Scriban templates',
      'Queued delivery via BackgroundJobs',
      'Retries with exponential backoff',
    ],
    narration:
      'Email ships with templates, queuing, and retries. Send from any module through one contract — delivery details stay out of your way.',
  },
  {
    id: 'BackgroundJobs',
    name: 'BackgroundJobs',
    tagline: 'Durable work, off the request path.',
    accent: '#c084fc',
    features: [
      'Cron and one-off scheduling',
      'Persistent, crash-safe queues',
      'Powered by TickerQ',
    ],
    narration:
      'BackgroundJobs runs work off the request path. Scheduled jobs, durable queues, and automatic retries — powered by TickerQ.',
  },
  {
    id: 'FeatureFlags',
    name: 'FeatureFlags',
    tagline: 'Ship dark. Flip live.',
    accent: '#fb923c',
    features: [
      'Named flags with defaults',
      'Percentage and rule targeting',
      'Toggle at runtime, no deploy',
    ],
    narration:
      'FeatureFlags gates code behind runtime toggles. Percentage rollouts, staff-only previews, or classic A/B — all flippable without a deploy.',
  },
] as const;

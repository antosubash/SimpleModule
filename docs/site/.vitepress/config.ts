import { defineConfig } from 'vitepress';

export default defineConfig({
  title: 'SimpleModule',
  description: 'Modular monolith framework for .NET with compile-time module discovery',

  head: [['link', { rel: 'icon', href: '/favicon.svg' }]],

  cleanUrls: true,

  ignoreDeadLinks: [/localhost/],

  themeConfig: {
    logo: '/favicon.svg',

    nav: [
      { text: 'Getting Started', link: '/getting-started/introduction' },
      { text: 'Guide', link: '/guide/modules' },
      { text: 'Frontend', link: '/frontend/overview' },
      { text: 'CLI', link: '/cli/overview' },
      { text: 'Testing', link: '/testing/overview' },
      { text: 'Advanced', link: '/advanced/source-generator' },
      { text: 'Reference', link: '/reference/api' },
    ],

    sidebar: {
      '/getting-started/': [
        {
          text: 'Getting Started',
          items: [
            { text: 'Introduction', link: '/getting-started/introduction' },
            { text: 'Quick Start', link: '/getting-started/quick-start' },
            { text: 'Project Structure', link: '/getting-started/project-structure' },
          ],
        },
      ],

      '/guide/': [
        {
          text: 'Core Concepts',
          items: [
            { text: 'Modules', link: '/guide/modules' },
            { text: 'Endpoints', link: '/guide/endpoints' },
            { text: 'Contracts & DTOs', link: '/guide/contracts' },
            { text: 'Database', link: '/guide/database' },
          ],
        },
        {
          text: 'Communication',
          items: [
            { text: 'Event Bus', link: '/guide/events' },
            { text: 'Permissions', link: '/guide/permissions' },
            { text: 'Menus', link: '/guide/menus' },
            { text: 'Settings', link: '/guide/settings' },
            { text: 'Inertia.js Integration', link: '/guide/inertia' },
          ],
        },
      ],

      '/frontend/': [
        {
          text: 'Frontend',
          items: [
            { text: 'Overview', link: '/frontend/overview' },
            { text: 'Pages Registry', link: '/frontend/pages' },
            { text: 'UI Components', link: '/frontend/components' },
            { text: 'Styling & Theming', link: '/frontend/styling' },
            { text: 'Vite Build System', link: '/frontend/vite' },
          ],
        },
      ],

      '/cli/': [
        {
          text: 'CLI',
          items: [
            { text: 'Overview', link: '/cli/overview' },
            { text: 'sm new project', link: '/cli/new-project' },
            { text: 'sm new module', link: '/cli/new-module' },
            { text: 'sm new feature', link: '/cli/new-feature' },
            { text: 'sm doctor', link: '/cli/doctor' },
          ],
        },
      ],

      '/testing/': [
        {
          text: 'Testing',
          items: [
            { text: 'Overview', link: '/testing/overview' },
            { text: 'Unit Tests', link: '/testing/unit-tests' },
            { text: 'Integration Tests', link: '/testing/integration-tests' },
            { text: 'E2E Tests', link: '/testing/e2e-tests' },
          ],
        },
      ],

      '/advanced/': [
        {
          text: 'Advanced',
          items: [
            { text: 'Source Generator', link: '/advanced/source-generator' },
            { text: 'Type Generation', link: '/advanced/type-generation' },
            { text: 'EF Core Interceptors', link: '/advanced/interceptors' },
            { text: 'Deployment', link: '/advanced/deployment' },
          ],
        },
      ],

      '/reference/': [
        {
          text: 'Reference',
          items: [
            { text: 'API Reference', link: '/reference/api' },
            { text: 'Configuration', link: '/reference/configuration' },
            { text: 'Acknowledgments', link: '/reference/acknowledgments' },
          ],
        },
      ],
    },

    socialLinks: [{ icon: 'github', link: 'https://github.com/antosubash/SimpleModule' }],

    editLink: {
      pattern: 'https://github.com/antosubash/SimpleModule/edit/main/docs/site/:path',
      text: 'Edit this page on GitHub',
    },

    search: {
      provider: 'local',
    },

    docFooter: {
      prev: false,
      next: false,
    },

    footer: {
      message: 'Released under the MIT License.',
      copyright: 'Copyright 2026 SimpleModule Contributors',
    },
  },
});

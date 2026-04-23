export type Tech = {
  name: string;
  tagline: string;
};

export const techStack: readonly Tech[] = [
  { name: '.NET 10', tagline: 'Runtime' },
  { name: 'React 19', tagline: 'UI' },
  { name: 'Inertia.js', tagline: 'Bridge' },
  { name: 'Vite', tagline: 'Bundler' },
  { name: 'Tailwind', tagline: 'Styling' },
  { name: 'EF Core', tagline: 'ORM' },
  { name: 'OpenIddict', tagline: 'Auth' },
] as const;

export const stats = [
  { value: 20, label: 'Modules', suffix: '' },
  { value: 115, label: 'Thousand LOC', suffix: 'k' },
  { value: 129, label: 'Test Files', suffix: '' },
] as const;

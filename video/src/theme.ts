export const colors = {
  primary: '#059669',
  primaryDark: '#047857',
  accent: '#0f766e',
  accentLight: '#0d9488',
  light: '#34d399',
  lighter: '#a7f3d0',
  teal: '#2dd4bf',
  bgDeep: '#022c22',
  bgMid: '#064e3b',
  bgPanel: '#0a1a15',
  ink: '#ecfdf5',
  inkDim: '#a7f3d0',
  inkMuted: '#6ee7b7',
  white: '#ffffff',
  code: {
    bg: '#071613',
    border: '#0f766e',
    text: '#e0f2f1',
    keyword: '#34d399',
    type: '#2dd4bf',
    string: '#fde68a',
    comment: '#6b7280',
    number: '#f0abfc',
    attr: '#7dd3fc',
  },
} as const;

export const fonts = {
  sans: 'Inter, system-ui, sans-serif',
  mono: '"JetBrains Mono", "Fira Code", ui-monospace, Menlo, monospace',
} as const;

export const spring = {
  soft: { damping: 14, mass: 0.8, stiffness: 100 },
  punchy: { damping: 12, mass: 0.6, stiffness: 180 },
} as const;

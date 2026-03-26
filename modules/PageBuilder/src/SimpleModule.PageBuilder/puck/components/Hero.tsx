import type { ComponentConfig } from '@measured/puck';

export type HeroProps = {
  title: string;
  subtitle: string;
  align: 'left' | 'center' | 'right';
  padding: string;
};

export const Hero: ComponentConfig<HeroProps> = {
  fields: {
    title: { type: 'text' },
    subtitle: { type: 'textarea' },
    align: {
      type: 'radio',
      options: [
        { label: 'Left', value: 'left' },
        { label: 'Center', value: 'center' },
        { label: 'Right', value: 'right' },
      ],
    },
    padding: { type: 'text' },
  },
  defaultProps: {
    title: 'Hero Title',
    subtitle: 'Subtitle text goes here',
    align: 'center',
    padding: '64px',
  },
  render: ({ title, subtitle, align, padding }) => (
    <div style={{ textAlign: align, padding }} className="bg-surface-elevated rounded-lg">
      <h1 className="text-4xl font-extrabold tracking-tight mb-4">{title}</h1>
      <p className="text-lg text-text-secondary max-w-2xl mx-auto">{subtitle}</p>
    </div>
  ),
};

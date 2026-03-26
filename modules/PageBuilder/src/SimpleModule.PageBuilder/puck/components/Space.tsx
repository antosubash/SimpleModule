import type { ComponentConfig } from '@measured/puck';

export type SpaceProps = {
  size: string;
};

export const Space: ComponentConfig<SpaceProps> = {
  fields: {
    size: {
      type: 'select',
      options: [
        { label: 'Small (16px)', value: '16px' },
        { label: 'Medium (32px)', value: '32px' },
        { label: 'Large (64px)', value: '64px' },
        { label: 'XL (96px)', value: '96px' },
      ],
    },
  },
  defaultProps: {
    size: '32px',
  },
  render: ({ size }) => <div style={{ height: size }} />,
};

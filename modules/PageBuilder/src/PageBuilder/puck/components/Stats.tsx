import type { ComponentConfig } from '@measured/puck';

export type StatsProps = {
  items: { value: string; label: string }[];
};

export const Stats: ComponentConfig<StatsProps> = {
  fields: {
    items: {
      type: 'array',
      arrayFields: {
        value: { type: 'text' },
        label: { type: 'text' },
      },
      defaultItemProps: {
        value: '100+',
        label: 'Stat',
      },
    },
  },
  defaultProps: {
    items: [
      { value: '10K+', label: 'Users' },
      { value: '99.9%', label: 'Uptime' },
      { value: '24/7', label: 'Support' },
    ],
  },
  render: ({ items }) => (
    <div className="grid grid-cols-3 gap-8 text-center py-8">
      {items.map((item) => (
        <div key={`stat-${item.label}`}>
          <div className="text-3xl font-extrabold text-primary">{item.value}</div>
          <div className="text-sm text-text-muted mt-1">{item.label}</div>
        </div>
      ))}
    </div>
  ),
};

import type { ComponentConfig } from '@measured/puck';

export type ButtonBlockProps = {
  label: string;
  href: string;
  variant: 'primary' | 'secondary' | 'outline';
  align: 'left' | 'center' | 'right';
};

export const ButtonBlock: ComponentConfig<ButtonBlockProps> = {
  fields: {
    label: { type: 'text' },
    href: { type: 'text' },
    variant: {
      type: 'select',
      options: [
        { label: 'Primary', value: 'primary' },
        { label: 'Secondary', value: 'secondary' },
        { label: 'Outline', value: 'outline' },
      ],
    },
    align: {
      type: 'radio',
      options: [
        { label: 'Left', value: 'left' },
        { label: 'Center', value: 'center' },
        { label: 'Right', value: 'right' },
      ],
    },
  },
  defaultProps: {
    label: 'Click me',
    href: '#',
    variant: 'primary',
    align: 'center',
  },
  render: ({ label, href, variant, align, puck }) => {
    const variantClasses = {
      primary: 'bg-primary text-primary-foreground hover:bg-primary/90',
      secondary: 'bg-surface-elevated text-text hover:bg-surface-elevated/80',
      outline: 'border border-border text-text hover:bg-surface-elevated',
    };
    return (
      <div style={{ textAlign: align }}>
        <a
          href={puck.isEditing ? '#' : href}
          tabIndex={puck.isEditing ? -1 : undefined}
          className={`inline-block px-6 py-3 rounded-lg font-medium transition-colors ${variantClasses[variant]}`}
        >
          {label}
        </a>
      </div>
    );
  },
};

import type { ComponentConfig } from '@puckeditor/core';

export type TextProps = {
  text: string;
  align: 'left' | 'center' | 'right';
  size: 'sm' | 'base' | 'lg';
  color: 'default' | 'muted' | 'secondary';
};

export const Text: ComponentConfig<TextProps> = {
  fields: {
    text: { type: 'textarea' },
    align: {
      type: 'radio',
      options: [
        { label: 'Left', value: 'left' },
        { label: 'Center', value: 'center' },
        { label: 'Right', value: 'right' },
      ],
    },
    size: {
      type: 'select',
      options: [
        { label: 'Small', value: 'sm' },
        { label: 'Base', value: 'base' },
        { label: 'Large', value: 'lg' },
      ],
    },
    color: {
      type: 'select',
      options: [
        { label: 'Default', value: 'default' },
        { label: 'Muted', value: 'muted' },
        { label: 'Secondary', value: 'secondary' },
      ],
    },
  },
  defaultProps: {
    text: 'Enter your text here',
    align: 'left',
    size: 'base',
    color: 'default',
  },
  render: ({ text, align, size, color }) => {
    const sizeClasses = { sm: 'text-sm', base: 'text-base', lg: 'text-lg' };
    const colorClasses = {
      default: 'text-text',
      muted: 'text-text-muted',
      secondary: 'text-text-secondary',
    };
    return (
      <p style={{ textAlign: align }} className={`${sizeClasses[size]} ${colorClasses[color]}`}>
        {text}
      </p>
    );
  },
};

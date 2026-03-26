import type { ComponentConfig } from '@puckeditor/core';

export type CardProps = {
  title: string;
  description: string;
  image: string;
};

export const Card: ComponentConfig<CardProps> = {
  fields: {
    title: { type: 'text' },
    description: { type: 'textarea' },
    image: { type: 'text' },
  },
  defaultProps: {
    title: 'Card Title',
    description: 'Card description goes here.',
    image: '',
  },
  render: ({ title, description, image }) => (
    <div className="border border-border rounded-lg overflow-hidden bg-surface">
      {image && <img src={image} alt={title} className="w-full h-48 object-cover" />}
      <div className="p-6">
        <h3 className="text-lg font-semibold mb-2">{title}</h3>
        <p className="text-text-secondary text-sm">{description}</p>
      </div>
    </div>
  ),
};

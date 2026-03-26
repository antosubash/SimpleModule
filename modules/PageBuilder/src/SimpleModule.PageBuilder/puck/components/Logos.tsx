import type { ComponentConfig } from '@measured/puck';

export type LogosProps = {
  logos: { src: string; alt: string }[];
  grayscale: boolean;
};

export const Logos: ComponentConfig<LogosProps> = {
  fields: {
    logos: {
      type: 'array',
      arrayFields: {
        src: { type: 'text' },
        alt: { type: 'text' },
      },
      defaultItemProps: {
        src: 'https://placehold.co/120x40?text=Logo',
        alt: 'Logo',
      },
    },
    grayscale: {
      type: 'radio',
      options: [
        { label: 'Yes', value: true },
        { label: 'No', value: false },
      ],
    },
  },
  defaultProps: {
    logos: [
      { src: 'https://placehold.co/120x40?text=Logo+1', alt: 'Logo 1' },
      { src: 'https://placehold.co/120x40?text=Logo+2', alt: 'Logo 2' },
      { src: 'https://placehold.co/120x40?text=Logo+3', alt: 'Logo 3' },
    ],
    grayscale: true,
  },
  render: ({ logos, grayscale }) => (
    <div className="flex flex-wrap items-center justify-center gap-8 py-8">
      {logos.map((logo) => (
        <img
          key={`logo-${logo.src}`}
          src={logo.src}
          alt={logo.alt}
          className={`h-10 object-contain ${grayscale ? 'opacity-50 grayscale' : ''}`}
        />
      ))}
    </div>
  ),
};

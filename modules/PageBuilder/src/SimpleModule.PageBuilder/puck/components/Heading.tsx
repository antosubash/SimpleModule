import type { ComponentConfig } from '@measured/puck';

export type HeadingProps = {
  text: string;
  level: 'h1' | 'h2' | 'h3' | 'h4' | 'h5' | 'h6';
  align: 'left' | 'center' | 'right';
};

export const Heading: ComponentConfig<HeadingProps> = {
  fields: {
    text: { type: 'text' },
    level: {
      type: 'select',
      options: [
        { label: 'H1', value: 'h1' },
        { label: 'H2', value: 'h2' },
        { label: 'H3', value: 'h3' },
        { label: 'H4', value: 'h4' },
        { label: 'H5', value: 'h5' },
        { label: 'H6', value: 'h6' },
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
    text: 'Heading',
    level: 'h2',
    align: 'left',
  },
  render: ({ text, level, align }) => {
    const Tag = level;
    const sizeClasses: Record<string, string> = {
      h1: 'text-4xl font-extrabold',
      h2: 'text-3xl font-bold',
      h3: 'text-2xl font-bold',
      h4: 'text-xl font-semibold',
      h5: 'text-lg font-semibold',
      h6: 'text-base font-semibold',
    };
    return (
      <Tag style={{ textAlign: align }} className={`${sizeClasses[level]} tracking-tight`}>
        {text}
      </Tag>
    );
  },
};

import type { ComponentConfig } from '@measured/puck';

export type FlexProps = {
  direction: 'row' | 'column';
  justifyContent: 'flex-start' | 'center' | 'flex-end' | 'space-between' | 'space-around';
  alignItems: 'flex-start' | 'center' | 'flex-end' | 'stretch';
  gap: string;
  wrap: 'nowrap' | 'wrap';
};

export const Flex: ComponentConfig<FlexProps> = {
  fields: {
    direction: {
      type: 'radio',
      options: [
        { label: 'Row', value: 'row' },
        { label: 'Column', value: 'column' },
      ],
    },
    justifyContent: {
      type: 'select',
      options: [
        { label: 'Start', value: 'flex-start' },
        { label: 'Center', value: 'center' },
        { label: 'End', value: 'flex-end' },
        { label: 'Space Between', value: 'space-between' },
        { label: 'Space Around', value: 'space-around' },
      ],
    },
    alignItems: {
      type: 'select',
      options: [
        { label: 'Start', value: 'flex-start' },
        { label: 'Center', value: 'center' },
        { label: 'End', value: 'flex-end' },
        { label: 'Stretch', value: 'stretch' },
      ],
    },
    gap: { type: 'text' },
    wrap: {
      type: 'radio',
      options: [
        { label: 'No Wrap', value: 'nowrap' },
        { label: 'Wrap', value: 'wrap' },
      ],
    },
  },
  defaultProps: {
    direction: 'row',
    justifyContent: 'flex-start',
    alignItems: 'center',
    gap: '16px',
    wrap: 'nowrap',
  },
  render: ({
    direction,
    justifyContent,
    alignItems,
    gap,
    wrap,
    puck: { renderDropZone: DropZone },
  }) => (
    <div
      style={{
        display: 'flex',
        flexDirection: direction,
        justifyContent,
        alignItems,
        gap,
        flexWrap: wrap,
      }}
    >
      <DropZone zone="flex-items" />
    </div>
  ),
};

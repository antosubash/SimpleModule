import React from 'react';
import { colors, fonts } from '../theme';
import { ArrowFlow } from './ArrowFlow';

type Props = {
  left: React.ReactNode;
  right: React.ReactNode;
  leftLabel?: string;
  rightLabel?: string;
  arrowFrame?: number;
};

export const SplitCodePair: React.FC<Props> = ({
  left,
  right,
  leftLabel,
  rightLabel,
  arrowFrame = 30,
}) => {
  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 24,
        padding: '0 80px',
        width: '100%',
      }}
    >
      <div style={{ flex: 1, minWidth: 0 }}>
        {leftLabel ? <SectionLabel text={leftLabel} /> : null}
        {left}
      </div>

      <div style={{ width: 160, flexShrink: 0 }}>
        <ArrowFlow startFrame={arrowFrame} duration={24} />
      </div>

      <div style={{ flex: 1, minWidth: 0 }}>
        {rightLabel ? <SectionLabel text={rightLabel} /> : null}
        {right}
      </div>
    </div>
  );
};

const SectionLabel: React.FC<{ text: string }> = ({ text }) => (
  <div
    style={{
      fontFamily: fonts.sans,
      fontSize: 16,
      fontWeight: 700,
      color: colors.inkMuted,
      textTransform: 'uppercase',
      letterSpacing: 1.8,
      marginBottom: 14,
      paddingLeft: 4,
    }}
  >
    {text}
  </div>
);

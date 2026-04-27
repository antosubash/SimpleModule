import React from 'react';
import { useCurrentFrame, useVideoConfig, spring } from 'remotion';
import { colors, fonts } from '../theme';

type Props = {
  name: string;
  delay: number;
};

export const ModuleChip: React.FC<Props> = ({ name, delay }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const p = spring({
    frame: frame - delay,
    fps,
    config: { damping: 13, mass: 0.6, stiffness: 160 },
  });

  return (
    <div
      style={{
        opacity: p,
        transform: `scale(${0.7 + 0.3 * p}) translateY(${(1 - p) * 14}px)`,
        padding: '14px 22px',
        borderRadius: 999,
        fontFamily: fonts.sans,
        fontSize: 26,
        fontWeight: 600,
        color: colors.ink,
        background: `linear-gradient(135deg, ${colors.primary}33 0%, ${colors.accent}33 100%)`,
        border: `1.5px solid ${colors.light}88`,
        boxShadow: `0 6px 24px ${colors.primary}40`,
        whiteSpace: 'nowrap',
        textAlign: 'center',
      }}
    >
      {name}
    </div>
  );
};

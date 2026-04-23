import React from 'react';
import { AbsoluteFill, useCurrentFrame } from 'remotion';
import { colors } from '../theme';

type Props = {
  variant?: 'dark' | 'hero' | 'panel';
};

export const GradientBackground: React.FC<Props> = ({ variant = 'dark' }) => {
  const frame = useCurrentFrame();
  const drift = (frame / 6) % 360;

  const base =
    variant === 'hero'
      ? `radial-gradient(circle at 30% 20%, ${colors.accent}55 0%, transparent 55%),
         radial-gradient(circle at 75% 80%, ${colors.primary}55 0%, transparent 55%),
         linear-gradient(135deg, ${colors.bgDeep} 0%, #031a14 100%)`
      : variant === 'panel'
      ? `linear-gradient(135deg, #04241c 0%, ${colors.bgDeep} 100%)`
      : `linear-gradient(135deg, ${colors.bgDeep} 0%, #03110c 100%)`;

  return (
    <AbsoluteFill
      style={{
        background: base,
      }}
    >
      <AbsoluteFill
        style={{
          background: `conic-gradient(from ${drift}deg at 50% 50%,
            ${colors.primary}11, transparent 30%, ${colors.accent}11 60%,
            transparent 90%, ${colors.primary}11)`,
          opacity: 0.6,
        }}
      />
    </AbsoluteFill>
  );
};

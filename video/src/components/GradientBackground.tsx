import React from 'react';
import { AbsoluteFill, useCurrentFrame } from 'remotion';
import { colors } from '../theme';

type Props = {
  variant?: 'dark' | 'hero' | 'panel';
  // Optional module accent that tints the radial glow; lets each per-module
  // composition have its own signature background even with one template.
  accent?: string;
};

export const GradientBackground: React.FC<Props> = ({ variant = 'dark', accent }) => {
  const frame = useCurrentFrame();
  const drift = (frame / 6) % 360;

  const glowA = accent ?? colors.accent;
  const glowB = accent ?? colors.primary;

  const base =
    variant === 'hero'
      ? `radial-gradient(circle at 30% 20%, ${glowA}55 0%, transparent 55%),
         radial-gradient(circle at 75% 80%, ${glowB}55 0%, transparent 55%),
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
            ${glowB}11, transparent 30%, ${glowA}11 60%,
            transparent 90%, ${glowB}11)`,
          opacity: 0.6,
        }}
      />
    </AbsoluteFill>
  );
};

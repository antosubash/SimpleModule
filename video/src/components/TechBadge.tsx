import React from 'react';
import { useCurrentFrame, useVideoConfig, spring } from 'remotion';
import { colors, fonts } from '../theme';

type Props = {
  name: string;
  tagline: string;
  delay: number;
};

export const TechBadge: React.FC<Props> = ({ name, tagline, delay }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const p = spring({
    frame: frame - delay,
    fps,
    config: { damping: 14, mass: 0.7, stiffness: 150 },
  });

  return (
    <div
      style={{
        opacity: p,
        transform: `translateY(${(1 - p) * 24}px)`,
        minWidth: 180,
        padding: '20px 24px',
        borderRadius: 16,
        background: `linear-gradient(160deg, ${colors.bgPanel} 0%, #0b2820 100%)`,
        border: `1px solid ${colors.accent}66`,
        boxShadow: `0 10px 30px rgba(0,0,0,0.35), inset 0 0 0 1px ${colors.primary}22`,
        fontFamily: fonts.sans,
        textAlign: 'center',
      }}
    >
      <div
        style={{
          fontSize: 28,
          fontWeight: 700,
          color: colors.ink,
          letterSpacing: -0.4,
        }}
      >
        {name}
      </div>
      <div
        style={{
          marginTop: 6,
          fontSize: 15,
          fontWeight: 500,
          color: colors.inkMuted,
          textTransform: 'uppercase',
          letterSpacing: 1.4,
        }}
      >
        {tagline}
      </div>
    </div>
  );
};

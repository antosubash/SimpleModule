import React from 'react';
import { useCurrentFrame, interpolate, Easing } from 'remotion';
import { colors, fonts } from '../theme';

type Props = {
  value: number;
  label: string;
  suffix?: string;
  delay?: number;
  duration?: number;
};

export const BigNumber: React.FC<Props> = ({
  value,
  label,
  suffix = '',
  delay = 0,
  duration = 45,
}) => {
  const frame = useCurrentFrame();

  const current = Math.round(
    interpolate(frame, [delay, delay + duration], [0, value], {
      extrapolateLeft: 'clamp',
      extrapolateRight: 'clamp',
      easing: Easing.out(Easing.cubic),
    }),
  );

  const reveal = interpolate(frame, [delay, delay + 10], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <div
      style={{
        opacity: reveal,
        transform: `translateY(${(1 - reveal) * 20}px)`,
        textAlign: 'center',
        fontFamily: fonts.sans,
      }}
    >
      <div
        style={{
          fontSize: 144,
          fontWeight: 800,
          lineHeight: 1,
          letterSpacing: -5,
          background: `linear-gradient(135deg, ${colors.light} 0%, ${colors.teal} 100%)`,
          WebkitBackgroundClip: 'text',
          WebkitTextFillColor: 'transparent',
          backgroundClip: 'text',
        }}
      >
        {current}
        {suffix}
      </div>
      <div
        style={{
          marginTop: 12,
          fontSize: 24,
          fontWeight: 600,
          color: colors.inkMuted,
          textTransform: 'uppercase',
          letterSpacing: 2,
        }}
      >
        {label}
      </div>
    </div>
  );
};

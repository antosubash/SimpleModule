import React from 'react';
import { AbsoluteFill, useCurrentFrame, useVideoConfig, spring, interpolate } from 'remotion';
import { GradientBackground } from '../components/GradientBackground';
import { Logo } from '../components/Logo';
import { colors, fonts } from '../theme';

export const SceneOpening: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const titleSpring = spring({
    frame: frame - 40,
    fps,
    config: { damping: 16, mass: 0.9, stiffness: 130 },
  });
  const taglineOpacity = interpolate(frame, [60, 80], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const sceneOpacity = interpolate(
    frame,
    [0, 10, 110, 120],
    [0, 1, 1, 0],
    { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' },
  );

  return (
    <AbsoluteFill style={{ opacity: sceneOpacity }}>
      <GradientBackground variant="hero" />
      <AbsoluteFill
        style={{
          alignItems: 'center',
          justifyContent: 'center',
          flexDirection: 'column',
        }}
      >
        <Logo size={340} />
        <div
          style={{
            marginTop: 48,
            fontFamily: fonts.sans,
            fontSize: 128,
            fontWeight: 800,
            letterSpacing: -4,
            color: colors.ink,
            opacity: titleSpring,
            transform: `translateY(${(1 - titleSpring) * 20}px)`,
          }}
        >
          SimpleModule
        </div>
        <div
          style={{
            marginTop: 14,
            fontFamily: fonts.sans,
            fontSize: 32,
            fontWeight: 500,
            color: colors.inkMuted,
            letterSpacing: 0.4,
            opacity: taglineOpacity,
          }}
        >
          The Modular Monolith Framework for .NET
        </div>
      </AbsoluteFill>
    </AbsoluteFill>
  );
};

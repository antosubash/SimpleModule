import React from 'react';
import { AbsoluteFill, useCurrentFrame, useVideoConfig, spring, interpolate } from 'remotion';
import { GradientBackground } from '../components/GradientBackground';
import { Logo } from '../components/Logo';
import { colors, fonts } from '../theme';

export const SceneCTA: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const ctaSpring = spring({
    frame: frame - 20,
    fps,
    config: { damping: 14, mass: 0.8, stiffness: 140 },
  });

  const urlOpacity = interpolate(frame, [50, 70], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <AbsoluteFill>
      <GradientBackground variant="hero" />
      <AbsoluteFill
        style={{
          flexDirection: 'column',
          justifyContent: 'center',
          alignItems: 'center',
        }}
      >
        <Logo size={220} delay={-20} />
        <div
          style={{
            marginTop: 28,
            fontFamily: fonts.sans,
            fontSize: 96,
            fontWeight: 800,
            letterSpacing: -3,
            color: colors.ink,
            opacity: ctaSpring,
            transform: `translateY(${(1 - ctaSpring) * 20}px)`,
            background: `linear-gradient(135deg, ${colors.light} 0%, ${colors.teal} 100%)`,
            WebkitBackgroundClip: 'text',
            WebkitTextFillColor: 'transparent',
            backgroundClip: 'text',
          }}
        >
          Start building.
        </div>
        <div
          style={{
            marginTop: 28,
            opacity: urlOpacity,
            fontFamily: fonts.mono,
            fontSize: 36,
            fontWeight: 500,
            color: colors.ink,
            padding: '14px 28px',
            background: `${colors.primary}22`,
            border: `1px solid ${colors.light}88`,
            borderRadius: 12,
          }}
        >
          github.com/antosubash/SimpleModule
        </div>
      </AbsoluteFill>
    </AbsoluteFill>
  );
};

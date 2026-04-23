import React from 'react';
import { AbsoluteFill, useCurrentFrame, interpolate, spring, useVideoConfig } from 'remotion';
import { GradientBackground } from '../components/GradientBackground';
import { colors, fonts } from '../theme';

export const SceneProblemSolution: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const sceneOpacity = interpolate(
    frame,
    [0, 10, 140, 150],
    [0, 1, 1, 0],
    { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' },
  );

  const problem1 = interpolate(frame, [0, 18], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  const problem2 = interpolate(frame, [28, 48], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const strike = interpolate(frame, [60, 78], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  const solution = spring({
    frame: frame - 75,
    fps,
    config: { damping: 14, mass: 0.8, stiffness: 140 },
  });

  return (
    <AbsoluteFill style={{ opacity: sceneOpacity }}>
      <GradientBackground variant="dark" />
      <AbsoluteFill
        style={{
          alignItems: 'center',
          justifyContent: 'center',
          flexDirection: 'column',
          padding: '0 120px',
          textAlign: 'center',
          fontFamily: fonts.sans,
        }}
      >
        <ProblemLine opacity={problem1} strike={strike}>
          Monoliths get tangled.
        </ProblemLine>
        <ProblemLine opacity={problem2} strike={strike}>
          Microservices get complex.
        </ProblemLine>

        <div
          style={{
            marginTop: 60,
            opacity: solution,
            transform: `translateY(${(1 - solution) * 30}px)`,
          }}
        >
          <div
            style={{
              fontSize: 78,
              fontWeight: 800,
              letterSpacing: -2,
              background: `linear-gradient(135deg, ${colors.light} 0%, ${colors.teal} 100%)`,
              WebkitBackgroundClip: 'text',
              WebkitTextFillColor: 'transparent',
              backgroundClip: 'text',
            }}
          >
            Modular Monolith.
          </div>
          <div
            style={{
              marginTop: 8,
              fontSize: 40,
              fontWeight: 600,
              color: colors.ink,
              letterSpacing: -1,
            }}
          >
            Compile-time wiring.
          </div>
        </div>
      </AbsoluteFill>
    </AbsoluteFill>
  );
};

const ProblemLine: React.FC<{
  opacity: number;
  strike: number;
  children: React.ReactNode;
}> = ({ opacity, strike, children }) => (
  <div
    style={{
      position: 'relative',
      display: 'inline-block',
      fontSize: 54,
      fontWeight: 600,
      color: colors.inkMuted,
      opacity: opacity * (1 - strike * 0.5),
      marginBottom: 12,
    }}
  >
    {children}
    <span
      style={{
        position: 'absolute',
        left: 0,
        right: 0,
        top: '52%',
        height: 4,
        background: colors.light,
        transform: `scaleX(${strike})`,
        transformOrigin: 'left',
        borderRadius: 2,
      }}
    />
  </div>
);

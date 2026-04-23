import React from 'react';
import { AbsoluteFill, useCurrentFrame, interpolate } from 'remotion';
import { GradientBackground } from '../components/GradientBackground';
import { CodeBlock } from '../components/CodeBlock';
import { endpointCode } from '../data/codeSnippets';
import { colors, fonts } from '../theme';

export const SceneEndpoint: React.FC = () => {
  const frame = useCurrentFrame();
  const sceneOpacity = interpolate(
    frame,
    [0, 10, 170, 180],
    [0, 1, 1, 0],
    { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' },
  );

  const captionOpacity = interpolate(frame, [110, 130], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const badge1 = interpolate(frame, [80, 100], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  const badge2 = interpolate(frame, [95, 115], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <AbsoluteFill style={{ opacity: sceneOpacity }}>
      <GradientBackground variant="dark" />
      <AbsoluteFill
        style={{
          flexDirection: 'column',
          justifyContent: 'center',
          alignItems: 'center',
          padding: '0 120px',
        }}
      >
        <Heading>
          <span style={{ color: colors.light }}>IEndpoint</span> — one class, fully wired
        </Heading>

        <div style={{ width: '75%', maxWidth: 1280 }}>
          <CodeBlock
            code={endpointCode}
            fontSize={24}
            revealStart={8}
            revealDuration={70}
            label="Auto-discovered"
          />
        </div>

        <div
          style={{
            marginTop: 32,
            display: 'flex',
            gap: 20,
            fontFamily: fonts.sans,
          }}
        >
          <Badge opacity={badge1} label="Route registered" />
          <Badge opacity={badge2} label="Permission enforced" />
        </div>

        <div
          style={{
            marginTop: 36,
            opacity: captionOpacity,
            fontFamily: fonts.sans,
            fontSize: 30,
            fontWeight: 600,
            color: colors.inkMuted,
            letterSpacing: 0.3,
          }}
        >
          Endpoints register themselves. Permissions are declarative.
        </div>
      </AbsoluteFill>
    </AbsoluteFill>
  );
};

const Heading: React.FC<{ children: React.ReactNode }> = ({ children }) => (
  <div
    style={{
      fontFamily: fonts.sans,
      fontSize: 48,
      fontWeight: 700,
      color: colors.ink,
      letterSpacing: -0.8,
      marginBottom: 36,
      textAlign: 'center',
    }}
  >
    {children}
  </div>
);

const Badge: React.FC<{ opacity: number; label: string }> = ({ opacity, label }) => (
  <div
    style={{
      opacity,
      transform: `translateY(${(1 - opacity) * 12}px)`,
      padding: '10px 20px',
      borderRadius: 999,
      background: `${colors.primary}22`,
      border: `1px solid ${colors.light}88`,
      color: colors.ink,
      fontSize: 20,
      fontWeight: 600,
    }}
  >
    ✓ {label}
  </div>
);

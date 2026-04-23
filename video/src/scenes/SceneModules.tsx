import React from 'react';
import { AbsoluteFill, useCurrentFrame, interpolate } from 'remotion';
import { GradientBackground } from '../components/GradientBackground';
import { ModuleChip } from '../components/ModuleChip';
import { modules } from '../data/modules';
import { colors, fonts } from '../theme';

export const SceneModules: React.FC = () => {
  const frame = useCurrentFrame();
  const sceneOpacity = interpolate(
    frame,
    [0, 10, 170, 180],
    [0, 1, 1, 0],
    { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' },
  );

  const captionOpacity = interpolate(frame, [130, 150], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <AbsoluteFill style={{ opacity: sceneOpacity }}>
      <GradientBackground variant="hero" />
      <AbsoluteFill
        style={{
          flexDirection: 'column',
          justifyContent: 'center',
          alignItems: 'center',
          padding: '0 120px',
        }}
      >
        <div
          style={{
            fontFamily: fonts.sans,
            fontSize: 46,
            fontWeight: 700,
            color: colors.ink,
            letterSpacing: -0.8,
            marginBottom: 44,
            textAlign: 'center',
          }}
        >
          Batteries-included modules — or build your own.
        </div>

        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(5, minmax(0, 1fr))',
            gap: 18,
            width: '100%',
            maxWidth: 1500,
            justifyItems: 'center',
          }}
        >
          {modules.map((name, i) => (
            <ModuleChip key={name} name={name} delay={10 + i * 4} />
          ))}
        </div>

        <div
          style={{
            marginTop: 44,
            opacity: captionOpacity,
            fontFamily: fonts.sans,
            fontSize: 28,
            fontWeight: 600,
            color: colors.inkMuted,
            letterSpacing: 0.4,
          }}
        >
          20 production-ready modules shipped today.
        </div>
      </AbsoluteFill>
    </AbsoluteFill>
  );
};

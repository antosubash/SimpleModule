import React from 'react';
import { AbsoluteFill, useCurrentFrame, interpolate } from 'remotion';
import { GradientBackground } from '../components/GradientBackground';
import { TechBadge } from '../components/TechBadge';
import { BigNumber } from '../components/BigNumber';
import { techStack, stats } from '../data/techStack';
import { colors, fonts } from '../theme';

export const SceneTechStats: React.FC = () => {
  const frame = useCurrentFrame();
  const sceneOpacity = interpolate(
    frame,
    [0, 10, 170, 180],
    [0, 1, 1, 0],
    { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' },
  );

  return (
    <AbsoluteFill style={{ opacity: sceneOpacity }}>
      <GradientBackground variant="dark" />
      <AbsoluteFill
        style={{
          flexDirection: 'column',
          justifyContent: 'center',
          alignItems: 'center',
          padding: '0 80px',
          gap: 60,
        }}
      >
        <div>
          <div
            style={{
              fontFamily: fonts.sans,
              fontSize: 20,
              fontWeight: 700,
              color: colors.inkMuted,
              textTransform: 'uppercase',
              letterSpacing: 2.5,
              marginBottom: 22,
              textAlign: 'center',
            }}
          >
            Built on modern foundations
          </div>
          <div
            style={{
              display: 'flex',
              gap: 18,
              flexWrap: 'wrap',
              justifyContent: 'center',
            }}
          >
            {techStack.map((t, i) => (
              <TechBadge
                key={t.name}
                name={t.name}
                tagline={t.tagline}
                delay={10 + i * 6}
              />
            ))}
          </div>
        </div>

        <div
          style={{
            display: 'flex',
            gap: 120,
            justifyContent: 'center',
            alignItems: 'flex-start',
          }}
        >
          {stats.map((s, i) => (
            <BigNumber
              key={s.label}
              value={s.value}
              label={s.label}
              suffix={s.suffix}
              delay={70 + i * 15}
              duration={50}
            />
          ))}
        </div>
      </AbsoluteFill>
    </AbsoluteFill>
  );
};

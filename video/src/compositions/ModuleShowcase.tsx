import React from 'react';
import {
  AbsoluteFill,
  Audio,
  Sequence,
  interpolate,
  spring,
  staticFile,
  useCurrentFrame,
  useVideoConfig,
} from 'remotion';
import { GradientBackground } from '../components/GradientBackground';
import { Logo } from '../components/Logo';
import type { ModuleShowcase as ModuleShowcaseData } from '../data/moduleShowcases';
import { colors, fonts } from '../theme';

type Props = {
  module: ModuleShowcaseData;
  narrationFrom?: number;
};

// Single-composition template, 420 frames (14s @ 30fps):
//   0–40f   background reveal
//   20–80f  module name springs in
//   70–110f tagline fades in
//   120–300f three feature bullets stagger in (60 frames each)
//   320–420f outro CTA fades in
export const ModuleShowcase: React.FC<Props> = ({ module, narrationFrom = 18 }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const audioDisabled = process.env.REMOTION_DISABLE_AUDIO === '1';

  const nameSpring = spring({
    frame: frame - 20,
    fps,
    config: { damping: 14, mass: 0.85, stiffness: 140 },
  });

  const taglineOpacity = interpolate(frame, [70, 110], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const outroSpring = spring({
    frame: frame - 320,
    fps,
    config: { damping: 14, mass: 0.8, stiffness: 140 },
  });

  const musicFadeOutStart = 420 - 35;

  return (
    <AbsoluteFill>
      <GradientBackground variant="hero" accent={module.accent} />

      {/* accent halo behind name */}
      <AbsoluteFill
        style={{
          alignItems: 'center',
          justifyContent: 'center',
          opacity: nameSpring * 0.65,
          filter: 'blur(80px)',
          transform: `scale(${0.8 + 0.2 * nameSpring})`,
        }}
      >
        <div
          style={{
            width: 900,
            height: 900,
            borderRadius: '50%',
            background: `radial-gradient(circle, ${module.accent}88 0%, transparent 70%)`,
          }}
        />
      </AbsoluteFill>

      <AbsoluteFill
        style={{
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          padding: '0 120px',
          gap: 0,
        }}
      >
        <div
          style={{
            fontFamily: fonts.sans,
            fontSize: 18,
            fontWeight: 700,
            color: module.accent,
            letterSpacing: 4,
            textTransform: 'uppercase',
            opacity: nameSpring,
            marginBottom: 10,
          }}
        >
          SimpleModule Core
        </div>

        <div
          style={{
            fontFamily: fonts.sans,
            fontSize: 156,
            fontWeight: 800,
            letterSpacing: -4,
            color: colors.ink,
            opacity: nameSpring,
            transform: `translateY(${(1 - nameSpring) * 24}px)`,
            background: `linear-gradient(135deg, ${module.accent} 0%, ${colors.light} 100%)`,
            WebkitBackgroundClip: 'text',
            WebkitTextFillColor: 'transparent',
            backgroundClip: 'text',
            lineHeight: 1.02,
            textAlign: 'center',
          }}
        >
          {module.name}
        </div>

        <div
          style={{
            marginTop: 20,
            fontFamily: fonts.sans,
            fontSize: 34,
            fontWeight: 500,
            color: colors.inkMuted,
            letterSpacing: 0.3,
            opacity: taglineOpacity,
            textAlign: 'center',
          }}
        >
          {module.tagline}
        </div>

        <div
          style={{
            marginTop: 60,
            display: 'flex',
            flexDirection: 'column',
            gap: 18,
            width: '100%',
            maxWidth: 920,
          }}
        >
          {module.features.map((feat, i) => {
            const p = spring({
              frame: frame - (120 + i * 55),
              fps,
              config: { damping: 14, mass: 0.6, stiffness: 160 },
            });
            return (
              <div
                key={feat}
                style={{
                  opacity: p,
                  transform: `translateX(${(1 - p) * 28}px)`,
                  display: 'flex',
                  alignItems: 'center',
                  gap: 20,
                  padding: '20px 28px',
                  borderRadius: 14,
                  background: `${colors.bgPanel}cc`,
                  border: `1.5px solid ${module.accent}55`,
                  boxShadow: `0 0 ${32 * p}px ${module.accent}33`,
                  fontFamily: fonts.sans,
                  fontSize: 30,
                  fontWeight: 600,
                  color: colors.ink,
                }}
              >
                <span
                  style={{
                    color: module.accent,
                    fontWeight: 800,
                    fontSize: 26,
                    minWidth: 28,
                  }}
                >
                  {String(i + 1).padStart(2, '0')}
                </span>
                <span>{feat}</span>
              </div>
            );
          })}
        </div>

        <div
          style={{
            marginTop: 58,
            display: 'flex',
            alignItems: 'center',
            gap: 20,
            opacity: outroSpring,
            transform: `translateY(${(1 - outroSpring) * 14}px)`,
          }}
        >
          <Logo size={64} delay={320} />
          <div
            style={{
              fontFamily: fonts.mono,
              fontSize: 28,
              fontWeight: 600,
              color: colors.ink,
              padding: '10px 20px',
              background: `${module.accent}22`,
              border: `1px solid ${module.accent}88`,
              borderRadius: 10,
            }}
          >
            SimpleModule.{module.name}
          </div>
        </div>
      </AbsoluteFill>

      {audioDisabled ? null : (
        <>
          <Audio
            src={staticFile('background.mp3')}
            volume={(f) =>
              interpolate(
                f,
                [0, 25, musicFadeOutStart, 420],
                [0, 0.12, 0.12, 0],
                { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' },
              )
            }
          />
          <Sequence from={narrationFrom}>
            <Audio src={staticFile(`narration/modules/${module.id}.mp3`)} volume={1} />
          </Sequence>
        </>
      )}
    </AbsoluteFill>
  );
};

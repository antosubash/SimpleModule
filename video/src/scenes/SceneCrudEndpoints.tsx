import React from 'react';
import { AbsoluteFill, useCurrentFrame, useVideoConfig, spring, interpolate } from 'remotion';
import { GradientBackground } from '../components/GradientBackground';
import { CodeBlock } from '../components/CodeBlock';
import { ArrowFlow } from '../components/ArrowFlow';
import { crudEndpointsCode, crudVerbs } from '../data/codeSnippets';
import { colors, fonts } from '../theme';

export const SceneCrudEndpoints: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const sceneOpacity = interpolate(
    frame,
    [0, 10, 200, 210],
    [0, 1, 1, 0],
    { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' },
  );

  const captionOpacity = interpolate(frame, [170, 190], [0, 1], {
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
        }}
      >
        <Heading>
          <span style={{ color: colors.light }}>MapCrud&lt;&gt;</span> — batteries included
        </Heading>

        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            gap: 24,
            padding: '0 80px',
            width: '100%',
          }}
        >
          <div style={{ flex: 1.1, minWidth: 0 }}>
            <SectionLabel text="You write" />
            <CodeBlock
              code={crudEndpointsCode}
              fontSize={20}
              revealStart={8}
              revealDuration={45}
            />
          </div>

          <div style={{ width: 140, flexShrink: 0 }}>
            <ArrowFlow startFrame={60} duration={24} />
          </div>

          <div style={{ flex: 1.2, minWidth: 0 }}>
            <SectionLabel text="Five endpoints, auto-generated" />
            <div
              style={{
                display: 'flex',
                flexDirection: 'column',
                gap: 12,
              }}
            >
              {crudVerbs.map((v, i) => {
                const p = spring({
                  frame: frame - (90 + i * 10),
                  fps,
                  config: { damping: 14, mass: 0.6, stiffness: 180 },
                });
                return (
                  <VerbCard
                    key={i}
                    method={v.method}
                    path={v.path}
                    handler={v.handler}
                    progress={p}
                  />
                );
              })}
            </div>
          </div>
        </div>

        <div
          style={{
            marginTop: 40,
            opacity: captionOpacity,
            fontFamily: fonts.sans,
            fontSize: 30,
            fontWeight: 600,
            color: colors.inkMuted,
            letterSpacing: 0.3,
          }}
        >
          One line. Five endpoints. Batteries included.
        </div>
      </AbsoluteFill>
    </AbsoluteFill>
  );
};

const methodColor = (m: string) => {
  switch (m) {
    case 'GET': return '#34d399';
    case 'POST': return '#fde68a';
    case 'PUT': return '#7dd3fc';
    case 'DELETE': return '#fda4af';
    default: return '#a7f3d0';
  }
};

const VerbCard: React.FC<{
  method: string;
  path: string;
  handler: string;
  progress: number;
}> = ({ method, path, handler, progress }) => (
  <div
    style={{
      opacity: progress,
      transform: `translateX(${(1 - progress) * 24}px)`,
      display: 'flex',
      alignItems: 'center',
      gap: 16,
      padding: '14px 20px',
      background: '#071613',
      border: `1px solid ${colors.accent}55`,
      borderRadius: 12,
      fontFamily: fonts.mono,
      fontSize: 22,
    }}
  >
    <span
      style={{
        display: 'inline-block',
        minWidth: 84,
        padding: '4px 10px',
        borderRadius: 6,
        fontSize: 18,
        fontWeight: 700,
        background: `${methodColor(method)}22`,
        color: methodColor(method),
        textAlign: 'center',
        letterSpacing: 0.5,
      }}
    >
      {method}
    </span>
    <span style={{ color: colors.ink, flex: 1 }}>{path}</span>
    <span style={{ color: colors.inkMuted, fontSize: 18 }}>→ {handler}</span>
  </div>
);

const Heading: React.FC<{ children: React.ReactNode }> = ({ children }) => (
  <div
    style={{
      fontFamily: fonts.sans,
      fontSize: 46,
      fontWeight: 700,
      color: colors.ink,
      letterSpacing: -0.8,
      marginBottom: 32,
      textAlign: 'center',
    }}
  >
    {children}
  </div>
);

const SectionLabel: React.FC<{ text: string }> = ({ text }) => (
  <div
    style={{
      fontFamily: fonts.sans,
      fontSize: 15,
      fontWeight: 700,
      color: colors.inkMuted,
      textTransform: 'uppercase',
      letterSpacing: 1.8,
      marginBottom: 14,
      paddingLeft: 4,
    }}
  >
    {text}
  </div>
);

import React from 'react';
import { AbsoluteFill, useCurrentFrame, useVideoConfig, spring, interpolate } from 'remotion';
import { GradientBackground } from '../components/GradientBackground';
import { CodeBlock } from '../components/CodeBlock';
import { inertiaCode, eventBusCode } from '../data/codeSnippets';
import { colors, fonts } from '../theme';

export const SceneInertiaEvents: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const sceneOpacity = interpolate(
    frame,
    [0, 10, 170, 180],
    [0, 1, 1, 0],
    { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' },
  );

  const listeners = [0, 1, 2].map((i) =>
    spring({
      frame: frame - (95 + i * 7),
      fps,
      config: { damping: 13, mass: 0.5, stiffness: 180 },
    }),
  );

  const reactPage = spring({
    frame: frame - 50,
    fps,
    config: { damping: 13, mass: 0.6, stiffness: 170 },
  });

  const captionOpacity = interpolate(frame, [130, 150], [0, 1], {
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
          padding: '0 80px',
        }}
      >
        <Heading>Server-rendered React. Loosely coupled modules.</Heading>

        <div style={{ display: 'flex', gap: 32, width: '100%', maxWidth: 1700 }}>
          <Panel title="Inertia bridge">
            <CodeBlock
              code={inertiaCode}
              fontSize={22}
              revealStart={8}
              revealDuration={30}
            />
            <div
              style={{
                marginTop: 16,
                height: 120,
                borderRadius: 12,
                background: `linear-gradient(135deg, ${colors.primary}22 0%, ${colors.accent}33 100%)`,
                border: `1px solid ${colors.light}66`,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                fontFamily: fonts.sans,
                fontSize: 22,
                fontWeight: 600,
                color: colors.ink,
                opacity: reactPage,
                transform: `scale(${0.9 + 0.1 * reactPage})`,
              }}
            >
              <span style={{ color: colors.light, marginRight: 10 }}>⚛</span>
              Products/Browse — React page
            </div>
          </Panel>

          <Panel title="Event bus">
            <CodeBlock
              code={eventBusCode}
              fontSize={22}
              revealStart={35}
              revealDuration={30}
            />
            <div
              style={{
                marginTop: 16,
                display: 'flex',
                gap: 10,
                flexWrap: 'wrap',
                justifyContent: 'center',
              }}
            >
              {['Inventory', 'Email', 'AuditLogs'].map((name, i) => (
                <div
                  key={name}
                  style={{
                    opacity: listeners[i],
                    transform: `translateY(${(1 - listeners[i]) * 12}px)`,
                    padding: '12px 20px',
                    borderRadius: 10,
                    background: `linear-gradient(135deg, ${colors.accent}33 0%, ${colors.primary}33 100%)`,
                    border: `1px solid ${colors.teal}88`,
                    fontFamily: fonts.sans,
                    fontSize: 18,
                    fontWeight: 600,
                    color: colors.ink,
                    boxShadow: `0 0 ${16 * listeners[i]}px ${colors.teal}88`,
                  }}
                >
                  {name} handler
                </div>
              ))}
            </div>
          </Panel>
        </div>

        <div
          style={{
            marginTop: 32,
            opacity: captionOpacity,
            fontFamily: fonts.sans,
            fontSize: 28,
            fontWeight: 600,
            color: colors.inkMuted,
            letterSpacing: 0.3,
            textAlign: 'center',
          }}
        >
          One page, one event — the framework handles the wiring.
        </div>
      </AbsoluteFill>
    </AbsoluteFill>
  );
};

const Heading: React.FC<{ children: React.ReactNode }> = ({ children }) => (
  <div
    style={{
      fontFamily: fonts.sans,
      fontSize: 42,
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

const Panel: React.FC<{ title: string; children: React.ReactNode }> = ({
  title,
  children,
}) => (
  <div
    style={{
      flex: 1,
      padding: 24,
      borderRadius: 18,
      background: `linear-gradient(160deg, ${colors.bgPanel} 0%, #03201a 100%)`,
      border: `1px solid ${colors.accent}55`,
    }}
  >
    <div
      style={{
        fontFamily: fonts.sans,
        fontSize: 18,
        fontWeight: 700,
        color: colors.inkMuted,
        textTransform: 'uppercase',
        letterSpacing: 2,
        marginBottom: 14,
      }}
    >
      {title}
    </div>
    {children}
  </div>
);

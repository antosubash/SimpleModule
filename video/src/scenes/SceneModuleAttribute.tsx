import React from 'react';
import { AbsoluteFill, useCurrentFrame, interpolate } from 'remotion';
import { GradientBackground } from '../components/GradientBackground';
import { CodeBlock } from '../components/CodeBlock';
import { SplitCodePair } from '../components/SplitCodePair';
import { moduleAttrUser, moduleAttrGenerated } from '../data/codeSnippets';
import { colors, fonts } from '../theme';

export const SceneModuleAttribute: React.FC = () => {
  const frame = useCurrentFrame();

  const captionOpacity = interpolate(frame, [120, 140], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <AbsoluteFill>
      <GradientBackground variant="dark" />
      <AbsoluteFill
        style={{
          flexDirection: 'column',
          justifyContent: 'center',
          alignItems: 'center',
        }}
      >
        <Heading>
          <span style={{ color: colors.light }}>[Module]</span> — compile-time discovery
        </Heading>

        <div style={{ width: '100%' }}>
          <SplitCodePair
            leftLabel="You write"
            rightLabel="Generated at compile time"
            arrowFrame={50}
            left={
              <CodeBlock
                code={moduleAttrUser}
                fontSize={20}
                revealStart={10}
                revealDuration={35}
              />
            }
            right={
              <CodeBlock
                code={moduleAttrGenerated}
                fontSize={18}
                revealStart={70}
                revealDuration={50}
              />
            }
          />
        </div>

        <div
          style={{
            marginTop: 48,
            opacity: captionOpacity,
            fontFamily: fonts.sans,
            fontSize: 30,
            fontWeight: 600,
            color: colors.inkMuted,
            letterSpacing: 0.3,
          }}
        >
          No reflection. No manual registration.
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

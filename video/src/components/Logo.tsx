import React from 'react';
import { useCurrentFrame, useVideoConfig, spring, interpolate } from 'remotion';
import { colors } from '../theme';

type Props = {
  size?: number;
  delay?: number;
};

export const Logo: React.FC<Props> = ({ size = 360, delay = 0 }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const blockProgress = (i: number) =>
    spring({
      frame: frame - delay - i * 4,
      fps,
      config: { damping: 14, mass: 0.6, stiffness: 180 },
    });

  const lineOpacity = interpolate(
    frame - delay - 30,
    [0, 20],
    [0, 1],
    { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' },
  );

  const blocks = [
    { x: 96, y: 96, fill: 'url(#grad1)' },
    { x: 276, y: 96, fill: 'url(#grad2)' },
    { x: 96, y: 276, fill: 'url(#grad2)' },
    { x: 276, y: 276, fill: 'url(#grad3)' },
  ];

  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 512 512"
      width={size}
      height={size}
    >
      <defs>
        <linearGradient id="grad1" x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" stopColor="#059669" />
          <stop offset="100%" stopColor="#0d9488" />
        </linearGradient>
        <linearGradient id="grad2" x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" stopColor="#34d399" />
          <stop offset="100%" stopColor="#2dd4bf" />
        </linearGradient>
        <linearGradient id="grad3" x1="0%" y1="100%" x2="100%" y2="0%">
          <stop offset="0%" stopColor="#047857" />
          <stop offset="100%" stopColor="#0f766e" />
        </linearGradient>
      </defs>

      {blocks.map((b, i) => {
        const p = blockProgress(i);
        const cx = b.x + 70;
        const cy = b.y + 70;
        return (
          <g key={i} transform={`translate(${cx} ${cy}) scale(${p}) translate(${-cx} ${-cy})`} opacity={p}>
            <rect x={b.x} y={b.y} width="140" height="140" rx="20" fill={b.fill} />
          </g>
        );
      })}

      <g opacity={lineOpacity}>
        <line x1="236" y1="166" x2="276" y2="166" stroke={colors.lighter} strokeWidth="3" strokeLinecap="round" strokeDasharray="6,4" />
        <line x1="236" y1="346" x2="276" y2="346" stroke={colors.lighter} strokeWidth="3" strokeLinecap="round" strokeDasharray="6,4" />
        <line x1="166" y1="236" x2="166" y2="276" stroke={colors.lighter} strokeWidth="3" strokeLinecap="round" strokeDasharray="6,4" />
        <line x1="346" y1="236" x2="346" y2="276" stroke={colors.lighter} strokeWidth="3" strokeLinecap="round" strokeDasharray="6,4" />
        <circle cx="256" cy="166" r="5" fill={colors.lighter} />
        <circle cx="256" cy="346" r="5" fill={colors.lighter} />
        <circle cx="166" cy="256" r="5" fill={colors.lighter} />
        <circle cx="346" cy="256" r="5" fill={colors.lighter} />
      </g>
    </svg>
  );
};

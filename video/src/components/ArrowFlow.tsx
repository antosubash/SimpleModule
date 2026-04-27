import React from 'react';
import { useCurrentFrame, interpolate } from 'remotion';
import { colors } from '../theme';

type Props = {
  startFrame?: number;
  duration?: number;
};

export const ArrowFlow: React.FC<Props> = ({ startFrame = 0, duration = 20 }) => {
  const frame = useCurrentFrame();
  const progress = interpolate(
    frame,
    [startFrame, startFrame + duration],
    [0, 1],
    { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' },
  );

  const dash = 120;
  const offset = dash * (1 - progress);

  return (
    <svg
      width="100%"
      height="60"
      viewBox="0 0 200 60"
      style={{ display: 'block' }}
    >
      <defs>
        <linearGradient id="flowGrad" x1="0%" y1="0%" x2="100%" y2="0%">
          <stop offset="0%" stopColor={colors.light} />
          <stop offset="100%" stopColor={colors.teal} />
        </linearGradient>
      </defs>
      <line
        x1="10"
        y1="30"
        x2="170"
        y2="30"
        stroke="url(#flowGrad)"
        strokeWidth="4"
        strokeLinecap="round"
        strokeDasharray={dash}
        strokeDashoffset={offset}
      />
      <polygon
        points="170,20 190,30 170,40"
        fill={colors.teal}
        opacity={progress}
      />
    </svg>
  );
};

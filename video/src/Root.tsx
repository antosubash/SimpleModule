import React from 'react';
import { Composition } from 'remotion';
import { Video } from './Video';

export const Root: React.FC = () => {
  return (
    <>
      <Composition
        id="SimpleModule"
        component={Video}
        durationInFrames={1500}
        fps={30}
        width={1920}
        height={1080}
      />
    </>
  );
};

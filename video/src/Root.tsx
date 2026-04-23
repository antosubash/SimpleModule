import React from 'react';
import { Composition } from 'remotion';
import { Video } from './Video';
import { ModuleShowcase } from './compositions/ModuleShowcase';
import { moduleShowcases } from './data/moduleShowcases';

export const Root: React.FC = () => {
  return (
    <>
      <Composition
        id="SimpleModule"
        component={Video}
        durationInFrames={1171}
        fps={30}
        width={1920}
        height={1080}
      />

      {moduleShowcases.map((mod) => (
        <Composition
          key={mod.id}
          id={`Module-${mod.id}`}
          component={ModuleShowcase}
          durationInFrames={420}
          fps={30}
          width={1920}
          height={1080}
          defaultProps={{ module: mod }}
        />
      ))}
    </>
  );
};

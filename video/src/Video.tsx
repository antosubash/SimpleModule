import React from 'react';
import { AbsoluteFill, Audio, Series, staticFile, interpolate } from 'remotion';
import { SceneOpening } from './scenes/SceneOpening';
import { SceneProblemSolution } from './scenes/SceneProblemSolution';
import { SceneModuleAttribute } from './scenes/SceneModuleAttribute';
import { SceneEndpoint } from './scenes/SceneEndpoint';
import { SceneCrudEndpoints } from './scenes/SceneCrudEndpoints';
import { SceneInertiaEvents } from './scenes/SceneInertiaEvents';
import { SceneModules } from './scenes/SceneModules';
import { SceneTechStats } from './scenes/SceneTechStats';
import { SceneCTA } from './scenes/SceneCTA';
import './fonts';

// Audio is opt-in: once you drop a track at public/background.mp3,
// set REMOTION_ENABLE_AUDIO=1 before running `npm run render`.
const audioEnabled = process.env.REMOTION_ENABLE_AUDIO === '1';

export const Video: React.FC = () => {
  return (
    <AbsoluteFill>
      <Series>
        <Series.Sequence durationInFrames={120}>
          <SceneOpening />
        </Series.Sequence>
        <Series.Sequence durationInFrames={150}>
          <SceneProblemSolution />
        </Series.Sequence>
        <Series.Sequence durationInFrames={180}>
          <SceneModuleAttribute />
        </Series.Sequence>
        <Series.Sequence durationInFrames={180}>
          <SceneEndpoint />
        </Series.Sequence>
        <Series.Sequence durationInFrames={210}>
          <SceneCrudEndpoints />
        </Series.Sequence>
        <Series.Sequence durationInFrames={180}>
          <SceneInertiaEvents />
        </Series.Sequence>
        <Series.Sequence durationInFrames={180}>
          <SceneModules />
        </Series.Sequence>
        <Series.Sequence durationInFrames={180}>
          <SceneTechStats />
        </Series.Sequence>
        <Series.Sequence durationInFrames={120}>
          <SceneCTA />
        </Series.Sequence>
      </Series>

      {audioEnabled ? (
        <Audio
          src={staticFile('background.mp3')}
          volume={(f) =>
            interpolate(
              f,
              [0, 30, 1440, 1500],
              [0, 0.55, 0.55, 0],
              { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' },
            )
          }
        />
      ) : null}
    </AbsoluteFill>
  );
};

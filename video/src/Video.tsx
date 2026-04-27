import React from 'react';
import { AbsoluteFill, Audio, Sequence, staticFile, interpolate } from 'remotion';
import { TransitionSeries, linearTiming } from '@remotion/transitions';
import { fade } from '@remotion/transitions/fade';
import { slide } from '@remotion/transitions/slide';
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

// Audio toggles: both background music and narration play by default.
// Set REMOTION_DISABLE_AUDIO=1 to render a silent clip.
const audioDisabled = process.env.REMOTION_DISABLE_AUDIO === '1';

const TRANSITION_FRAMES = 18;

// Narration clips with their start frames. Scene start frames (with 18-frame
// transition overlap): S1=0, S2=77, S3=199, S4=341, S5=473, S6=640, S7=782,
// S8=914, S9=1071. Each voice clip is offset ~10–15 frames into its scene
// to let visuals settle before speech begins.
const narration = [
  { src: 'narration/s1.mp3', from: 5 },
  { src: 'narration/s2.mp3', from: 90 },
  { src: 'narration/s3.mp3', from: 215 },
  { src: 'narration/s4.mp3', from: 355 },
  { src: 'narration/s5.mp3', from: 488 },
  { src: 'narration/s6.mp3', from: 655 },
  { src: 'narration/s7.mp3', from: 795 },
  { src: 'narration/s8.mp3', from: 925 },
  { src: 'narration/s9.mp3', from: 1080 },
];

export const Video: React.FC = () => {
  // Total = sum of sequence lengths (1315) - 8 × TRANSITION_FRAMES overlap = 1171.
  const totalFrames = 1315 - 8 * TRANSITION_FRAMES;
  const musicFadeOutStart = totalFrames - 45;

  return (
    <AbsoluteFill>
      <TransitionSeries>
        <TransitionSeries.Sequence durationInFrames={95}>
          <SceneOpening />
        </TransitionSeries.Sequence>

        <TransitionSeries.Transition
          presentation={fade()}
          timing={linearTiming({ durationInFrames: TRANSITION_FRAMES })}
        />

        <TransitionSeries.Sequence durationInFrames={140}>
          <SceneProblemSolution />
        </TransitionSeries.Sequence>

        <TransitionSeries.Transition
          presentation={slide({ direction: 'from-right' })}
          timing={linearTiming({ durationInFrames: TRANSITION_FRAMES })}
        />

        <TransitionSeries.Sequence durationInFrames={160}>
          <SceneModuleAttribute />
        </TransitionSeries.Sequence>

        <TransitionSeries.Transition
          presentation={fade()}
          timing={linearTiming({ durationInFrames: TRANSITION_FRAMES })}
        />

        <TransitionSeries.Sequence durationInFrames={150}>
          <SceneEndpoint />
        </TransitionSeries.Sequence>

        <TransitionSeries.Transition
          presentation={slide({ direction: 'from-right' })}
          timing={linearTiming({ durationInFrames: TRANSITION_FRAMES })}
        />

        <TransitionSeries.Sequence durationInFrames={185}>
          <SceneCrudEndpoints />
        </TransitionSeries.Sequence>

        <TransitionSeries.Transition
          presentation={fade()}
          timing={linearTiming({ durationInFrames: TRANSITION_FRAMES })}
        />

        <TransitionSeries.Sequence durationInFrames={160}>
          <SceneInertiaEvents />
        </TransitionSeries.Sequence>

        <TransitionSeries.Transition
          presentation={slide({ direction: 'from-bottom' })}
          timing={linearTiming({ durationInFrames: TRANSITION_FRAMES })}
        />

        <TransitionSeries.Sequence durationInFrames={150}>
          <SceneModules />
        </TransitionSeries.Sequence>

        <TransitionSeries.Transition
          presentation={fade()}
          timing={linearTiming({ durationInFrames: TRANSITION_FRAMES })}
        />

        <TransitionSeries.Sequence durationInFrames={175}>
          <SceneTechStats />
        </TransitionSeries.Sequence>

        <TransitionSeries.Transition
          presentation={fade()}
          timing={linearTiming({ durationInFrames: TRANSITION_FRAMES })}
        />

        <TransitionSeries.Sequence durationInFrames={100}>
          <SceneCTA />
        </TransitionSeries.Sequence>
      </TransitionSeries>

      {audioDisabled ? null : (
        <>
          <Audio
            src={staticFile('background.mp3')}
            volume={(f) =>
              interpolate(
                f,
                [0, 25, musicFadeOutStart, totalFrames],
                [0, 0.14, 0.14, 0],
                { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' },
              )
            }
          />
          {narration.map((n) => (
            <Sequence key={n.src} from={n.from}>
              <Audio src={staticFile(n.src)} volume={1} />
            </Sequence>
          ))}
        </>
      )}
    </AbsoluteFill>
  );
};

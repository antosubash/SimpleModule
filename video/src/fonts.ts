import { loadFont as loadInter } from '@remotion/google-fonts/Inter';
import { loadFont as loadMono } from '@remotion/google-fonts/JetBrainsMono';

export const inter = loadInter('normal', {
  weights: ['400', '500', '600', '700', '800'],
});

export const jetbrains = loadMono('normal', {
  weights: ['400', '500', '700'],
});

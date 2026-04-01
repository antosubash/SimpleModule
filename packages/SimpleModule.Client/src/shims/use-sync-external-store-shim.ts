// ESM shim for use-sync-external-store/shim
// React 18+ includes useSyncExternalStore natively, so the CJS shim
// (which uses require('react')) is unnecessary and breaks Rolldown's
// external CJS handling in Vite 8 library mode.
export { useSyncExternalStore } from 'react';

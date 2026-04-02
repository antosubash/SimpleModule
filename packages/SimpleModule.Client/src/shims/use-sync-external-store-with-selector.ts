// ESM shim for use-sync-external-store/shim/with-selector
// React 19 includes useSyncExternalStore natively; this avoids the CJS
// require('react') call that breaks in Rolldown/Vite 8 library mode.
import { useCallback, useRef, useSyncExternalStore } from 'react';

export function useSyncExternalStoreWithSelector<Snapshot, Selection>(
  subscribe: (onStoreChange: () => void) => () => void,
  getSnapshot: () => Snapshot,
  _getServerSnapshot: undefined | null | (() => Snapshot),
  selector: (snapshot: Snapshot) => Selection,
  isEqual?: (a: Selection, b: Selection) => boolean,
): Selection {
  const prevRef = useRef<{ snapshot: Snapshot; selection: Selection } | null>(null);

  const getSelection = useCallback(() => {
    const nextSnapshot = getSnapshot();
    const prev = prevRef.current;

    if (prev !== null && Object.is(prev.snapshot, nextSnapshot)) {
      return prev.selection;
    }

    const nextSelection = selector(nextSnapshot);

    if (prev !== null && isEqual?.(prev.selection, nextSelection)) {
      // Keep the previous reference if isEqual says they match.
      prevRef.current = { snapshot: nextSnapshot, selection: prev.selection };
      return prev.selection;
    }

    prevRef.current = { snapshot: nextSnapshot, selection: nextSelection };
    return nextSelection;
  }, [getSnapshot, selector, isEqual]);

  return useSyncExternalStore(subscribe, getSelection);
}

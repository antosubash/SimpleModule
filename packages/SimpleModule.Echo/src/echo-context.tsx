import { createContext, type ReactNode, useContext, useEffect, useMemo, useState } from 'react';
import { Echo, type EchoOptions } from './echo';
import type { EchoStatus } from './types';

const EchoContext = createContext<Echo | null>(null);

interface EchoProviderProps extends EchoOptions {
  children: ReactNode;
  /**
   * Provide an existing Echo instance (useful for testing). When omitted,
   * the provider lazily creates one from the rest of the options.
   */
  echo?: Echo;
  /** Connect immediately on mount. Defaults to true. */
  autoStart?: boolean;
}

/**
 * Top-level provider that owns one Echo instance for the React tree. Mounting
 * many providers is fine but wasteful — each one opens its own WebSocket.
 */
export function EchoProvider({ children, echo, autoStart = true, ...options }: EchoProviderProps) {
  // Options are captured at mount; re-creating Echo on every options identity
  // change would tear down the WebSocket on each render. Callers who need to
  // swap config should pass a stable `echo` prop instead.
  // biome-ignore lint/correctness/useExhaustiveDependencies: intentional one-shot init
  const instance = useMemo(() => echo ?? new Echo(options), [echo]);

  useEffect(() => {
    if (!autoStart) return;
    void instance.start().catch(() => {
      // Surface as status; an unhandled rejection here would log to the
      // console twice in dev. Consumers observe failures via useEchoStatus.
    });
    return () => {
      void instance.stop();
    };
  }, [instance, autoStart]);

  return <EchoContext.Provider value={instance}>{children}</EchoContext.Provider>;
}

export function useEcho(): Echo {
  const ctx = useContext(EchoContext);
  if (!ctx) {
    throw new Error('useEcho() requires <EchoProvider> in the tree');
  }
  return ctx;
}

export function useEchoStatus(): EchoStatus {
  const echo = useEcho();
  const [status, setStatus] = useState<EchoStatus>(echo.state);
  useEffect(() => echo.onStatus(setStatus), [echo]);
  return status;
}

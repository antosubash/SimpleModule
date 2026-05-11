import { useEffect, useRef, useState } from 'react';
import { useEcho } from './echo-context';
import type { EventHandler, PresenceChange, PresenceMember } from './types';

/**
 * Subscribe to a single broadcast event on `channel`. The handler is held in
 * a ref so the consumer can use freshly-captured state without forcing a
 * re-subscribe every render. Pass `undefined` to `channel` to disable
 * subscription (useful while loading the channel identifier).
 */
export function useEvent<T = unknown>(
  channel: string | undefined | null,
  event: string,
  handler: EventHandler<T>,
): void {
  const handlerRef = useRef(handler);
  handlerRef.current = handler;

  const echo = useEcho();

  useEffect(() => {
    if (!channel) return;
    const dispose = echo.on<T>(channel, event, (payload, envelope) =>
      handlerRef.current(payload, envelope),
    );
    return dispose;
  }, [echo, channel, event]);
}

/**
 * Subscribe to a presence channel and re-render the consumer whenever the
 * roster changes. Optional `handler` is called for every update — `change`
 * is `null` for the initial / post-reconnect snapshot so callers can tell a
 * fresh roster apart from a join/leave delta.
 */
export function usePresence(
  channel: string | undefined | null,
  handler?: (members: PresenceMember[], change: PresenceChange | null) => void,
): PresenceMember[] {
  const handlerRef = useRef(handler);
  handlerRef.current = handler;

  const echo = useEcho();
  const [members, setMembers] = useState<PresenceMember[]>([]);

  useEffect(() => {
    if (!channel) {
      setMembers([]);
      return;
    }
    const dispose = echo.onPresence(channel, (change, nextMembers) => {
      setMembers(nextMembers);
      handlerRef.current?.(nextMembers, change);
    });
    return () => {
      dispose();
      setMembers([]);
    };
  }, [echo, channel]);

  return members;
}

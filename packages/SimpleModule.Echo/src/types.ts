/**
 * Wire format the broadcast hub sends to clients. The shape mirrors the
 * server's `BroadcastEnvelope` record — keep these in lockstep when either
 * side changes.
 */
export interface BroadcastEnvelope<TPayload = unknown> {
  channel: string;
  event: string;
  payload: TPayload;
}

export interface PresenceMember {
  userId: string;
  info?: Record<string, string>;
}

export type PresenceChangeKind = 'Joined' | 'Left';

export interface PresenceChange {
  channel: string;
  kind: PresenceChangeKind;
  member: PresenceMember;
  members: PresenceMember[];
}

export interface SubscribeResult {
  authorized: boolean;
  reason: string | null;
  members: PresenceMember[];
}

export type EventHandler<T = unknown> = (payload: T, envelope: BroadcastEnvelope<T>) => void;

/**
 * Presence notifications. `change` is non-null for real join/leave deltas
 * and `null` for snapshot calls (initial subscription, post-reconnect
 * re-seed) so consumers can update their roster without confusing a fresh
 * snapshot for a delta.
 */
export type PresenceHandler = (
  change: PresenceChange | null,
  members: PresenceMember[],
  channel: string,
) => void;

export type EchoStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

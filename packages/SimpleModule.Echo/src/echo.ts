import {
  type HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import type {
  BroadcastEnvelope,
  EchoStatus,
  EventHandler,
  PresenceChange,
  PresenceHandler,
  PresenceMember,
  SubscribeResult,
} from './types';

// Hub method names — must match SimpleModule.Hosting.Broadcasting.BroadcastClientMethods
// (server) and the [Hub] method signatures on BroadcastHub.
const CLIENT_METHOD_BROADCAST = 'broadcast';
const CLIENT_METHOD_PRESENCE = 'presence';
const HUB_METHOD_SUBSCRIBE = 'Subscribe';
const HUB_METHOD_UNSUBSCRIBE = 'Unsubscribe';

export interface EchoOptions {
  /** Hub URL. Defaults to `/hub/broadcast` (same-origin). */
  url?: string;
  /** Reconnect backoff (ms) tried in order, then repeated. Defaults to 0, 2s, 10s, 30s. */
  reconnectDelays?: number[];
  /** Console log level passed through to the SignalR client. */
  logLevel?: LogLevel;
}

/**
 * Single-connection, channel-multiplexed broadcasting client. Channels are
 * reference-counted: subscribing N times costs one network roundtrip on the
 * first call and one unsubscribe on the last `off`. All event/presence
 * dispatch happens in user code — Echo itself only owns the connection and
 * the listener map.
 */
export class Echo {
  private connection: HubConnection;
  private url: string;
  private status: EchoStatus = 'disconnected';
  private statusListeners = new Set<(status: EchoStatus) => void>();
  // channel -> event -> listeners
  private listeners = new Map<string, Map<string, Set<EventHandler>>>();
  private presenceListeners = new Map<string, Set<PresenceHandler>>();
  private presenceState = new Map<string, PresenceMember[]>();
  private refCounts = new Map<string, number>();
  // Promise-per-channel for any subscribe currently in flight. Concurrent
  // callers await the same promise instead of returning a stale cached
  // roster while the first subscribe is still negotiating with the hub.
  private inFlight = new Map<string, Promise<SubscribeResult>>();
  private startPromise: Promise<void> | null = null;

  constructor(options: EchoOptions = {}) {
    this.url = options.url ?? '/hub/broadcast';
    const reconnect = options.reconnectDelays ?? [0, 2000, 10_000, 30_000];

    this.connection = new HubConnectionBuilder()
      .withUrl(this.url, { withCredentials: true })
      .withAutomaticReconnect(reconnect)
      .configureLogging(options.logLevel ?? LogLevel.Warning)
      .build();

    this.connection.on(CLIENT_METHOD_BROADCAST, (envelope: BroadcastEnvelope) => {
      this.dispatchEvent(envelope);
    });

    this.connection.on(CLIENT_METHOD_PRESENCE, (change: PresenceChange) => {
      this.dispatchPresence(change);
    });

    this.connection.onreconnecting(() => this.setStatus('reconnecting'));
    this.connection.onreconnected(() => {
      this.setStatus('connected');
      void this.resubscribeAll();
    });
    this.connection.onclose(() => this.setStatus('disconnected'));
  }

  get state(): EchoStatus {
    return this.status;
  }

  onStatus(listener: (status: EchoStatus) => void): () => void {
    this.statusListeners.add(listener);
    listener(this.status);
    return () => this.statusListeners.delete(listener);
  }

  /**
   * Connects (idempotent — concurrent callers share one start promise). It is
   * safe to call from React effects; if the hub is already started this
   * resolves immediately.
   */
  async start(): Promise<void> {
    if (this.connection.state === HubConnectionState.Connected) {
      return;
    }
    if (this.startPromise) {
      return this.startPromise;
    }
    this.setStatus('connecting');
    this.startPromise = this.connection.start().finally(() => {
      this.startPromise = null;
    });
    try {
      await this.startPromise;
      this.setStatus('connected');
    } catch (e) {
      this.setStatus('disconnected');
      throw e;
    }
  }

  async stop(): Promise<void> {
    await this.connection.stop();
  }

  /**
   * Subscribes to <channel> on the hub. Multiple calls collapse into one
   * server-side subscription; pair every call with `unsubscribe` (or the
   * disposer returned by `on`). Concurrent calls share the in-flight
   * subscribe promise so every caller sees the same authoritative roster.
   */
  async subscribe(channel: string): Promise<SubscribeResult> {
    const inFlight = this.inFlight.get(channel);
    if (inFlight) {
      this.refCounts.set(channel, (this.refCounts.get(channel) ?? 0) + 1);
      return inFlight;
    }

    const current = this.refCounts.get(channel) ?? 0;
    if (current > 0) {
      this.refCounts.set(channel, current + 1);
      return {
        authorized: true,
        reason: null,
        members: this.presenceState.get(channel) ?? [],
      };
    }

    this.refCounts.set(channel, 1);
    const promise = (async () => {
      try {
        await this.start();
        return await this.invokeSubscribe(channel);
      } catch (e) {
        // Roll back so a later subscribe retries the hub call cleanly.
        const c = this.refCounts.get(channel) ?? 0;
        if (c <= 1) {
          this.refCounts.delete(channel);
        } else {
          this.refCounts.set(channel, c - 1);
        }
        throw e;
      } finally {
        this.inFlight.delete(channel);
      }
    })();

    this.inFlight.set(channel, promise);
    const result = await promise;
    if (!result.authorized) {
      // Hub denied — undo the +1 we optimistically added so subsequent
      // unsubscribes don't underflow the count and a re-subscribe can retry.
      const c = this.refCounts.get(channel) ?? 0;
      if (c <= 1) {
        this.refCounts.delete(channel);
      } else {
        this.refCounts.set(channel, c - 1);
      }
    }
    return result;
  }

  async unsubscribe(channel: string): Promise<void> {
    const current = this.refCounts.get(channel) ?? 0;
    if (current <= 1) {
      this.refCounts.delete(channel);
      this.presenceState.delete(channel);
      if (this.connection.state === HubConnectionState.Connected) {
        await this.connection.invoke(HUB_METHOD_UNSUBSCRIBE, channel);
      }
    } else {
      this.refCounts.set(channel, current - 1);
    }
  }

  /**
   * Register a handler for `event` on `channel`. Subscribes on first
   * listener, unsubscribes on last. Returns a disposer.
   */
  on<T = unknown>(channel: string, event: string, handler: EventHandler<T>): () => void {
    let byEvent = this.listeners.get(channel);
    if (!byEvent) {
      byEvent = new Map();
      this.listeners.set(channel, byEvent);
    }
    let set = byEvent.get(event);
    if (!set) {
      set = new Set();
      byEvent.set(event, set);
    }
    set.add(handler as EventHandler);

    void this.subscribe(channel);

    return () => {
      set?.delete(handler as EventHandler);
      if (set?.size === 0) {
        byEvent?.delete(event);
      }
      if (byEvent?.size === 0) {
        this.listeners.delete(channel);
      }
      void this.unsubscribe(channel);
    };
  }

  /**
   * Register a presence handler for `channel`. The handler receives a `null`
   * change argument on the initial snapshot so callers can distinguish it
   * from real join/leave deltas. Pairs with `on` semantics.
   */
  onPresence(channel: string, handler: PresenceHandler): () => void {
    let set = this.presenceListeners.get(channel);
    if (!set) {
      set = new Set();
      this.presenceListeners.set(channel, set);
    }
    set.add(handler);

    void this.subscribe(channel).then((result) => {
      if (result.authorized) {
        this.presenceState.set(channel, result.members);
        handler(null, result.members, channel);
      }
    });

    return () => {
      set?.delete(handler);
      if (set?.size === 0) {
        this.presenceListeners.delete(channel);
      }
      void this.unsubscribe(channel);
    };
  }

  private async resubscribeAll(): Promise<void> {
    // Sequential so we don't flood the hub on reconnect; presence channels
    // re-emit their roster snapshot through dispatchPresence so listeners
    // see the post-reconnect membership without an extra delta event.
    for (const channel of [...this.refCounts.keys()]) {
      try {
        const result = await this.invokeSubscribe(channel);
        if (result.authorized) {
          this.presenceState.set(channel, result.members);
          this.broadcastPresenceSnapshot(channel, result.members);
        }
      } catch (e) {
        console.error('echo re-subscribe failed', channel, e);
      }
    }
  }

  private async invokeSubscribe(channel: string): Promise<SubscribeResult> {
    if (this.connection.state !== HubConnectionState.Connected) {
      return { authorized: false, reason: 'not connected', members: [] };
    }
    const result = await this.connection.invoke<SubscribeResult>(HUB_METHOD_SUBSCRIBE, channel);
    if (result.authorized) {
      this.presenceState.set(channel, result.members);
    }
    return result;
  }

  private dispatchEvent(envelope: BroadcastEnvelope): void {
    const byEvent = this.listeners.get(envelope.channel);
    if (!byEvent) return;
    const set = byEvent.get(envelope.event);
    if (!set) return;
    for (const listener of set) {
      try {
        listener(envelope.payload, envelope);
      } catch (e) {
        console.error('echo listener failed', e);
      }
    }
  }

  private dispatchPresence(change: PresenceChange): void {
    this.presenceState.set(change.channel, change.members);
    const set = this.presenceListeners.get(change.channel);
    if (!set) return;
    for (const listener of set) {
      try {
        listener(change, change.members, change.channel);
      } catch (e) {
        console.error('echo presence listener failed', e);
      }
    }
  }

  private broadcastPresenceSnapshot(channel: string, members: PresenceMember[]): void {
    const set = this.presenceListeners.get(channel);
    if (!set) return;
    for (const listener of set) {
      try {
        listener(null, members, channel);
      } catch (e) {
        console.error('echo presence snapshot listener failed', e);
      }
    }
  }

  private setStatus(status: EchoStatus): void {
    if (this.status === status) return;
    this.status = status;
    for (const listener of this.statusListeners) {
      try {
        listener(status);
      } catch (e) {
        console.error('echo status listener failed', e);
      }
    }
  }
}

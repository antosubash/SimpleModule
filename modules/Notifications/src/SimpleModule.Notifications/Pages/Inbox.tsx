import { router } from '@inertiajs/react';
import { Badge, Button, Card, CardContent, EmptyState, PageShell } from '@simplemodule/ui';
import type { Notification } from '../types';

interface Props {
  items: Notification[];
  totalCount: number;
  unreadCount: number;
}

function formatDate(value: string): string {
  try {
    return new Date(value).toLocaleString();
  } catch {
    return value;
  }
}

export default function Inbox({ items, totalCount, unreadCount }: Props) {
  // These are plain JSON API endpoints that return 204 No Content, not Inertia
  // responses — call them with fetch and refresh the props on success. (router.post
  // expects an Inertia-protocol response and treats a 204 as a server error.)
  const postAndReload = async (url: string) => {
    const res = await fetch(url, { method: 'POST', credentials: 'same-origin' });
    if (!res.ok) {
      console.error(`Notification action failed: ${res.status} ${url}`);
      return;
    }
    router.reload({ only: ['items', 'unreadCount'] });
  };

  const markRead = (id: string) => postAndReload(`/api/notifications/${id}/read`);

  const markAllRead = () => postAndReload('/api/notifications/read-all');

  return (
    <PageShell
      className="space-y-4 sm:space-y-6"
      title="Notifications"
      description={`${totalCount} total · ${unreadCount} unread`}
    >
      <div className="flex justify-end">
        <Button onClick={markAllRead} disabled={unreadCount === 0} variant="secondary">
          Mark all read
        </Button>
      </div>

      {items.length === 0 ? (
        <Card>
          <CardContent>
            <EmptyState title="You're all caught up." />
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-2">
          {items.map((n) => (
            <Card key={n.id}>
              <CardContent className="flex items-start justify-between gap-4 p-4">
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2">
                    {!n.readAt && <Badge variant="info">New</Badge>}
                    <Badge variant="default">{n.channel}</Badge>
                    <span className="text-xs text-text-muted">{n.type}</span>
                  </div>
                  {n.title && <p className="mt-1 font-medium">{n.title}</p>}
                  {n.body && <p className="mt-1 text-sm text-text-muted">{n.body}</p>}
                  <p className="mt-1 text-xs text-text-muted">{formatDate(n.createdAt)}</p>
                </div>
                {!n.readAt && (
                  <Button onClick={() => markRead(n.id)} variant="ghost" size="sm">
                    Mark read
                  </Button>
                )}
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </PageShell>
  );
}

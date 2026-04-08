import { router } from '@inertiajs/react';
import { Button, Card, CardContent, Input, PageShell } from '@simplemodule/ui';
import { useState } from 'react';

type Conversation = {
  id: string;
  title: string;
  agentName: string;
  updatedAt: string;
};

type AgentInfo = {
  name: string;
  description: string;
};

type Props = {
  conversations: Conversation[];
  agents: AgentInfo[];
};

export default function Browse({ conversations, agents }: Props) {
  const [title, setTitle] = useState('');
  const [agentName, setAgentName] = useState(agents[0]?.name ?? '');
  const [creating, setCreating] = useState(false);

  async function createConversation() {
    if (!agentName) return;
    setCreating(true);
    try {
      const response = await fetch('/api/chat/conversations', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ agentName, title: title || null }),
      });
      if (!response.ok) {
        throw new Error(`Failed to create conversation: ${response.status}`);
      }
      const conversation = (await response.json()) as Conversation;
      router.visit(`/chat/${conversation.id}`);
    } finally {
      setCreating(false);
    }
  }

  return (
    <PageShell title="Chat" description="Conversations with AI agents">
      <Card>
        <CardContent className="flex flex-col gap-3 p-4">
          <div className="text-sm font-medium">Start a new conversation</div>
          <select
            className="border rounded px-2 py-1 bg-background"
            value={agentName}
            onChange={(e) => setAgentName(e.target.value)}
          >
            {agents.map((a) => (
              <option key={a.name} value={a.name}>
                {a.name} — {a.description}
              </option>
            ))}
          </select>
          <Input
            placeholder="Optional title"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
          />
          <Button onClick={createConversation} disabled={creating || !agentName}>
            {creating ? 'Creating…' : 'New chat'}
          </Button>
        </CardContent>
      </Card>

      <div className="space-y-2 mt-4">
        {conversations.length === 0 ? (
          <div className="text-text-muted text-sm">No conversations yet.</div>
        ) : (
          conversations.map((c) => (
            <Card
              key={c.id}
              className="cursor-pointer hover:bg-muted"
              onClick={() => router.visit(`/chat/${c.id}`)}
            >
              <CardContent className="flex justify-between items-center p-3">
                <div>
                  <div className="font-medium">{c.title}</div>
                  <div className="text-xs text-text-muted">{c.agentName}</div>
                </div>
                <div className="text-xs text-text-muted">
                  {new Date(c.updatedAt).toLocaleString()}
                </div>
              </CardContent>
            </Card>
          ))
        )}
      </div>
    </PageShell>
  );
}

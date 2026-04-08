import { Button, Card, CardContent, Input, PageShell } from '@simplemodule/ui';
import { fetchServerSentEvents, useChat } from '@tanstack/ai-react';
import { useMemo, useState } from 'react';

type UiMessagePart = { type: 'text'; content: string };

type UiMessage = {
  id: string;
  role: 'user' | 'assistant';
  parts: UiMessagePart[];
  createdAt: string;
};

type ConversationDto = {
  id: string;
  title: string;
  agentName: string;
};

type Props = {
  conversation: ConversationDto;
  initialMessages: UiMessage[];
};

export default function Conversation({ conversation, initialMessages }: Props) {
  const [input, setInput] = useState('');

  const hydratedInitial = useMemo(
    () => initialMessages.map((m) => ({ ...m, createdAt: new Date(m.createdAt) })),
    [initialMessages],
  );

  const { messages, sendMessage, isLoading, stop, error } = useChat({
    id: conversation.id,
    initialMessages: hydratedInitial,
    connection: fetchServerSentEvents(`/api/chat/conversations/${conversation.id}/stream`),
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const text = input.trim();
    if (!text || isLoading) return;
    sendMessage(text);
    setInput('');
  };

  return (
    <PageShell title={conversation.title} description={conversation.agentName}>
      <div className="flex flex-col gap-3 mb-4">
        {messages.map((message) => (
          <Card
            key={message.id}
            className={message.role === 'user' ? 'bg-accent/30 self-end max-w-2xl' : 'max-w-2xl'}
          >
            <CardContent className="p-3 whitespace-pre-wrap">
              {message.parts.map((part, idx) => {
                if (part.type === 'thinking') {
                  return (
                    <div key={idx} className="text-text-muted italic text-xs mb-1">
                      {part.content}
                    </div>
                  );
                }
                if (part.type === 'text') {
                  return <span key={idx}>{part.content}</span>;
                }
                if (part.type === 'tool-call') {
                  return (
                    <div key={idx} className="text-xs text-text-muted">
                      tool: {(part as unknown as { name?: string }).name ?? 'call'}
                    </div>
                  );
                }
                return null;
              })}
            </CardContent>
          </Card>
        ))}
      </div>

      <form onSubmit={handleSubmit} className="flex gap-2 sticky bottom-0 bg-background pt-2">
        <Input
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder="Type a message…"
          disabled={isLoading}
        />
        {isLoading ? (
          <Button type="button" onClick={stop}>
            Stop
          </Button>
        ) : (
          <Button type="submit" disabled={!input.trim()}>
            Send
          </Button>
        )}
      </form>

      {error && <div className="text-red-500 text-sm mt-2">{error.message}</div>}
    </PageShell>
  );
}

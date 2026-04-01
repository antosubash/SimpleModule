import * as React from 'react';
import { useAgentChat } from '../hooks/use-agent-chat';
import { cn } from '../lib/utils';

interface AgentChatProps {
  agentName: string;
  baseUrl?: string;
  streaming?: boolean;
  className?: string;
  placeholder?: string;
}

export function AgentChat({
  agentName,
  baseUrl,
  streaming = true,
  className,
  placeholder = 'Ask a question...',
}: AgentChatProps) {
  const { messages, isLoading, sendMessage, sendMessageStream, stopStreaming, clearMessages } =
    useAgentChat({ agentName, baseUrl });
  const [input, setInput] = React.useState('');
  const messagesEndRef = React.useRef<HTMLDivElement>(null);

  // biome-ignore lint/correctness/useExhaustiveDependencies: scroll on every message change
  React.useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages.length]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!input.trim() || isLoading) return;
    const message = input;
    setInput('');
    if (streaming) {
      await sendMessageStream(message);
    } else {
      await sendMessage(message);
    }
  };

  return (
    <div className={cn('flex flex-col h-full border rounded-lg bg-background', className)}>
      <div className="flex items-center justify-between border-b px-4 py-2">
        <span className="text-sm font-medium">{agentName}</span>
        <button
          type="button"
          onClick={clearMessages}
          className="text-xs text-muted-foreground hover:text-foreground"
        >
          Clear
        </button>
      </div>

      <div className="flex-1 overflow-y-auto p-4 space-y-3">
        {messages.map((msg) => (
          <div
            key={msg.id}
            className={cn(
              'max-w-[80%] rounded-lg px-3 py-2 text-sm',
              msg.role === 'user'
                ? 'ml-auto bg-primary text-primary-foreground'
                : 'bg-muted text-foreground',
            )}
          >
            {msg.content || (isLoading ? '...' : '')}
          </div>
        ))}
        <div ref={messagesEndRef} />
      </div>

      <form onSubmit={handleSubmit} className="border-t p-3 flex gap-2">
        <input
          type="text"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder={placeholder}
          disabled={isLoading}
          className="flex-1 rounded-md border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-ring"
        />
        {isLoading ? (
          <button
            type="button"
            onClick={stopStreaming}
            className="rounded-md bg-destructive px-4 py-2 text-sm text-destructive-foreground"
          >
            Stop
          </button>
        ) : (
          <button
            type="submit"
            disabled={!input.trim()}
            className="rounded-md bg-primary px-4 py-2 text-sm text-primary-foreground disabled:opacity-50"
          >
            Send
          </button>
        )}
      </form>
    </div>
  );
}

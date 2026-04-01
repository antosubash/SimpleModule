import { useCallback, useRef, useState } from 'react';

export interface AgentMessage {
  id: string;
  role: 'user' | 'agent';
  content: string;
  timestamp: Date;
}

export interface UseAgentChatOptions {
  agentName: string;
  baseUrl?: string;
}

export function useAgentChat({ agentName, baseUrl = '' }: UseAgentChatOptions) {
  const [messages, setMessages] = useState<AgentMessage[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [sessionId, setSessionId] = useState<string | null>(null);
  const abortControllerRef = useRef<AbortController | null>(null);

  const sendMessage = useCallback(
    async (content: string) => {
      const userMessage: AgentMessage = {
        id: crypto.randomUUID(),
        role: 'user',
        content,
        timestamp: new Date(),
      };
      setMessages((prev) => [...prev, userMessage]);
      setIsLoading(true);

      try {
        const response = await fetch(`${baseUrl}/api/agents/${agentName}/chat`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ message: content, sessionId }),
        });

        if (!response.ok) throw new Error(`Agent error: ${response.status}`);

        const data = await response.json();
        setSessionId(data.sessionId);

        const agentMessage: AgentMessage = {
          id: crypto.randomUUID(),
          role: 'agent',
          content: data.message,
          timestamp: new Date(),
        };
        setMessages((prev) => [...prev, agentMessage]);
      } finally {
        setIsLoading(false);
      }
    },
    [agentName, baseUrl, sessionId],
  );

  const sendMessageStream = useCallback(
    async (content: string) => {
      const userMessage: AgentMessage = {
        id: crypto.randomUUID(),
        role: 'user',
        content,
        timestamp: new Date(),
      };
      setMessages((prev) => [...prev, userMessage]);
      setIsLoading(true);

      const agentMessageId = crypto.randomUUID();
      setMessages((prev) => [
        ...prev,
        { id: agentMessageId, role: 'agent', content: '', timestamp: new Date() },
      ]);

      abortControllerRef.current = new AbortController();

      try {
        const response = await fetch(`${baseUrl}/api/agents/${agentName}/chat/stream`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ message: content, sessionId }),
          signal: abortControllerRef.current.signal,
        });

        if (!response.ok) throw new Error(`Agent error: ${response.status}`);
        if (!response.body) throw new Error('No response body');

        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let buffer = '';

        while (true) {
          const { done, value } = await reader.read();
          if (done) break;

          buffer += decoder.decode(value, { stream: true });
          const lines = buffer.split('\n');
          buffer = lines.pop() ?? '';

          for (const line of lines) {
            if (!line.startsWith('data: ')) continue;
            const data = line.slice(6);
            if (data === '[DONE]') continue;

            try {
              const parsed = JSON.parse(data);
              if (parsed.text) {
                setMessages((prev) =>
                  prev.map((m) =>
                    m.id === agentMessageId ? { ...m, content: m.content + parsed.text } : m,
                  ),
                );
              }
            } catch {
              // Skip malformed SSE data
            }
          }
        }
      } finally {
        setIsLoading(false);
        abortControllerRef.current = null;
      }
    },
    [agentName, baseUrl, sessionId],
  );

  const stopStreaming = useCallback(() => {
    abortControllerRef.current?.abort();
  }, []);

  const clearMessages = useCallback(() => {
    setMessages([]);
    setSessionId(null);
  }, []);

  return {
    messages,
    isLoading,
    sendMessage,
    sendMessageStream,
    stopStreaming,
    clearMessages,
    sessionId,
  };
}

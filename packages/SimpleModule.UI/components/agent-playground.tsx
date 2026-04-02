import * as React from 'react';
import { cn } from '../lib/utils';
import { AgentChat } from './agent-chat';
import { AgentSelector } from './agent-selector';

interface AgentPlaygroundProps {
  className?: string;
  baseUrl?: string;
}

export function AgentPlayground({ className, baseUrl }: AgentPlaygroundProps) {
  const [selectedAgent, setSelectedAgent] = React.useState<string>('');

  return (
    <div className={cn('flex flex-col gap-4 h-full', className)}>
      <div className="flex items-center gap-3 border-b pb-3">
        <h1 className="text-lg font-semibold">Agent Playground</h1>
        <AgentSelector baseUrl={baseUrl} onSelect={setSelectedAgent} />
      </div>

      {selectedAgent ? (
        <AgentChat
          agentName={selectedAgent}
          baseUrl={baseUrl}
          streaming
          className="flex-1 min-h-[400px]"
          placeholder={`Chat with ${selectedAgent}...`}
        />
      ) : (
        <div className="flex-1 flex items-center justify-center text-muted-foreground">
          Select an agent to start chatting
        </div>
      )}
    </div>
  );
}

import * as React from 'react';
import { cn } from '../lib/utils';

interface AgentInfo {
  name: string;
  description: string;
  module: string;
}

interface AgentSelectorProps {
  baseUrl?: string;
  onSelect: (agentName: string) => void;
  className?: string;
}

export function AgentSelector({ baseUrl = '', onSelect, className }: AgentSelectorProps) {
  const [agents, setAgents] = React.useState<AgentInfo[]>([]);
  const [selected, setSelected] = React.useState<string>('');

  React.useEffect(() => {
    fetch(`${baseUrl}/api/agents`)
      .then((r) => r.json())
      .then(setAgents)
      .catch(() => {});
  }, [baseUrl]);

  const handleChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    setSelected(e.target.value);
    onSelect(e.target.value);
  };

  return (
    <select
      value={selected}
      onChange={handleChange}
      className={cn(
        'rounded-md border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-ring',
        className,
      )}
    >
      <option value="">Select an agent...</option>
      {agents.map((a) => (
        <option key={a.name} value={a.name}>
          {a.name} ({a.module})
        </option>
      ))}
    </select>
  );
}

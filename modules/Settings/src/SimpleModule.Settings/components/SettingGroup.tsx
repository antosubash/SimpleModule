import { Card } from '@simplemodule/ui';
import { toAnchorId } from './SettingsGroupNav';

interface SettingGroupProps {
  group: string;
  children: React.ReactNode;
}

export default function SettingGroup({ group, children }: SettingGroupProps) {
  return (
    <section
      id={toAnchorId(group)}
      aria-labelledby={`${toAnchorId(group)}-heading`}
      data-testid="setting-card"
      className="scroll-mt-32"
    >
      <h2
        id={`${toAnchorId(group)}-heading`}
        className="text-base font-bold mb-3 flex items-center gap-2 before:content-[''] before:w-1 before:h-5 before:rounded-full before:bg-gradient-to-b before:from-primary before:to-accent"
        style={{ fontFamily: 'var(--font-display)' }}
      >
        {group}
      </h2>
      <Card padding="none" className="divide-y divide-border">
        {children}
      </Card>
    </section>
  );
}

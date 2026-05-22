import { toAnchorId } from './SettingsGroupNav';

interface SettingGroupProps {
  group: string;
  children: React.ReactNode;
}

export default function SettingGroup({ group, children }: SettingGroupProps) {
  return (
    <section id={toAnchorId(group)} aria-labelledby={`${toAnchorId(group)}-heading`}>
      <h2
        id={`${toAnchorId(group)}-heading`}
        className="sticky top-14 z-20 -mx-4 bg-surface/95 backdrop-blur-sm px-4 py-2 text-xs font-semibold uppercase tracking-widest text-text-muted border-b border-border sm:-mx-6 sm:px-6 lg:-mx-8 lg:px-8"
      >
        {group}
      </h2>
      <div className="bg-surface rounded-xl border border-border divide-y divide-border px-4 sm:px-6 mt-3">
        {children}
      </div>
    </section>
  );
}

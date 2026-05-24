interface SettingsGroupNavProps {
  groups: string[];
  activeGroup: string | null;
}

function toAnchorId(group: string) {
  return `settings-group-${group.toLowerCase().replace(/\s+/g, '-')}`;
}

export { toAnchorId };

export default function SettingsGroupNav({ groups, activeGroup }: SettingsGroupNavProps) {
  if (groups.length === 0) {
    return null;
  }

  const handleClick = (e: React.MouseEvent<HTMLAnchorElement>, group: string) => {
    e.preventDefault();
    const el = document.getElementById(toAnchorId(group));
    if (el) {
      el.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
  };

  return (
    <nav aria-label="Settings groups" className="sticky top-28 space-y-0.5">
      {groups.map((group) => {
        const isActive = group === activeGroup;
        return (
          <a
            key={group}
            href={`#${toAnchorId(group)}`}
            onClick={(e) => handleClick(e, group)}
            className={[
              'block rounded-lg px-3 py-2 text-sm transition-colors duration-150',
              isActive
                ? 'bg-primary-subtle text-primary font-semibold'
                : 'text-text-secondary hover:bg-surface-raised hover:text-text',
            ].join(' ')}
            aria-current={isActive ? 'location' : undefined}
          >
            {group}
          </a>
        );
      })}
    </nav>
  );
}

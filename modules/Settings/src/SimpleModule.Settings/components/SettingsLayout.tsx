import { useEffect, useRef, useState } from 'react';
import SettingsGroupNav, { toAnchorId } from './SettingsGroupNav';

interface SettingsLayoutProps {
  groups: string[];
  toolbar: React.ReactNode;
  children: React.ReactNode;
  topOffset?: number;
}

export default function SettingsLayout({
  groups,
  toolbar,
  children,
  topOffset = 112,
}: SettingsLayoutProps) {
  const [activeGroup, setActiveGroup] = useState<string | null>(groups[0] ?? null);
  const observerRef = useRef<IntersectionObserver | null>(null);

  useEffect(() => {
    observerRef.current?.disconnect();

    const entries = new Map<string, number>();

    observerRef.current = new IntersectionObserver(
      (observed) => {
        for (const entry of observed) {
          entries.set(entry.target.id, entry.intersectionRatio);
        }
        let bestId: string | null = null;
        let bestRatio = -1;
        for (const [id, ratio] of entries) {
          if (ratio > bestRatio) {
            bestRatio = ratio;
            bestId = id;
          }
        }
        if (bestId !== null) {
          const group = groups.find((g) => toAnchorId(g) === bestId) ?? null;
          if (group !== null) setActiveGroup(group);
        }
      },
      {
        rootMargin: `-${topOffset}px 0px -60% 0px`,
        threshold: [0, 0.25, 0.5, 0.75, 1],
      },
    );

    for (const group of groups) {
      const el = document.getElementById(toAnchorId(group));
      if (el) observerRef.current.observe(el);
    }

    return () => observerRef.current?.disconnect();
  }, [groups, topOffset]);

  return (
    <div className="flex flex-col gap-4">
      <div
        className="sticky z-30 -mx-4 bg-surface/95 backdrop-blur-sm border-b border-border px-4 py-3 sm:-mx-6 sm:px-6 lg:-mx-8 lg:px-8"
        style={{ top: 0 }}
      >
        {toolbar}
      </div>

      <div className="flex gap-8">
        <aside className="hidden lg:block w-48 flex-shrink-0">
          <SettingsGroupNav groups={groups} activeGroup={activeGroup} />
        </aside>

        <div className="flex-1 min-w-0 space-y-8 pb-24">{children}</div>
      </div>
    </div>
  );
}

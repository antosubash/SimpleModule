import { router } from '@inertiajs/react';
import { Tabs, TabsList, TabsTrigger } from '@simplemodule/ui';

interface Tab {
  id: string;
  label: string;
}

interface TabNavProps {
  tabs: Tab[];
  activeTab: string;
  baseUrl: string;
}

export function TabNav({ tabs, activeTab, baseUrl }: TabNavProps) {
  return (
    <Tabs
      value={activeTab}
      onValueChange={(value) => router.get(baseUrl, { tab: value }, { preserveState: true })}
      className="mb-6"
    >
      <TabsList>
        {tabs.map((tab) => (
          <TabsTrigger key={tab.id} value={tab.id}>
            {tab.label}
          </TabsTrigger>
        ))}
      </TabsList>
    </Tabs>
  );
}

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
      className="mb-4 sm:mb-6"
    >
      <div className="overflow-x-auto -mx-4 px-4 sm:mx-0 sm:px-0">
        <TabsList>
          {tabs.map((tab) => (
            <TabsTrigger key={tab.id} value={tab.id}>
              {tab.label}
            </TabsTrigger>
          ))}
        </TabsList>
      </div>
    </Tabs>
  );
}

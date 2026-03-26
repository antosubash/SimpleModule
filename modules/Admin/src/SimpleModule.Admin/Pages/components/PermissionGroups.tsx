import { Checkbox, Label } from '@simplemodule/ui';
import { useState } from 'react';

interface PermissionGroupsProps {
  permissionsByModule: Record<string, string[]>;
  selected: string[];
  namePrefix: string;
}

export function PermissionGroups({
  permissionsByModule,
  selected,
  namePrefix,
}: PermissionGroupsProps) {
  const modules = Object.entries(permissionsByModule);
  const [selectedPerms, setSelectedPerms] = useState<Set<string>>(new Set(selected));

  if (modules.length === 0) {
    return <p className="text-sm text-text-muted">No permissions registered.</p>;
  }

  function togglePermission(perm: string) {
    setSelectedPerms((prev) => {
      const next = new Set(prev);
      if (next.has(perm)) {
        next.delete(perm);
      } else {
        next.add(perm);
      }
      return next;
    });
  }

  function toggleAll(permissions: string[], checked: boolean) {
    setSelectedPerms((prev) => {
      const next = new Set(prev);
      for (const perm of permissions) {
        if (checked) {
          next.add(perm);
        } else {
          next.delete(perm);
        }
      }
      return next;
    });
  }

  return (
    <div className="space-y-4">
      {modules.map(([moduleName, permissions]) => {
        const allSelected = permissions.every((p) => selectedPerms.has(p));

        return (
          <div key={moduleName} className="border border-border rounded-lg p-4">
            <div className="flex items-center justify-between mb-3">
              <h4 className="font-medium text-sm">{moduleName}</h4>
              <div className="flex items-center gap-1.5">
                <Checkbox
                  id={`select-all-${moduleName}`}
                  checked={allSelected}
                  onCheckedChange={(checked) => toggleAll(permissions, checked === true)}
                />
                <Label
                  htmlFor={`select-all-${moduleName}`}
                  className="text-xs text-text-muted mb-0 cursor-pointer"
                >
                  Select all
                </Label>
              </div>
            </div>
            <div className="flex flex-wrap gap-x-6 gap-y-2">
              {permissions.map((perm) => {
                const shortName = perm.includes('.') ? perm.split('.').pop() : perm;
                return (
                  <div key={perm} className="flex items-center gap-1.5">
                    <Checkbox
                      id={`perm-${perm}`}
                      name={namePrefix}
                      value={perm}
                      checked={selectedPerms.has(perm)}
                      onCheckedChange={() => togglePermission(perm)}
                    />
                    <Label
                      htmlFor={`perm-${perm}`}
                      className="text-sm mb-0 font-normal cursor-pointer"
                    >
                      {shortName}
                    </Label>
                  </div>
                );
              })}
            </div>
          </div>
        );
      })}
    </div>
  );
}

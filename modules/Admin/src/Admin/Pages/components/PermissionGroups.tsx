interface PermissionGroupsProps {
  permissionsByModule: Record<string, string[]>;
  selected: string[];
  namePrefix: string;
}

export function PermissionGroups({ permissionsByModule, selected, namePrefix }: PermissionGroupsProps) {
  const modules = Object.entries(permissionsByModule);

  if (modules.length === 0) {
    return <p className="text-sm text-text-muted">No permissions registered.</p>;
  }

  return (
    <div className="space-y-4">
      {modules.map(([moduleName, permissions]) => {
        const allSelected = permissions.every((p) => selected.includes(p));

        return (
          <div key={moduleName} className="border border-border rounded-lg p-4">
            <div className="flex items-center justify-between mb-3">
              <h4 className="font-medium text-sm">{moduleName}</h4>
              <label className="flex items-center gap-1.5 text-xs text-text-muted cursor-pointer">
                <input
                  type="checkbox"
                  checked={allSelected}
                  onChange={(e) => {
                    const checkboxes = e.currentTarget
                      .closest('.border')
                      ?.querySelectorAll<HTMLInputElement>(`input[name="${namePrefix}"]`);
                    checkboxes?.forEach((cb) => {
                      cb.checked = e.currentTarget.checked;
                    });
                  }}
                  className="h-4 w-4 rounded border border-border bg-surface accent-primary"
                />
                Select all
              </label>
            </div>
            <div className="flex flex-wrap gap-x-6 gap-y-2">
              {permissions.map((perm) => {
                const shortName = perm.includes('.') ? perm.split('.').pop() : perm;
                return (
                  <label key={perm} className="flex items-center gap-1.5 text-sm cursor-pointer">
                    <input
                      type="checkbox"
                      name={namePrefix}
                      value={perm}
                      defaultChecked={selected.includes(perm)}
                      className="h-4 w-4 rounded border border-border bg-surface accent-primary"
                    />
                    {shortName}
                  </label>
                );
              })}
            </div>
          </div>
        );
      })}
    </div>
  );
}

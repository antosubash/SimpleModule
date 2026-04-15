import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Checkbox,
  Label,
} from '@simplemodule/ui';
import { PermissionGroups } from '@/components/PermissionGroups';
import { AdminKeys } from '@/Locales/keys';

interface Role {
  id: string;
  name: string;
  description: string | null;
}

interface Props {
  userId: string;
  userRoles: string[];
  allRoles: Role[];
  userPermissions: string[];
  permissionsByModule: Record<string, string[]>;
}

export function UserRolesTab({
  userId,
  userRoles,
  allRoles,
  userPermissions,
  permissionsByModule,
}: Props) {
  const { t } = useTranslation('Admin');

  return (
    <>
      <Card>
        <CardHeader>
          <CardTitle>{t(AdminKeys.UsersEdit.RolesTitle)}</CardTitle>
        </CardHeader>
        <CardContent>
          <form
            onSubmit={(e) => {
              e.preventDefault();
              router.post(`/admin/users/${userId}/roles`, new FormData(e.currentTarget));
            }}
          >
            <div className="space-y-2 mb-4">
              {allRoles.map((role) => (
                <div key={role.id} className="flex items-center gap-2">
                  <Checkbox
                    id={`role-${role.id}`}
                    name="roles"
                    value={role.name ?? ''}
                    defaultChecked={userRoles.includes(role.name ?? '')}
                  />
                  <Label htmlFor={`role-${role.id}`} className="mb-0">
                    {role.name}
                    {role.description && (
                      <span className="text-text-muted ml-1">&mdash; {role.description}</span>
                    )}
                  </Label>
                </div>
              ))}
              {allRoles.length === 0 && (
                <p className="text-sm text-text-muted">{t(AdminKeys.UsersEdit.NoRolesDefined)}</p>
              )}
            </div>
            <Button type="submit">{t(AdminKeys.UsersEdit.SaveRolesButton)}</Button>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t(AdminKeys.UsersEdit.DirectPermissionsTitle)}</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-text-muted mb-4">
            {t(AdminKeys.UsersEdit.DirectPermissionsDescription)}
          </p>
          <form
            onSubmit={(e) => {
              e.preventDefault();
              router.post(`/admin/users/${userId}/permissions`, new FormData(e.currentTarget));
            }}
          >
            <PermissionGroups
              permissionsByModule={permissionsByModule}
              selected={userPermissions}
              namePrefix="permissions"
            />
            <Button type="submit" className="mt-4">
              {t(AdminKeys.UsersEdit.SavePermissionsButton)}
            </Button>
          </form>
        </CardContent>
      </Card>
    </>
  );
}

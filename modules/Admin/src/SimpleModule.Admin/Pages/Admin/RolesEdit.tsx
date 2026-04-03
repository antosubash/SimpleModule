import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Container,
  Field,
  FieldGroup,
  Input,
  Label,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { AdminKeys } from '../../Locales/keys';
import { PermissionGroups } from '../components/PermissionGroups';
import { TabNav } from '../components/TabNav';

interface RoleDetail {
  id: string;
  name: string;
  description: string | null;
  createdAt: string;
}

interface UserSummary {
  id: string;
  displayName: string;
  email: string;
}

interface Props {
  role: RoleDetail;
  users: UserSummary[];
  rolePermissions: string[];
  permissionsByModule: Record<string, string[]>;
  tab: string;
}

export default function RolesEdit({
  role,
  users,
  rolePermissions,
  permissionsByModule,
  tab,
}: Props) {
  const { t } = useTranslation('Admin');

  const tabs = [
    { id: 'details', label: t(AdminKeys.RolesEdit.TabDetails) },
    { id: 'permissions', label: t(AdminKeys.RolesEdit.TabPermissions) },
    { id: 'users', label: t(AdminKeys.RolesEdit.TabUsers) },
  ];

  return (
    <Container className="space-y-6">
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/admin/roles">
              {t(AdminKeys.RolesEdit.BreadcrumbRoles)}
            </BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>{t(AdminKeys.RolesEdit.BreadcrumbEdit)}</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>
      <h1 className="text-2xl font-bold tracking-tight">{t(AdminKeys.RolesEdit.Title)}</h1>

      <TabNav tabs={tabs} activeTab={tab} baseUrl={`/admin/roles/${role.id}/edit`} />

      {tab === 'details' && (
        <Card>
          <CardContent className="p-6">
            <form
              onSubmit={(e) => {
                e.preventDefault();
                router.post(`/admin/roles/${role.id}`, new FormData(e.currentTarget));
              }}
            >
              <FieldGroup>
                <Field>
                  <Label htmlFor="name">{t(AdminKeys.RolesEdit.FieldName)}</Label>
                  <Input id="name" name="name" defaultValue={role.name} required />
                </Field>
                <Field>
                  <Label htmlFor="description">{t(AdminKeys.RolesEdit.FieldDescription)}</Label>
                  <Input
                    id="description"
                    name="description"
                    defaultValue={role.description ?? ''}
                  />
                </Field>
                <Button type="submit">{t(AdminKeys.RolesEdit.SaveButton)}</Button>
              </FieldGroup>
            </form>
          </CardContent>
        </Card>
      )}

      {tab === 'permissions' && (
        <Card>
          <CardHeader>
            <CardTitle>{t(AdminKeys.RolesEdit.RolePermissionsTitle)}</CardTitle>
          </CardHeader>
          <CardContent>
            <form
              onSubmit={(e) => {
                e.preventDefault();
                router.post(`/admin/roles/${role.id}/permissions`, new FormData(e.currentTarget));
              }}
            >
              <PermissionGroups
                permissionsByModule={permissionsByModule}
                selected={rolePermissions}
                namePrefix="permissions"
              />
              <Button type="submit" className="mt-4">
                {t(AdminKeys.RolesEdit.SavePermissionsButton)}
              </Button>
            </form>
          </CardContent>
        </Card>
      )}

      {tab === 'users' && (
        <Card>
          <CardHeader>
            <CardTitle>
              {t(AdminKeys.RolesEdit.AssignedUsersTitle, { count: String(users.length) })}
            </CardTitle>
          </CardHeader>
          <CardContent>
            {users.length === 0 ? (
              <p className="text-sm text-text-muted">{t(AdminKeys.RolesEdit.NoUsersAssigned)}</p>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t(AdminKeys.RolesEdit.ColName)}</TableHead>
                    <TableHead>{t(AdminKeys.RolesEdit.ColEmail)}</TableHead>
                    <TableHead />
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {users.map((u) => (
                    <TableRow key={u.id}>
                      <TableCell className="font-medium">{u.displayName || '\u2014'}</TableCell>
                      <TableCell className="text-text-muted">{u.email}</TableCell>
                      <TableCell>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => router.get(`/admin/users/${u.id}/edit`)}
                        >
                          {t(AdminKeys.RolesEdit.EditUserButton)}
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      )}
    </Container>
  );
}

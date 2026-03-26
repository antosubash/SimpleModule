import { router } from '@inertiajs/react';
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

const tabs = [
  { id: 'details', label: 'Details' },
  { id: 'permissions', label: 'Permissions' },
  { id: 'users', label: 'Users' },
];

export default function RolesEdit({
  role,
  users,
  rolePermissions,
  permissionsByModule,
  tab,
}: Props) {
  return (
    <Container className="space-y-6">
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/admin/roles">Roles</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>Edit Role</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>
      <h1 className="text-2xl font-bold tracking-tight">Edit Role</h1>

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
                  <Label htmlFor="name">Name</Label>
                  <Input id="name" name="name" defaultValue={role.name} required />
                </Field>
                <Field>
                  <Label htmlFor="description">Description</Label>
                  <Input
                    id="description"
                    name="description"
                    defaultValue={role.description ?? ''}
                  />
                </Field>
                <Button type="submit">Save</Button>
              </FieldGroup>
            </form>
          </CardContent>
        </Card>
      )}

      {tab === 'permissions' && (
        <Card>
          <CardHeader>
            <CardTitle>Role Permissions</CardTitle>
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
                Save Permissions
              </Button>
            </form>
          </CardContent>
        </Card>
      )}

      {tab === 'users' && (
        <Card>
          <CardHeader>
            <CardTitle>Assigned Users ({users.length})</CardTitle>
          </CardHeader>
          <CardContent>
            {users.length === 0 ? (
              <p className="text-sm text-text-muted">No users assigned to this role.</p>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Name</TableHead>
                    <TableHead>Email</TableHead>
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
                          Edit
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

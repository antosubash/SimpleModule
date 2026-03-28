import { router } from '@inertiajs/react';
import {
  Badge,
  Button,
  DataGridPage,
  Input,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { type FormEvent, useState } from 'react';

interface User {
  id: string;
  displayName: string;
  email: string;
  emailConfirmed: boolean;
  roles: string[];
  isLockedOut: boolean;
  isDeactivated: boolean;
  createdAt: string;
}

interface Props {
  users: User[];
  search: string;
  page: number;
  totalPages: number;
  totalCount: number;
  allRoles: string[];
  filterStatus: string;
  filterRole: string;
}

function userStatus(user: User) {
  if (user.isDeactivated) return { label: 'Deactivated', variant: 'secondary' as const };
  if (user.isLockedOut) return { label: 'Locked', variant: 'danger' as const };
  return { label: 'Active', variant: 'success' as const };
}

export default function Users({
  users,
  search,
  page,
  totalPages,
  totalCount,
  allRoles,
  filterStatus,
  filterRole,
}: Props) {
  const [searchValue, setSearchValue] = useState(search);

  function navigate(params: Record<string, string | number>) {
    router.get(
      '/admin/users',
      { search: searchValue, page: 1, filterStatus, filterRole, ...params },
      { preserveState: true },
    );
  }

  function handleSearch(e: FormEvent) {
    e.preventDefault();
    navigate({ search: searchValue });
  }

  const filterBar = (
    <div className="flex flex-col sm:flex-row gap-2">
      <form onSubmit={handleSearch} className="flex gap-2 flex-1">
        <Input
          value={searchValue}
          onChange={(e) => setSearchValue(e.target.value)}
          placeholder="Search by name or email..."
          className="flex-1"
        />
        <Button type="submit" variant="secondary">
          Search
        </Button>
      </form>
      <Select
        value={filterStatus || '__all__'}
        onValueChange={(v) => navigate({ filterStatus: v === '__all__' ? '' : v })}
      >
        <SelectTrigger className="w-[160px]" aria-label="Status filter">
          <SelectValue placeholder="All statuses" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="__all__">All statuses</SelectItem>
          <SelectItem value="active">Active</SelectItem>
          <SelectItem value="locked">Locked</SelectItem>
          <SelectItem value="deactivated">Deactivated</SelectItem>
        </SelectContent>
      </Select>
      {allRoles.length > 0 && (
        <Select
          value={filterRole || '__all__'}
          onValueChange={(v) => navigate({ filterRole: v === '__all__' ? '' : v })}
        >
          <SelectTrigger className="w-[160px]" aria-label="Role filter">
            <SelectValue placeholder="All roles" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="__all__">All roles</SelectItem>
            {allRoles.map((role) => (
              <SelectItem key={role} value={role}>
                {role}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      )}
    </div>
  );

  return (
    <DataGridPage
      title="Users"
      description={`${totalCount} total users`}
      actions={<Button onClick={() => router.get('/admin/users/create')}>Create User</Button>}
      data={users}
      filterBar={filterBar}
      emptyTitle="No users found"
      emptyDescription={
        search ? `No users matching "${search}".` : 'Get started by creating your first user.'
      }
      emptyAction={
        !search ? (
          <Button onClick={() => router.get('/admin/users/create')}>Create User</Button>
        ) : undefined
      }
    >
      {(pageData) => (
        <>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Email</TableHead>
                <TableHead>Roles</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Created</TableHead>
                <TableHead />
              </TableRow>
            </TableHeader>
            <TableBody>
              {pageData.map((user) => {
                const status = userStatus(user);
                return (
                  <TableRow key={user.id}>
                    <TableCell className="font-medium">{user.displayName || '\u2014'}</TableCell>
                    <TableCell className="text-text-secondary">
                      {user.email}
                      {!user.emailConfirmed && (
                        <Badge variant="warning" className="ml-2">
                          unverified
                        </Badge>
                      )}
                    </TableCell>
                    <TableCell>
                      <div className="flex gap-1 flex-wrap">
                        {user.roles.map((role) => (
                          <Badge key={role} variant="info">
                            {role}
                          </Badge>
                        ))}
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge variant={status.variant}>{status.label}</Badge>
                    </TableCell>
                    <TableCell className="text-sm text-text-muted">
                      {new Date(user.createdAt).toLocaleDateString()}
                    </TableCell>
                    <TableCell>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => router.get(`/admin/users/${user.id}/edit`)}
                      >
                        Edit
                      </Button>
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>

          {totalPages > 1 && (
            <div className="flex items-center justify-between pt-4">
              <span className="text-sm text-text-muted">
                Page {page} of {totalPages}
              </span>
              <div className="flex items-center gap-1">
                <Button
                  variant="ghost"
                  size="sm"
                  disabled={page <= 1}
                  onClick={() => navigate({ page: page - 1 })}
                >
                  Previous
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  disabled={page >= totalPages}
                  onClick={() => navigate({ page: page + 1 })}
                >
                  Next
                </Button>
              </div>
            </div>
          )}
        </>
      )}
    </DataGridPage>
  );
}

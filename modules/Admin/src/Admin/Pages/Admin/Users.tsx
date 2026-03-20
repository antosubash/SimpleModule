import { router } from '@inertiajs/react';
import {
  Badge,
  Button,
  Input,
  PageHeader,
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
}

function userStatus(user: User) {
  if (user.isDeactivated) return { label: 'Deactivated', variant: 'secondary' as const };
  if (user.isLockedOut) return { label: 'Locked', variant: 'danger' as const };
  return { label: 'Active', variant: 'success' as const };
}

export default function Users({ users, search, page, totalPages, totalCount }: Props) {
  const [searchValue, setSearchValue] = useState(search);

  function handleSearch(e: FormEvent) {
    e.preventDefault();
    router.get('/admin/users', { search: searchValue, page: 1 }, { preserveState: true });
  }

  function goToPage(p: number) {
    router.get('/admin/users', { search: searchValue, page: p }, { preserveState: true });
  }

  return (
    <div className="mx-auto max-w-5xl space-y-6">
      <PageHeader
        className="mb-0"
        title="Users"
        description={`${totalCount} total users`}
        actions={<Button onClick={() => router.get('/admin/users/create')}>Create User</Button>}
      />

      <form onSubmit={handleSearch} className="flex gap-2">
        <Input
          value={searchValue}
          onChange={(e) => setSearchValue(e.target.value)}
          placeholder="Search by name or email..."
          className="flex-1"
        />
        <Button type="submit">Search</Button>
      </form>

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
          {users.map((user) => {
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

      {users.length === 0 && search && (
        <p className="py-8 text-center text-sm text-text-muted">
          No users found matching &ldquo;{search}&rdquo;.
        </p>
      )}

      {totalPages > 1 && (
        <div className="flex items-center justify-between">
          <span className="text-sm text-text-muted">
            Showing page {page} of {totalPages} ({totalCount} users)
          </span>
          <div className="flex items-center gap-1">
            <Button
              variant="ghost"
              size="sm"
              disabled={page <= 1}
              onClick={() => goToPage(page - 1)}
            >
              <svg
                className="h-4 w-4"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <path d="m15 18-6-6 6-6" />
              </svg>
            </Button>
            {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
              <Button
                key={p}
                variant={p === page ? 'secondary' : 'ghost'}
                size="sm"
                className="h-8 w-8 p-0"
                onClick={() => goToPage(p)}
              >
                {p}
              </Button>
            ))}
            <Button
              variant="ghost"
              size="sm"
              disabled={page >= totalPages}
              onClick={() => goToPage(page + 1)}
            >
              <svg
                className="h-4 w-4"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <path d="m9 18 6-6-6-6" />
              </svg>
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}

import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
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
import { AdminKeys } from '@/Locales/keys';

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
  const { t } = useTranslation('Admin');
  const [searchValue, setSearchValue] = useState(search);

  function userStatus(user: User) {
    if (user.isDeactivated)
      return { label: t(AdminKeys.Users.StatusDeactivated), variant: 'default' as const };
    if (user.isLockedOut)
      return { label: t(AdminKeys.Users.StatusLocked), variant: 'danger' as const };
    return { label: t(AdminKeys.Users.StatusActive), variant: 'success' as const };
  }

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
    <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:gap-3">
      <form onSubmit={handleSearch} className="flex gap-2 flex-1">
        <Input
          value={searchValue}
          onChange={(e) => setSearchValue(e.target.value)}
          placeholder={t(AdminKeys.Users.SearchPlaceholder)}
          className="flex-1"
        />
        <Button type="submit" variant="secondary">
          {t(AdminKeys.Users.SearchButton)}
        </Button>
      </form>
      <Select
        value={filterStatus || '__all__'}
        onValueChange={(v) => navigate({ filterStatus: v === '__all__' ? '' : v })}
      >
        <SelectTrigger className="w-full sm:w-[160px]" aria-label="Status filter">
          <SelectValue placeholder={t(AdminKeys.Users.AllStatuses)} />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="__all__">{t(AdminKeys.Users.AllStatuses)}</SelectItem>
          <SelectItem value="active">{t(AdminKeys.Users.StatusActive)}</SelectItem>
          <SelectItem value="locked">{t(AdminKeys.Users.StatusLocked)}</SelectItem>
          <SelectItem value="deactivated">{t(AdminKeys.Users.StatusDeactivated)}</SelectItem>
        </SelectContent>
      </Select>
      {allRoles.length > 0 && (
        <Select
          value={filterRole || '__all__'}
          onValueChange={(v) => navigate({ filterRole: v === '__all__' ? '' : v })}
        >
          <SelectTrigger className="w-full sm:w-[160px]" aria-label="Role filter">
            <SelectValue placeholder={t(AdminKeys.Users.AllRoles)} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="__all__">{t(AdminKeys.Users.AllRoles)}</SelectItem>
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
      title={t(AdminKeys.Users.Title)}
      description={t(AdminKeys.Users.TotalCount, { count: String(totalCount) })}
      actions={
        <Button onClick={() => router.get('/admin/users/create')}>
          {t(AdminKeys.Users.CreateButton)}
        </Button>
      }
      data={users}
      filterBar={filterBar}
      emptyTitle={t(AdminKeys.Users.EmptyTitle)}
      emptyDescription={
        search ? t(AdminKeys.Users.EmptySearch, { search }) : t(AdminKeys.Users.EmptyDescription)
      }
      emptyAction={
        !search ? (
          <Button onClick={() => router.get('/admin/users/create')}>
            {t(AdminKeys.Users.CreateButton)}
          </Button>
        ) : undefined
      }
    >
      {(pageData) => (
        <>
          <div className="overflow-x-auto -mx-4 px-4 sm:mx-0 sm:px-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t(AdminKeys.Users.ColName)}</TableHead>
                  <TableHead>{t(AdminKeys.Users.ColEmail)}</TableHead>
                  <TableHead>{t(AdminKeys.Users.ColRoles)}</TableHead>
                  <TableHead>{t(AdminKeys.Users.ColStatus)}</TableHead>
                  <TableHead>{t(AdminKeys.Users.ColCreated)}</TableHead>
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
                            {t(AdminKeys.Users.Unverified)}
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
                          {t(AdminKeys.Users.EditButton)}
                        </Button>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </div>

          {totalPages > 1 && (
            <div className="flex items-center justify-between pt-3 sm:pt-4">
              <span className="text-sm text-text-muted">
                {t(AdminKeys.Users.Pagination, {
                  page: String(page),
                  totalPages: String(totalPages),
                })}
              </span>
              <div className="flex items-center gap-1">
                <Button
                  variant="ghost"
                  size="sm"
                  disabled={page <= 1}
                  onClick={() => navigate({ page: page - 1 })}
                >
                  {t(AdminKeys.Users.PreviousButton)}
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  disabled={page >= totalPages}
                  onClick={() => navigate({ page: page + 1 })}
                >
                  {t(AdminKeys.Users.NextButton)}
                </Button>
              </div>
            </div>
          )}
        </>
      )}
    </DataGridPage>
  );
}

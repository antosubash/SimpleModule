import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Badge,
  Button,
  DataGridPage,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { useState } from 'react';
import { BackgroundJobsKeys } from '@/Locales/keys';

interface RecurringJob {
  id: string;
  name: string;
  jobType: string;
  cronExpression: string;
  isEnabled: boolean;
  lastRunAt: string | null;
  nextRunAt: string | null;
  createdAt: string;
}

interface Props {
  jobs: RecurringJob[];
}

export default function Recurring({ jobs }: Props) {
  const { t } = useTranslation('BackgroundJobs');
  const [deleteId, setDeleteId] = useState<string | null>(null);

  function handleToggle(id: string) {
    router.post(`/api/jobs/recurring/${id}/toggle`, {}, { preserveState: true });
  }

  function handleDelete() {
    if (!deleteId) return;
    router.delete(`/api/jobs/recurring/${deleteId}`);
    setDeleteId(null);
  }

  return (
    <>
      <DataGridPage
        title={t(BackgroundJobsKeys.Recurring.Title)}
        description={t(BackgroundJobsKeys.Recurring.Description, { count: jobs.length })}
        data={jobs}
        emptyTitle={t(BackgroundJobsKeys.Recurring.EmptyTitle)}
        emptyDescription={t(BackgroundJobsKeys.Recurring.EmptyDescription)}
      >
        {(pageData) => (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t(BackgroundJobsKeys.Recurring.ColName)}</TableHead>
                <TableHead>{t(BackgroundJobsKeys.Recurring.ColSchedule)}</TableHead>
                <TableHead>{t(BackgroundJobsKeys.Recurring.ColStatus)}</TableHead>
                <TableHead>{t(BackgroundJobsKeys.Recurring.ColLastRun)}</TableHead>
                <TableHead />
              </TableRow>
            </TableHeader>
            <TableBody>
              {pageData.map((job) => (
                <TableRow key={job.id}>
                  <TableCell className="font-medium">{job.name}</TableCell>
                  <TableCell>
                    <code className="rounded bg-muted px-1.5 py-0.5 text-sm">
                      {job.cronExpression}
                    </code>
                  </TableCell>
                  <TableCell>
                    <Badge variant={job.isEnabled ? 'success' : 'default'}>
                      {job.isEnabled
                        ? t(BackgroundJobsKeys.Recurring.Enabled)
                        : t(BackgroundJobsKeys.Recurring.Disabled)}
                    </Badge>
                  </TableCell>
                  <TableCell className="text-sm text-text-muted">
                    {job.lastRunAt
                      ? new Date(job.lastRunAt).toLocaleString()
                      : t(BackgroundJobsKeys.Recurring.Never)}
                  </TableCell>
                  <TableCell>
                    <div className="flex flex-wrap gap-2">
                      <Button variant="ghost" size="sm" onClick={() => handleToggle(job.id)}>
                        {job.isEnabled
                          ? t(BackgroundJobsKeys.Recurring.ActionDisable)
                          : t(BackgroundJobsKeys.Recurring.ActionEnable)}
                      </Button>
                      <Button variant="danger" size="sm" onClick={() => setDeleteId(job.id)}>
                        {t(BackgroundJobsKeys.Recurring.ActionDelete)}
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </DataGridPage>

      <Dialog open={deleteId !== null} onOpenChange={(open) => !open && setDeleteId(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t(BackgroundJobsKeys.Recurring.DeleteDialogTitle)}</DialogTitle>
            <DialogDescription>
              {t(BackgroundJobsKeys.Recurring.DeleteDialogDescription)}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="secondary" onClick={() => setDeleteId(null)}>
              {t(BackgroundJobsKeys.Recurring.DeleteDialogCancel)}
            </Button>
            <Button variant="danger" onClick={handleDelete}>
              {t(BackgroundJobsKeys.Recurring.DeleteDialogConfirm)}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}

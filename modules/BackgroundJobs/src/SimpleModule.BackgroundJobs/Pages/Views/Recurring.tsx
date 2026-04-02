import { router } from '@inertiajs/react';
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
        title="Recurring Jobs"
        description={`${jobs.length} recurring jobs configured`}
        data={jobs}
        emptyTitle="No recurring jobs"
        emptyDescription="No recurring background jobs have been configured yet."
      >
        {(pageData) => (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Schedule</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Last Run</TableHead>
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
                      {job.isEnabled ? 'Enabled' : 'Disabled'}
                    </Badge>
                  </TableCell>
                  <TableCell className="text-sm text-text-muted">
                    {job.lastRunAt ? new Date(job.lastRunAt).toLocaleString() : 'Never'}
                  </TableCell>
                  <TableCell>
                    <div className="flex flex-wrap gap-2">
                      <Button variant="ghost" size="sm" onClick={() => handleToggle(job.id)}>
                        {job.isEnabled ? 'Disable' : 'Enable'}
                      </Button>
                      <Button variant="danger" size="sm" onClick={() => setDeleteId(job.id)}>
                        Delete
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
            <DialogTitle>Delete Recurring Job</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete this recurring job? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="secondary" onClick={() => setDeleteId(null)}>
              Cancel
            </Button>
            <Button variant="danger" onClick={handleDelete}>
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}

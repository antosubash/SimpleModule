import { router } from '@inertiajs/react';
import {
  Badge,
  DataGridPage,
  Progress,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { stateVariant } from '../utils/jobState';

interface JobSummary {
  id: string;
  jobType: string;
  state: string;
  progressPercentage: number;
  progressMessage: string | null;
  createdAt: string;
  completedAt: string | null;
}

interface PagedResult {
  items: JobSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
}

interface Props {
  jobs: PagedResult;
}

export default function List({ jobs }: Props) {
  return (
    <DataGridPage
      title="All Jobs"
      description={`${jobs.totalCount} total jobs`}
      data={jobs.items}
      emptyTitle="No jobs found"
      emptyDescription="No background jobs have been executed yet."
    >
      {(pageData) => (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Job Type</TableHead>
              <TableHead>State</TableHead>
              <TableHead>Progress</TableHead>
              <TableHead>Created</TableHead>
              <TableHead>Completed</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {pageData.map((job) => (
              <TableRow
                key={job.id}
                className="cursor-pointer"
                onClick={() => router.get(`/admin/jobs/${job.id}`)}
              >
                <TableCell className="font-medium">{job.jobType}</TableCell>
                <TableCell>
                  <Badge variant={stateVariant[job.state] ?? 'default'}>{job.state}</Badge>
                </TableCell>
                <TableCell>
                  <div className="flex items-center gap-2">
                    <Progress value={job.progressPercentage} className="w-20" />
                    <span className="text-xs text-text-muted">{job.progressPercentage}%</span>
                  </div>
                </TableCell>
                <TableCell className="text-sm text-text-muted">
                  {new Date(job.createdAt).toLocaleString()}
                </TableCell>
                <TableCell className="text-sm text-text-muted">
                  {job.completedAt ? new Date(job.completedAt).toLocaleString() : '—'}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </DataGridPage>
  );
}

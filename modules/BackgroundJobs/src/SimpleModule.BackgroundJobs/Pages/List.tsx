import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
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
import { BackgroundJobsKeys } from '@/Locales/keys';
import { stateVariant } from '@/Pages/utils/jobState';

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
  const { t } = useTranslation('BackgroundJobs');

  return (
    <DataGridPage
      title={t(BackgroundJobsKeys.List.Title)}
      description={t(BackgroundJobsKeys.List.TotalCount, { count: String(jobs.totalCount) })}
      data={jobs.items}
      emptyTitle={t(BackgroundJobsKeys.List.EmptyTitle)}
      emptyDescription={t(BackgroundJobsKeys.List.EmptyDescription)}
    >
      {(pageData) => (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t(BackgroundJobsKeys.List.ColJobType)}</TableHead>
              <TableHead>{t(BackgroundJobsKeys.List.ColState)}</TableHead>
              <TableHead>{t(BackgroundJobsKeys.List.ColProgress)}</TableHead>
              <TableHead>{t(BackgroundJobsKeys.List.ColCreated)}</TableHead>
              <TableHead>{t(BackgroundJobsKeys.List.ColCompleted)}</TableHead>
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

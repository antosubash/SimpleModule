import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  PageShell,
  Progress,
} from '@simplemodule/ui';
import { useEffect } from 'react';
import { BackgroundJobsKeys } from '@/Locales/keys';
import { stateVariant } from '@/Pages/utils/jobState';

interface LogEntry {
  message: string;
  timestamp: string;
}

interface JobDetail {
  id: string;
  jobType: string;
  moduleName: string;
  state: string;
  progressPercentage: number;
  progressMessage: string | null;
  error: string | null;
  data: string | null;
  logs: LogEntry[];
  retryCount: number;
  createdAt: string;
  startedAt: string | null;
  completedAt: string | null;
}

interface Props {
  job: JobDetail;
}

export default function Detail({ job }: Props) {
  const { t } = useTranslation('BackgroundJobs');

  useEffect(() => {
    if (job.state !== 'Running') return;
    const interval = setInterval(() => {
      router.reload({ only: ['job'] });
    }, 2000);
    return () => clearInterval(interval);
  }, [job.state]);

  return (
    <PageShell
      title={job.jobType}
      description={t(BackgroundJobsKeys.Detail.ModuleLabel, { name: job.moduleName })}
      actions={
        <div className="flex flex-wrap gap-2">
          {job.state === 'Running' && (
            <Button variant="danger" onClick={() => router.post(`/api/jobs/${job.id}/cancel`)}>
              {t(BackgroundJobsKeys.Detail.Cancel)}
            </Button>
          )}
          {job.state === 'Failed' && (
            <Button onClick={() => router.post(`/api/jobs/${job.id}/retry`)}>
              {t(BackgroundJobsKeys.Detail.Retry)}
            </Button>
          )}
          <Button variant="secondary" onClick={() => router.get('/admin/jobs/list')}>
            {t(BackgroundJobsKeys.Detail.BackToList)}
          </Button>
        </div>
      }
    >
      <div className="grid grid-cols-1 gap-3 sm:gap-4 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>{t(BackgroundJobsKeys.Detail.StatusCard)}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center gap-3">
              <Badge variant={stateVariant[job.state] ?? 'default'}>{job.state}</Badge>
              {job.retryCount > 0 && (
                <span className="text-sm text-text-muted">
                  {t(BackgroundJobsKeys.Detail.RetryCount, { count: job.retryCount })}
                </span>
              )}
            </div>
            <div>
              <div className="mb-1 flex justify-between text-sm">
                <span>{job.progressMessage ?? t(BackgroundJobsKeys.Detail.Processing)}</span>
                <span>{job.progressPercentage}%</span>
              </div>
              <Progress value={job.progressPercentage} className="h-3" />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t(BackgroundJobsKeys.Detail.TimestampsCard)}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-2 text-sm">
            <div className="flex justify-between">
              <span className="text-text-muted">{t(BackgroundJobsKeys.Detail.Created)}</span>
              <span>{new Date(job.createdAt).toLocaleString()}</span>
            </div>
            {job.startedAt && (
              <div className="flex justify-between">
                <span className="text-text-muted">{t(BackgroundJobsKeys.Detail.Started)}</span>
                <span>{new Date(job.startedAt).toLocaleString()}</span>
              </div>
            )}
            {job.completedAt && (
              <div className="flex justify-between">
                <span className="text-text-muted">{t(BackgroundJobsKeys.Detail.Completed)}</span>
                <span>{new Date(job.completedAt).toLocaleString()}</span>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {job.error && (
        <Card className="mt-4 sm:mt-6 border-red-200">
          <CardHeader>
            <CardTitle className="text-red-600">{t(BackgroundJobsKeys.Detail.ErrorCard)}</CardTitle>
          </CardHeader>
          <CardContent>
            <pre className="overflow-auto rounded bg-red-50 p-3 text-sm text-red-800">
              {job.error}
            </pre>
          </CardContent>
        </Card>
      )}

      {job.logs.length > 0 && (
        <Card className="mt-4 sm:mt-6">
          <CardHeader>
            <CardTitle>
              {t(BackgroundJobsKeys.Detail.LogsCard, { count: job.logs.length })}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="max-h-96 overflow-auto">
              {job.logs.map((log, i) => (
                <div key={i} className="flex gap-2 sm:gap-3 border-b py-1.5 text-sm last:border-0">
                  <span className="shrink-0 text-text-muted">
                    {new Date(log.timestamp).toLocaleTimeString()}
                  </span>
                  <span>{log.message}</span>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </PageShell>
  );
}

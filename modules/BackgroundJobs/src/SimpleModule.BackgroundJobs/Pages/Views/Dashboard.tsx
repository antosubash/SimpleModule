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
import { BackgroundJobsKeys } from '../../Locales/keys';

interface JobSummary {
  id: string;
  jobType: string;
  state: string;
  progressPercentage: number;
  progressMessage: string | null;
  createdAt: string;
}

interface Props {
  activeJobs: JobSummary[];
  activeCount: number;
  failedJobs: JobSummary[];
  failedCount: number;
  recurringCount: number;
}

export default function Dashboard({
  activeJobs,
  activeCount,
  failedJobs,
  failedCount,
  recurringCount,
}: Props) {
  const { t } = useTranslation('BackgroundJobs');

  return (
    <PageShell
      title={t(BackgroundJobsKeys.Dashboard.Title)}
      description={t(BackgroundJobsKeys.Dashboard.Description)}
    >
      <div className="grid grid-cols-1 gap-3 sm:gap-4 sm:grid-cols-2 md:grid-cols-3">
        <Card>
          <CardHeader>
            <CardTitle>{t(BackgroundJobsKeys.Dashboard.ActiveJobs)}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">{activeCount}</div>
            <Button
              variant="ghost"
              size="sm"
              className="mt-2"
              onClick={() => router.get('/admin/jobs/list?state=Running')}
            >
              {t(BackgroundJobsKeys.Dashboard.ViewAll)}
            </Button>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>{t(BackgroundJobsKeys.Dashboard.FailedJobs)}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold text-red-500">{failedCount}</div>
            <Button
              variant="ghost"
              size="sm"
              className="mt-2"
              onClick={() => router.get('/admin/jobs/list?state=Failed')}
            >
              {t(BackgroundJobsKeys.Dashboard.ViewAll)}
            </Button>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>{t(BackgroundJobsKeys.Dashboard.RecurringJobs)}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">{recurringCount}</div>
            <Button
              variant="ghost"
              size="sm"
              className="mt-2"
              onClick={() => router.get('/admin/jobs/recurring')}
            >
              {t(BackgroundJobsKeys.Dashboard.Manage)}
            </Button>
          </CardContent>
        </Card>
      </div>

      {activeJobs.length > 0 && (
        <Card className="mt-4 sm:mt-6">
          <CardHeader>
            <CardTitle>{t(BackgroundJobsKeys.Dashboard.CurrentlyRunning)}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {activeJobs.map((job) => (
                <div
                  key={job.id}
                  className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between rounded-md border p-3"
                >
                  <div>
                    <span className="font-medium">{job.jobType}</span>
                    <span className="ml-2 text-sm text-text-muted">{job.progressMessage}</span>
                  </div>
                  <div className="flex items-center gap-2 sm:gap-3">
                    <Progress value={job.progressPercentage} className="w-24" />
                    <span className="text-sm">{job.progressPercentage}%</span>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {failedJobs.length > 0 && (
        <Card className="mt-4 sm:mt-6">
          <CardHeader>
            <CardTitle>{t(BackgroundJobsKeys.Dashboard.RecentFailures)}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {failedJobs.map((job) => (
                <button
                  type="button"
                  key={job.id}
                  className="flex w-full items-center justify-between rounded-md border border-red-200 p-3 text-left hover:bg-red-50"
                  onClick={() => router.get(`/admin/jobs/${job.id}`)}
                >
                  <span className="font-medium">{job.jobType}</span>
                  <Badge variant="danger">{t(BackgroundJobsKeys.Dashboard.BadgeFailed)}</Badge>
                </button>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </PageShell>
  );
}

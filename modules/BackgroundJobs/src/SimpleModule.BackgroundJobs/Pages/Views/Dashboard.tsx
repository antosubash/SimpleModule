import { router } from '@inertiajs/react';
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
  return (
    <PageShell title="Background Jobs" description="Overview of job processing activity">
      <div className="grid grid-cols-1 gap-3 sm:gap-4 sm:grid-cols-2 md:grid-cols-3">
        <Card>
          <CardHeader>
            <CardTitle>Active Jobs</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">{activeCount}</div>
            <Button
              variant="ghost"
              size="sm"
              className="mt-2"
              onClick={() => router.get('/admin/jobs/list?state=Running')}
            >
              View all
            </Button>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Failed Jobs</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold text-red-500">{failedCount}</div>
            <Button
              variant="ghost"
              size="sm"
              className="mt-2"
              onClick={() => router.get('/admin/jobs/list?state=Failed')}
            >
              View all
            </Button>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Recurring Jobs</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">{recurringCount}</div>
            <Button
              variant="ghost"
              size="sm"
              className="mt-2"
              onClick={() => router.get('/admin/jobs/recurring')}
            >
              Manage
            </Button>
          </CardContent>
        </Card>
      </div>

      {activeJobs.length > 0 && (
        <Card className="mt-4 sm:mt-6">
          <CardHeader>
            <CardTitle>Currently Running</CardTitle>
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
            <CardTitle>Recent Failures</CardTitle>
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
                  <Badge variant="danger">Failed</Badge>
                </button>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </PageShell>
  );
}

import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Alert,
  AlertDescription,
  AlertTitle,
  Button,
  Card,
  CardContent,
  Container,
  PageShell,
} from '@simplemodule/ui';
import { DashboardKeys } from '@/Locales/keys';
import { ApiTester } from './components/ApiTester';
import { TokenTester } from './components/TokenTester';
import { UserInfoPanel } from './components/UserInfoPanel';

interface HomeProps {
  isAuthenticated: boolean;
  displayName: string;
  isDevelopment: boolean;
}

export default function Home({ isAuthenticated, displayName, isDevelopment }: HomeProps) {
  return isAuthenticated ? (
    <DashboardView displayName={displayName} isDevelopment={isDevelopment} />
  ) : (
    <LandingView isDevelopment={isDevelopment} />
  );
}

// --- Dashboard View ---

function DashboardView({
  displayName,
  isDevelopment,
}: {
  displayName: string;
  isDevelopment: boolean;
}) {
  const { t } = useTranslation('Dashboard');
  return (
    <PageShell
      title={t(DashboardKeys.Home.DashboardTitle, { displayName })}
      description={t(DashboardKeys.Home.DashboardDescription)}
    >
      {/* Quick Actions */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 sm:gap-4 mb-6 sm:mb-8">
        <a href="/Identity/Account/Manage" className="no-underline">
          <Card className="h-full group">
            <CardContent>
              <div className="flex items-center gap-3 mb-3">
                <span className="w-9 h-9 rounded-xl flex items-center justify-center text-primary bg-primary-subtle">
                  <svg
                    className="w-[18px] h-[18px]"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    viewBox="0 0 24 24"
                    aria-hidden="true"
                  >
                    <path d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                  </svg>
                </span>
                <span className="text-sm font-semibold text-text group-hover:text-primary transition-colors">
                  {t(DashboardKeys.Home.AccountCardTitle)}
                </span>
              </div>
              <p className="text-xs text-text-muted">
                {t(DashboardKeys.Home.AccountCardDescription)}
              </p>
            </CardContent>
          </Card>
        </a>
        {isDevelopment && (
          <a href="/swagger" className="no-underline">
            <Card className="h-full group">
              <CardContent>
                <div className="flex items-center gap-3 mb-3">
                  <span className="w-9 h-9 rounded-xl flex items-center justify-center text-accent bg-success-bg">
                    <svg
                      className="w-[18px] h-[18px]"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="2"
                      viewBox="0 0 24 24"
                      aria-hidden="true"
                    >
                      <path d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4" />
                    </svg>
                  </span>
                  <span className="text-sm font-semibold text-text group-hover:text-primary transition-colors">
                    {t(DashboardKeys.Home.ApiDocsCardTitle)}
                  </span>
                </div>
                <p className="text-xs text-text-muted">
                  {t(DashboardKeys.Home.ApiDocsCardDescription)}
                </p>
              </CardContent>
            </Card>
          </a>
        )}
        <a href="/health/live" className="no-underline">
          <Card className="h-full group">
            <CardContent>
              <div className="flex items-center gap-3 mb-3">
                <span className="w-9 h-9 rounded-xl flex items-center justify-center text-info bg-info-bg">
                  <svg
                    className="w-[18px] h-[18px]"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    viewBox="0 0 24 24"
                    aria-hidden="true"
                  >
                    <path d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                  </svg>
                </span>
                <span className="text-sm font-semibold text-text group-hover:text-primary transition-colors">
                  {t(DashboardKeys.Home.HealthCardTitle)}
                </span>
              </div>
              <p className="text-xs text-text-muted">
                {t(DashboardKeys.Home.HealthCardDescription)}
              </p>
            </CardContent>
          </Card>
        </a>
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2 lg:gap-6">
        <UserInfoPanel />
        <TokenTester />
      </div>

      <ApiTester />
    </PageShell>
  );
}

// --- Landing View ---

function LandingView({ isDevelopment }: { isDevelopment: boolean }) {
  const { t } = useTranslation('Dashboard');
  return (
    <Container className="flex items-center justify-center min-h-[calc(100vh-16rem)] px-4">
      <div className="text-center max-w-lg mx-auto w-full">
        {/* Inline style required: Tailwind gradient utilities cannot reference CSS custom properties */}
        <div
          className="w-14 h-14 sm:w-16 sm:h-16 rounded-2xl mx-auto mb-4 sm:mb-6 flex items-center justify-center text-white text-xl sm:text-2xl font-bold shadow-lg"
          style={{
            background: 'linear-gradient(135deg, var(--color-primary), var(--color-accent))',
          }}
        >
          S
        </div>
        <h1 className="text-3xl sm:text-4xl font-extrabold mb-3 tracking-tight">
          {t(DashboardKeys.Home.LandingTitle)}
        </h1>
        <p className="text-text-muted text-sm sm:text-base mb-6 sm:mb-8 max-w-sm mx-auto leading-relaxed">
          {t(DashboardKeys.Home.LandingDescription)}
        </p>

        <div className="flex flex-col sm:flex-row gap-3 justify-center">
          <Button asChild size="lg">
            <a href="/Identity/Account/Login" className="no-underline">
              {t(DashboardKeys.Home.LandingGetStarted)}
            </a>
          </Button>
          <Button asChild variant="secondary" size="lg">
            <a href="/Identity/Account/Register" className="no-underline">
              {t(DashboardKeys.Home.LandingCreateAccount)}
            </a>
          </Button>
        </div>

        {isDevelopment && (
          <Alert variant="warning" className="mt-6 text-left text-xs">
            <AlertTitle>{t(DashboardKeys.Home.LandingQuickStartTitle)}</AlertTitle>
            <AlertDescription>
              Email:{' '}
              <code className="bg-warning-bg px-1.5 py-0.5 rounded text-xs font-mono font-medium">
                admin@simplemodule.dev
              </code>
              &nbsp; Password:{' '}
              <code className="bg-warning-bg px-1.5 py-0.5 rounded text-xs font-mono font-medium">
                Admin123!
              </code>
            </AlertDescription>
          </Alert>
        )}

        <div className="flex gap-5 justify-center mt-8 text-sm">
          {isDevelopment && (
            <>
              <a
                href="/swagger"
                className="text-text-muted no-underline hover:text-primary transition-colors"
              >
                {t(DashboardKeys.Home.LandingApiDocs)}
              </a>
              <span className="text-border">&middot;</span>
            </>
          )}
          <a
            href="/health/live"
            className="text-text-muted no-underline hover:text-primary transition-colors"
          >
            {t(DashboardKeys.Home.LandingHealthCheck)}
          </a>
        </div>
      </div>
    </Container>
  );
}

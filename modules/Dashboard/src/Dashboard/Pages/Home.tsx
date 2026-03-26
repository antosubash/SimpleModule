import {
  Alert,
  AlertDescription,
  AlertTitle,
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Container,
  PageHeader,
  Spinner,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import React from 'react';

interface HomeProps {
  isAuthenticated: boolean;
  displayName: string;
  isDevelopment: boolean;
}

export default function Home({ isAuthenticated, displayName, isDevelopment }: HomeProps) {
  return isAuthenticated ? (
    <DashboardView displayName={displayName} />
  ) : (
    <LandingView isDevelopment={isDevelopment} />
  );
}

// --- Dashboard View ---

function DashboardView({ displayName }: { displayName: string }) {
  return (
    <Container className="space-y-6">
      <PageHeader
        title={`Welcome back, ${displayName}`}
        description="Here's your development dashboard"
      />

      {/* Quick Actions */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-8">
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
                  Account
                </span>
              </div>
              <p className="text-xs text-text-muted">Manage your profile and security settings</p>
            </CardContent>
          </Card>
        </a>
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
                  API Docs
                </span>
              </div>
              <p className="text-xs text-text-muted">Explore endpoints and test requests</p>
            </CardContent>
          </Card>
        </a>
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
                  Health
                </span>
              </div>
              <p className="text-xs text-text-muted">Check system status and diagnostics</p>
            </CardContent>
          </Card>
        </a>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <UserInfoPanel />
        <TokenTester />
      </div>

      <ApiTester />
    </Container>
  );
}

// --- User Info Panel ---

interface UserInfo {
  displayName?: string;
  name?: string;
  email?: string;
  id?: string | { value: string };
  roles?: string | string[];
}

function InfoRow({
  label,
  value,
  monospace,
}: {
  label: string;
  value: string;
  monospace?: boolean;
}) {
  return (
    <div className="flex justify-between items-center py-3 text-sm border-b border-border last:border-b-0">
      <span className="text-text-muted text-xs uppercase tracking-wide">{label}</span>
      <span
        className={monospace ? 'font-mono text-xs text-text-secondary' : 'font-medium text-text'}
      >
        {value}
      </span>
    </div>
  );
}

function UserInfoPanel() {
  const [userInfo, setUserInfo] = React.useState<UserInfo | null>(null);
  const [error, setError] = React.useState<string | null>(null);
  const [loading, setLoading] = React.useState(true);

  React.useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const res = await fetch('/api/users/me');
        if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
        const data = await res.json();
        if (!cancelled) {
          setUserInfo(data);
          setLoading(false);
        }
      } catch (e: unknown) {
        if (!cancelled) {
          setError(e instanceof Error ? e.message : String(e));
          setLoading(false);
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <Card>
      <CardHeader>
        <CardTitle>User Info</CardTitle>
      </CardHeader>
      <CardContent>
        {loading && (
          <div className="text-text-muted text-sm flex items-center gap-2">
            Loading user info
            <Spinner size="sm" />
          </div>
        )}
        {error && <span className="text-danger text-sm">Failed to load: {error}</span>}
        {userInfo && (
          <>
            <InfoRow label="Name" value={userInfo.displayName || userInfo.name || '-'} />
            <InfoRow label="Email" value={userInfo.email || '-'} />
            <InfoRow
              label="ID"
              value={
                typeof userInfo.id === 'object' && userInfo.id
                  ? userInfo.id.value
                  : userInfo.id || '-'
              }
              monospace
            />
            {userInfo.roles && (
              <InfoRow
                label="Roles"
                value={Array.isArray(userInfo.roles) ? userInfo.roles.join(', ') : userInfo.roles}
              />
            )}
          </>
        )}
      </CardContent>
    </Card>
  );
}

// --- Token Tester ---

function generateCodeVerifier(): string {
  const arr = new Uint8Array(32);
  crypto.getRandomValues(arr);
  return btoa(String.fromCharCode(...arr))
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=/g, '');
}

async function generateCodeChallenge(verifier: string): Promise<string> {
  const data = new TextEncoder().encode(verifier);
  const hash = await crypto.subtle.digest('SHA-256', data);
  return btoa(String.fromCharCode(...new Uint8Array(hash)))
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=/g, '');
}

interface DecodedClaim {
  key: string;
  value: string;
}

function decodeToken(token: string): DecodedClaim[] | null {
  try {
    const parts = token.split('.');
    const payload = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/')));
    return Object.keys(payload).map((key) => {
      const val = payload[key];
      const display =
        key === 'exp' || key === 'iat' || key === 'nbf'
          ? new Date(val * 1000).toLocaleString()
          : typeof val === 'object'
            ? JSON.stringify(val)
            : String(val);
      return { key, value: display };
    });
  } catch {
    return null;
  }
}

function TokenTester() {
  const [token, setToken] = React.useState<string | null>(null);
  const [authorizing, setAuthorizing] = React.useState(false);

  const claims = React.useMemo(() => (token ? decodeToken(token) : null), [token]);

  React.useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const code = params.get('code');
    const state = params.get('state');
    if (!code) return;

    window.history.replaceState({}, '', '/');

    const verifier = sessionStorage.getItem('pkce_verifier');
    const savedState = sessionStorage.getItem('pkce_state');

    if (state !== savedState) {
      alert('OAuth state mismatch');
      return;
    }

    sessionStorage.removeItem('pkce_verifier');
    sessionStorage.removeItem('pkce_state');

    (async () => {
      try {
        const res = await fetch('/connect/token', {
          method: 'POST',
          headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
          body: new URLSearchParams({
            grant_type: 'authorization_code',
            client_id: 'simplemodule-client',
            code,
            redirect_uri: `${window.location.origin}/oauth-callback`,
            code_verifier: verifier ?? '',
          }),
        });

        if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
        const data = await res.json();
        setToken(data.access_token);
      } catch (e: unknown) {
        alert(`Token exchange failed: ${e instanceof Error ? e.message : String(e)}`);
      }
    })();
  }, []);

  const startOAuthFlow = async () => {
    setAuthorizing(true);

    const verifier = generateCodeVerifier();
    const challenge = await generateCodeChallenge(verifier);
    const state = crypto.randomUUID();

    sessionStorage.setItem('pkce_verifier', verifier);
    sessionStorage.setItem('pkce_state', state);

    const params = new URLSearchParams({
      response_type: 'code',
      client_id: 'simplemodule-client',
      redirect_uri: `${window.location.origin}/oauth-callback`,
      scope: 'openid profile email',
      state,
      code_challenge: challenge,
      code_challenge_method: 'S256',
    });

    window.location.href = `/connect/authorize?${params.toString()}`;
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Token Tester</CardTitle>
      </CardHeader>
      <CardContent>
        <h3 className="text-sm font-semibold mb-1">OAuth2 Authorization Code + PKCE</h3>
        <p className="text-xs text-text-muted mb-4">
          Obtain an access token using the{' '}
          <code className="bg-surface-raised px-1.5 py-0.5 rounded text-xs font-mono">
            simplemodule-client
          </code>{' '}
          application.
        </p>
        <Button size="sm" disabled={authorizing} onClick={startOAuthFlow}>
          {authorizing ? (
            <>
              Authorizing
              <Spinner size="sm" />
            </>
          ) : (
            'Get Access Token'
          )}
        </Button>
        {token && (
          <div>
            <div className="bg-surface-raised rounded-xl p-3 font-mono text-xs break-all max-h-30 overflow-auto mt-4">
              {token}
            </div>
            {claims && (
              <>
                <h3 className="text-sm font-semibold mt-4 mb-2">Decoded Claims</h3>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Claim</TableHead>
                      <TableHead>Value</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {claims.map((claim) => (
                      <TableRow key={claim.key}>
                        <TableCell>{claim.key}</TableCell>
                        <TableCell>{claim.value}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  );
}

// --- API Tester ---

const API_ENDPOINTS = ['/api/users/me', '/api/users', '/api/products', '/api/orders'];

function ApiTester() {
  const [status, setStatus] = React.useState<{
    loading: boolean;
    ok?: boolean;
    code?: string;
    statusText?: string;
    url?: string;
    error?: string;
  } | null>(null);
  const [response, setResponse] = React.useState('Click an endpoint above to make a request.');

  const getAccessToken = (): string | null => {
    const codeBlocks = document.querySelectorAll('.font-mono.text-xs.break-all');
    for (const block of codeBlocks) {
      const text = block.textContent || '';
      if (text.includes('.') && text.length > 50) {
        return text.trim();
      }
    }
    return null;
  };

  const callApi = async (url: string) => {
    setStatus({ loading: true, url });
    setResponse('');

    const headers: Record<string, string> = {};
    const accessToken = getAccessToken();
    if (accessToken) {
      headers.Authorization = `Bearer ${accessToken}`;
    }

    try {
      const res = await fetch(url, { headers });
      const text = await res.text();
      setStatus({
        loading: false,
        ok: res.ok,
        code: String(res.status),
        statusText: res.statusText,
        url,
      });
      try {
        setResponse(JSON.stringify(JSON.parse(text), null, 2));
      } catch {
        setResponse(text);
      }
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : String(e);
      setStatus({ loading: false, ok: false, code: 'Error', error: msg, url });
      setResponse(msg);
    }
  };

  return (
    <Card className="mt-6">
      <CardHeader>
        <CardTitle>API Tester</CardTitle>
      </CardHeader>
      <CardContent>
        <h3 className="text-sm font-semibold mb-3">Call Protected Endpoints</h3>
        <div className="flex gap-2 flex-wrap mb-4">
          {API_ENDPOINTS.map((url) => (
            <Button key={url} variant="outline" size="sm" onClick={() => callApi(url)}>
              GET {url}
            </Button>
          ))}
        </div>
        <div className="text-xs text-text-muted mb-2 flex items-center gap-2">
          {status?.loading && (
            <>
              Calling {status.url}
              <Spinner size="sm" />
            </>
          )}
          {status && !status.loading && (
            <>
              <Badge variant={status.ok ? 'success' : 'danger'}>{status.code}</Badge>{' '}
              {status.error ? status.error : `${status.statusText} \u2014 ${status.url}`}
            </>
          )}
        </div>
        <div className="bg-surface-raised rounded-xl p-3 font-mono text-xs whitespace-pre-wrap max-h-50 overflow-auto">
          {response}
        </div>
      </CardContent>
    </Card>
  );
}

// --- Landing View ---

function LandingView({ isDevelopment }: { isDevelopment: boolean }) {
  return (
    <Container className="flex items-center justify-center min-h-[calc(100vh-16rem)]">
      <div className="text-center max-w-lg mx-auto">
        {/* Inline style required: Tailwind gradient utilities cannot reference CSS custom properties */}
        <div
          className="w-16 h-16 rounded-2xl mx-auto mb-6 flex items-center justify-center text-white text-2xl font-bold shadow-lg"
          style={{
            background: 'linear-gradient(135deg, var(--color-primary), var(--color-accent))',
          }}
        >
          S
        </div>
        <h1 className="text-4xl font-extrabold mb-3 tracking-tight">SimpleModule</h1>
        <p className="text-text-muted text-base mb-8 max-w-sm mx-auto leading-relaxed">
          Modular monolith framework for .NET with compile&#8209;time module&nbsp;discovery
        </p>

        <div className="flex gap-3 justify-center flex-wrap">
          <Button asChild size="lg">
            <a href="/Identity/Account/Login" className="no-underline">
              Get Started
            </a>
          </Button>
          <Button asChild variant="secondary" size="lg">
            <a href="/Identity/Account/Register" className="no-underline">
              Create Account
            </a>
          </Button>
        </div>

        {isDevelopment && (
          <Alert variant="warning" className="mt-6 text-left text-xs">
            <AlertTitle>Quick Start (Development Only)</AlertTitle>
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
          <a
            href="/swagger"
            className="text-text-muted no-underline hover:text-primary transition-colors"
          >
            API Docs
          </a>
          <span className="text-border">&middot;</span>
          <a
            href="/health/live"
            className="text-text-muted no-underline hover:text-primary transition-colors"
          >
            Health Check
          </a>
        </div>
      </div>
    </Container>
  );
}

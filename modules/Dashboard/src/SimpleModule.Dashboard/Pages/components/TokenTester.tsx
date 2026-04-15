import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Spinner,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import React from 'react';
import { DashboardKeys } from '@/Locales/keys';

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

export function TokenTester() {
  const { t } = useTranslation('Dashboard');
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
        <CardTitle>{t(DashboardKeys.Home.TokenTesterTitle)}</CardTitle>
      </CardHeader>
      <CardContent>
        <h3 className="text-sm font-semibold mb-1">{t(DashboardKeys.Home.TokenTesterSubtitle)}</h3>
        <p className="text-xs text-text-muted mb-4">
          {t(DashboardKeys.Home.TokenTesterDescription, { clientId: 'simplemodule-client' })}
        </p>
        <Button size="sm" disabled={authorizing} onClick={startOAuthFlow}>
          {authorizing ? (
            <>
              {t(DashboardKeys.Home.TokenTesterAuthorizing)}
              <Spinner size="sm" />
            </>
          ) : (
            t(DashboardKeys.Home.TokenTesterGetToken)
          )}
        </Button>
        {token && (
          <div>
            <div className="bg-surface-raised rounded-xl p-2.5 sm:p-3 font-mono text-xs break-all max-h-30 overflow-auto mt-4">
              {token}
            </div>
            {claims && (
              <>
                <h3 className="text-sm font-semibold mt-4 mb-2">
                  {t(DashboardKeys.Home.TokenTesterDecodedClaims)}
                </h3>
                <div className="overflow-x-auto -mx-4 px-4 sm:mx-0 sm:px-0">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>{t(DashboardKeys.Home.TokenTesterColClaim)}</TableHead>
                        <TableHead>{t(DashboardKeys.Home.TokenTesterColValue)}</TableHead>
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
                </div>
              </>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  );
}

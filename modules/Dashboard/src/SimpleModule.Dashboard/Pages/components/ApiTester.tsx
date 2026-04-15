import { useTranslation } from '@simplemodule/client/use-translation';
import { Badge, Button, Card, CardContent, CardHeader, CardTitle, Spinner } from '@simplemodule/ui';
import React from 'react';
import { DashboardKeys } from '@/Locales/keys';

const API_ENDPOINTS = ['/api/users/me', '/api/users', '/api/products', '/api/orders'];

export function ApiTester() {
  const { t } = useTranslation('Dashboard');
  const [status, setStatus] = React.useState<{
    loading: boolean;
    ok?: boolean;
    code?: string;
    statusText?: string;
    url?: string;
    error?: string;
  } | null>(null);
  const [response, setResponse] = React.useState(t(DashboardKeys.Home.ApiTesterDefaultResponse));

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
        <CardTitle>{t(DashboardKeys.Home.ApiTesterTitle)}</CardTitle>
      </CardHeader>
      <CardContent>
        <h3 className="text-sm font-semibold mb-3">{t(DashboardKeys.Home.ApiTesterSubtitle)}</h3>
        <div className="flex gap-2 flex-wrap mb-4 overflow-x-auto">
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

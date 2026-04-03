import { sleep } from 'k6';
import http from 'k6/http';
import { type AuthResult, authenticate, authHeaders } from '../lib/auth.ts';
import { config, defaultThresholds, loadProfiles, tlsOptions } from '../lib/config.ts';
import { checkResponse } from '../lib/helpers.ts';

const profile = __ENV.K6_PROFILE || 'smoke';

export const options = {
  ...tlsOptions,
  stages: loadProfiles[profile]?.stages || loadProfiles.smoke.stages,
  thresholds: {
    ...defaultThresholds,
    'http_req_duration{name:query-logs}': ['p(95)<500'],
    'http_req_duration{name:query-logs-filtered}': ['p(95)<500'],
    'http_req_duration{name:get-log-entry}': ['p(95)<500'],
    'http_req_duration{name:audit-stats}': ['p(95)<500'],
    'http_req_duration{name:export-csv}': ['p(95)<2000'],
    'http_req_duration{name:export-json}': ['p(95)<2000'],
  },
};

export function setup(): AuthResult {
  return authenticate();
}

export default function (auth: AuthResult) {
  const headers = authHeaders(auth.accessToken);
  const baseUrl = `${config.baseUrl}/api/audit-logs`;

  const queryRes = http.get(baseUrl, { headers, tags: { name: 'query-logs' } });
  checkResponse(queryRes, 'query-logs');

  const filteredRes = http.get(`${baseUrl}?module=Products&pageSize=10&sortDescending=true`, {
    headers,
    tags: { name: 'query-logs-filtered' },
  });
  checkResponse(filteredRes, 'query-logs-filtered');

  if (queryRes.status === 200) {
    try {
      const result = JSON.parse(queryRes.body as string);
      if (result.items?.length > 0) {
        const entryId = result.items[0].id;
        const getRes = http.get(`${baseUrl}/${entryId}`, {
          headers,
          tags: { name: 'get-log-entry' },
        });
        checkResponse(getRes, 'get-log-entry');
      }
    } catch {
      // empty audit log is fine
    }
  }

  const now = new Date().toISOString();
  const yesterday = new Date(Date.now() - 86400000).toISOString();
  const statsRes = http.get(`${baseUrl}/stats?from=${yesterday}&to=${now}`, {
    headers,
    tags: { name: 'audit-stats' },
  });
  checkResponse(statsRes, 'audit-stats');

  const csvRes = http.get(`${baseUrl}/export?format=csv&pageSize=10`, {
    headers,
    tags: { name: 'export-csv' },
  });
  checkResponse(csvRes, 'export-csv');

  const jsonRes = http.get(`${baseUrl}/export?format=json&pageSize=10`, {
    headers,
    tags: { name: 'export-json' },
  });
  checkResponse(jsonRes, 'export-json');

  sleep(1);
}

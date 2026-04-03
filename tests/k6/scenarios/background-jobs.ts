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
    'http_req_duration{name:list-jobs}': ['p(95)<500'],
    'http_req_duration{name:list-jobs-filtered}': ['p(95)<500'],
    'http_req_duration{name:get-job-by-id}': ['p(95)<500'],
    'http_req_duration{name:list-recurring}': ['p(95)<500'],
  },
};

export function setup(): AuthResult {
  return authenticate();
}

export default function (auth: AuthResult) {
  const headers = authHeaders(auth.accessToken);
  const baseUrl = `${config.baseUrl}/api/jobs`;

  const listRes = http.get(baseUrl, { headers, tags: { name: 'list-jobs' } });
  checkResponse(listRes, 'list-jobs');

  const filteredRes = http.get(`${baseUrl}?page=1&pageSize=10`, {
    headers,
    tags: { name: 'list-jobs-filtered' },
  });
  checkResponse(filteredRes, 'list-jobs-filtered');

  if (listRes.status === 200) {
    try {
      const jobs = JSON.parse(listRes.body as string);
      const items = jobs.items || jobs.data || jobs;
      if (Array.isArray(items) && items.length > 0) {
        const jobId = items[0].id;
        const getRes = http.get(`${baseUrl}/${jobId}`, {
          headers,
          tags: { name: 'get-job-by-id' },
        });
        checkResponse(getRes, 'get-job-by-id', getRes.status === 404 ? 404 : 200);
      }
    } catch {
      // ignore parse errors
    }
  }

  const recurringRes = http.get(`${baseUrl}/recurring`, {
    headers,
    tags: { name: 'list-recurring' },
  });
  checkResponse(recurringRes, 'list-recurring');

  sleep(1);
}

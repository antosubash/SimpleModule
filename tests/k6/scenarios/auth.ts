import { check, sleep } from 'k6';
import http from 'k6/http';
import { authenticate, authHeaders } from '../lib/auth.ts';
import { config, defaultThresholds, loadProfiles, tlsOptions } from '../lib/config.ts';

const profile = __ENV.K6_PROFILE || 'smoke';

export const options = {
  ...tlsOptions,
  stages: loadProfiles[profile]?.stages || loadProfiles.smoke.stages,
  thresholds: {
    ...defaultThresholds,
    'http_req_duration{name:auth}': ['p(95)<1000'],
    'http_req_duration{name:current-user}': ['p(95)<500'],
  },
};

export default function () {
  const auth = authenticate();

  const userRes = http.get(`${config.baseUrl}/api/users/me`, {
    headers: authHeaders(auth.accessToken),
    tags: { name: 'current-user' },
  });
  check(userRes, {
    'current-user: status 200': (r) => r.status === 200,
    'current-user: has email': (r) => {
      try {
        return JSON.parse(r.body as string).email !== undefined;
      } catch {
        return false;
      }
    },
  });

  sleep(1);
}

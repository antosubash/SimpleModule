import http from 'k6/http';
import { check, sleep } from 'k6';
import { config, defaultThresholds, loadProfiles, tlsOptions } from '../lib/config.js';
import { authenticate, authHeaders } from '../lib/auth.js';

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
  // Test: obtain access token via password grant
  const auth = authenticate();

  // Test: call authenticated API endpoint with token
  const userRes = http.get(`${config.baseUrl}/api/users/me`, {
    headers: authHeaders(auth.accessToken),
    tags: { name: 'current-user' },
  });
  check(userRes, {
    'current-user: status 200': (r) => r.status === 200,
    'current-user: has email': (r) => {
      try {
        return JSON.parse(r.body).email !== undefined;
      } catch {
        return false;
      }
    },
  });

  sleep(1);
}

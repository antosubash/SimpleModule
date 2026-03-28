import http from 'k6/http';
import { check, sleep } from 'k6';
import { config, defaultThresholds, loadProfiles } from '../lib/config.js';
import { authenticate, authHeaders } from '../lib/auth.js';

const profile = __ENV.K6_PROFILE || 'smoke';

export const options = {
  stages: loadProfiles[profile]?.stages || loadProfiles.smoke.stages,
  thresholds: {
    ...defaultThresholds,
    'http_req_duration{name:auth}': ['p(95)<1000'],
    'http_req_duration{name:userinfo}': ['p(95)<500'],
  },
};

export default function () {
  // Test: obtain access token via password grant
  const auth = authenticate();

  // Test: call userinfo endpoint with token
  const userinfoRes = http.get(`${config.baseUrl}/connect/userinfo`, {
    headers: authHeaders(auth.accessToken),
    tags: { name: 'userinfo' },
  });
  check(userinfoRes, {
    'userinfo: status 200': (r) => r.status === 200,
    'userinfo: has sub claim': (r) => {
      try {
        return JSON.parse(r.body).sub !== undefined;
      } catch {
        return false;
      }
    },
  });

  sleep(1);
}

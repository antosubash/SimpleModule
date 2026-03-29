import http from 'k6/http';
import { check, sleep } from 'k6';
import { config, defaultThresholds, loadProfiles, tlsOptions } from '../lib/config.js';

const profile = __ENV.K6_PROFILE || 'smoke';

export const options = {
  ...tlsOptions,
  stages: loadProfiles[profile]?.stages || loadProfiles.smoke.stages,
  thresholds: {
    ...defaultThresholds,
    'http_req_duration{name:health}': ['p(95)<200'],
    'http_req_duration{name:alive}': ['p(95)<200'],
  },
};

export default function () {
  // Health check endpoint
  const healthRes = http.get(`${config.baseUrl}/health`, {
    tags: { name: 'health' },
  });
  check(healthRes, {
    'health: status 200': (r) => r.status === 200,
    'health: is healthy': (r) => r.body && r.body.includes('Healthy'),
  });

  // Liveness check endpoint
  const aliveRes = http.get(`${config.baseUrl}/alive`, {
    tags: { name: 'alive' },
  });
  check(aliveRes, {
    'alive: status 200': (r) => r.status === 200,
  });

  sleep(1);
}

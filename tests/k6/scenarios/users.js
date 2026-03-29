import http from 'k6/http';
import { sleep } from 'k6';
import { config, defaultThresholds, loadProfiles, tlsOptions } from '../lib/config.js';
import { authenticate, authHeaders } from '../lib/auth.js';
import { checkResponse, randomString } from '../lib/helpers.js';

const profile = __ENV.K6_PROFILE || 'smoke';

export const options = {
  ...tlsOptions,
  stages: loadProfiles[profile]?.stages || loadProfiles.smoke.stages,
  thresholds: {
    ...defaultThresholds,
    'http_req_duration{name:list-users}': ['p(95)<500'],
    'http_req_duration{name:create-user}': ['p(95)<800'],
    'http_req_duration{name:get-user}': ['p(95)<500'],
    'http_req_duration{name:update-user}': ['p(95)<500'],
    'http_req_duration{name:delete-user}': ['p(95)<500'],
    'http_req_duration{name:get-current-user}': ['p(95)<300'],
  },
};

export function setup() {
  return authenticate();
}

export default function (auth) {
  const headers = authHeaders(auth.accessToken);
  const baseUrl = `${config.baseUrl}/api/users`;

  // List users
  const listRes = http.get(baseUrl, {
    headers,
    tags: { name: 'list-users' },
  });
  checkResponse(listRes, 'list-users');

  // Get current user
  const meRes = http.get(`${baseUrl}/me`, {
    headers,
    tags: { name: 'get-current-user' },
  });
  checkResponse(meRes, 'get-current-user');

  // Create user
  const email = `k6-${randomString(8)}@test.dev`;
  const createRes = http.post(
    baseUrl,
    JSON.stringify({
      email,
      displayName: `K6 User ${randomString(4)}`,
      password: 'K6Test123!',
    }),
    { headers, tags: { name: 'create-user' } },
  );
  checkResponse(createRes, 'create-user', 201);

  if (createRes.status === 201) {
    const userId = JSON.parse(createRes.body).id;

    // Get user by ID
    const getRes = http.get(`${baseUrl}/${userId}`, {
      headers,
      tags: { name: 'get-user' },
    });
    checkResponse(getRes, 'get-user');

    // Update user
    const updateRes = http.put(
      `${baseUrl}/${userId}`,
      JSON.stringify({
        email,
        displayName: `K6 Updated ${randomString(4)}`,
      }),
      { headers, tags: { name: 'update-user' } },
    );
    checkResponse(updateRes, 'update-user');

    // Delete user (cleanup)
    const deleteRes = http.del(`${baseUrl}/${userId}`, null, {
      headers,
      tags: { name: 'delete-user' },
    });
    checkResponse(deleteRes, 'delete-user', 204);
  }

  sleep(1);
}

import { sleep } from 'k6';
import http from 'k6/http';
import { authenticate, authHeaders } from '../lib/auth.js';
import { config, defaultThresholds, loadProfiles, tlsOptions } from '../lib/config.js';
import { checkResponse, randomString } from '../lib/helpers.js';

const profile = __ENV.K6_PROFILE || 'smoke';

export const options = {
  ...tlsOptions,
  stages: loadProfiles[profile]?.stages || loadProfiles.smoke.stages,
  thresholds: {
    ...defaultThresholds,
    'http_req_duration{name:list-tenants}': ['p(95)<500'],
    'http_req_duration{name:create-tenant}': ['p(95)<500'],
    'http_req_duration{name:get-tenant}': ['p(95)<500'],
    'http_req_duration{name:update-tenant}': ['p(95)<500'],
    'http_req_duration{name:change-status}': ['p(95)<500'],
    'http_req_duration{name:add-host}': ['p(95)<500'],
    'http_req_duration{name:remove-host}': ['p(95)<500'],
    'http_req_duration{name:get-tenant-features}': ['p(95)<500'],
    'http_req_duration{name:set-tenant-feature}': ['p(95)<500'],
    'http_req_duration{name:delete-tenant-feature}': ['p(95)<500'],
    'http_req_duration{name:delete-tenant}': ['p(95)<500'],
  },
};

export function setup() {
  return authenticate();
}

export default function (auth) {
  const headers = authHeaders(auth.accessToken);
  const baseUrl = `${config.baseUrl}/api/tenants`;
  const suffix = randomString(6);

  // --- List tenants ---
  const listRes = http.get(baseUrl, {
    headers,
    tags: { name: 'list-tenants' },
  });
  checkResponse(listRes, 'list-tenants');

  // --- Create tenant ---
  const createRes = http.post(
    baseUrl,
    JSON.stringify({
      name: `k6-tenant-${suffix}`,
      slug: `k6-${suffix}`,
    }),
    { headers, tags: { name: 'create-tenant' } },
  );
  checkResponse(createRes, 'create-tenant', 201);

  if (createRes.status !== 201) {
    sleep(1);
    return;
  }

  let tenant;
  try {
    tenant = JSON.parse(createRes.body);
  } catch (_) {
    sleep(1);
    return;
  }
  const tenantId = tenant.id;

  // --- Get tenant by ID ---
  const getRes = http.get(`${baseUrl}/${tenantId}`, {
    headers,
    tags: { name: 'get-tenant' },
  });
  checkResponse(getRes, 'get-tenant');

  // --- Update tenant ---
  const updateRes = http.put(
    `${baseUrl}/${tenantId}`,
    JSON.stringify({
      name: `k6-tenant-${suffix}-updated`,
    }),
    { headers, tags: { name: 'update-tenant' } },
  );
  checkResponse(updateRes, 'update-tenant');

  // --- Change tenant status (suspend) ---
  const statusRes = http.put(
    `${baseUrl}/${tenantId}/status`,
    JSON.stringify({ status: 1 }), // 1 = Suspended
    { headers, tags: { name: 'change-status' } },
  );
  checkResponse(statusRes, 'change-status');

  // --- Restore status (active) ---
  http.put(
    `${baseUrl}/${tenantId}/status`,
    JSON.stringify({ status: 0 }), // 0 = Active
    { headers, tags: { name: 'change-status' } },
  );

  // --- Add a host ---
  const hostName = `k6-${suffix}.example.com`;
  const addHostRes = http.post(`${baseUrl}/${tenantId}/hosts`, JSON.stringify({ hostName }), {
    headers,
    tags: { name: 'add-host' },
  });
  checkResponse(addHostRes, 'add-host', 201);

  // --- Remove host ---
  if (addHostRes.status === 201) {
    try {
      const host = JSON.parse(addHostRes.body);
      const hostId = host.id;
      const removeHostRes = http.del(`${baseUrl}/${tenantId}/hosts/${hostId}`, null, {
        headers,
        tags: { name: 'remove-host' },
      });
      checkResponse(removeHostRes, 'remove-host', 204);
    } catch (_) {
      // ignore
    }
  }

  // --- Tenant feature flags ---
  const featuresRes = http.get(`${baseUrl}/${tenantId}/features`, {
    headers,
    tags: { name: 'get-tenant-features' },
  });
  checkResponse(featuresRes, 'get-tenant-features');

  // Set a feature override (use a known flag if available)
  if (featuresRes.status === 200) {
    try {
      const features = JSON.parse(featuresRes.body);
      const flags = features.flags || features;
      if (Array.isArray(flags) && flags.length > 0) {
        const flagName = flags[0].name;

        const setFeatureRes = http.put(
          `${baseUrl}/${tenantId}/features/${flagName}`,
          JSON.stringify({ isEnabled: false }),
          { headers, tags: { name: 'set-tenant-feature' } },
        );
        checkResponse(setFeatureRes, 'set-tenant-feature');

        // Delete the feature override
        const delFeatureRes = http.del(`${baseUrl}/${tenantId}/features/${flagName}`, null, {
          headers,
          tags: { name: 'delete-tenant-feature' },
        });
        checkResponse(delFeatureRes, 'delete-tenant-feature', 204);
      }
    } catch (_) {
      // ignore
    }
  }

  // --- Delete tenant (cleanup) ---
  const delRes = http.del(`${baseUrl}/${tenantId}`, null, {
    headers,
    tags: { name: 'delete-tenant' },
  });
  checkResponse(delRes, 'delete-tenant', 204);

  sleep(1);
}

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
    'http_req_duration{name:list-flags}': ['p(95)<500'],
    'http_req_duration{name:check-flag}': ['p(95)<300'],
    'http_req_duration{name:update-flag}': ['p(95)<500'],
    'http_req_duration{name:get-overrides}': ['p(95)<500'],
    'http_req_duration{name:set-override}': ['p(95)<500'],
    'http_req_duration{name:delete-override}': ['p(95)<500'],
  },
};

export function setup() {
  return authenticate();
}

export default function (auth) {
  const headers = authHeaders(auth.accessToken);
  const baseUrl = `${config.baseUrl}/api/feature-flags`;

  // --- List all feature flags ---
  const listRes = http.get(baseUrl, {
    headers,
    tags: { name: 'list-flags' },
  });
  checkResponse(listRes, 'list-flags');

  // Find a flag to work with
  let flagName = null;
  let originalEnabled = true;
  if (listRes.status === 200) {
    try {
      const flags = JSON.parse(listRes.body);
      const items = Array.isArray(flags) ? flags : flags.items || flags.data || [];
      if (items.length > 0) {
        flagName = items[0].name;
        originalEnabled = items[0].isEnabled;
      }
    } catch (_) {
      // ignore
    }
  }

  if (flagName) {
    // --- Check flag status ---
    const checkRes = http.get(`${baseUrl}/check/${flagName}`, {
      headers,
      tags: { name: 'check-flag' },
    });
    checkResponse(checkRes, 'check-flag');

    // --- Toggle flag (and restore) ---
    const updateRes = http.put(
      `${baseUrl}/${flagName}`,
      JSON.stringify({ isEnabled: !originalEnabled }),
      { headers, tags: { name: 'update-flag' } },
    );
    checkResponse(updateRes, 'update-flag');

    // Restore original state
    http.put(`${baseUrl}/${flagName}`, JSON.stringify({ isEnabled: originalEnabled }), {
      headers,
      tags: { name: 'update-flag' },
    });

    // --- Get overrides ---
    const overridesRes = http.get(`${baseUrl}/${flagName}/overrides`, {
      headers,
      tags: { name: 'get-overrides' },
    });
    checkResponse(overridesRes, 'get-overrides');

    // --- Create and delete an override ---
    const overrideRes = http.post(
      `${baseUrl}/${flagName}/overrides`,
      JSON.stringify({
        overrideType: 0, // 0=User, 1=Role, 2=Tenant
        overrideValue: `k6-test-${randomString(6)}`,
        isEnabled: true,
      }),
      { headers, tags: { name: 'set-override' } },
    );
    checkResponse(overrideRes, 'set-override', 201);

    if (overrideRes.status === 201) {
      try {
        const override = JSON.parse(overrideRes.body);
        const overrideId = override.id;
        const delRes = http.del(`${baseUrl}/overrides/${overrideId}`, null, {
          headers,
          tags: { name: 'delete-override' },
        });
        checkResponse(delRes, 'delete-override', 204);
      } catch (_) {
        // ignore
      }
    }
  }

  sleep(1);
}

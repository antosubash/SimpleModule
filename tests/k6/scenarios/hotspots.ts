import { sleep } from 'k6';
import http from 'k6/http';
import { authenticate, authHeaders } from '../lib/auth.ts';
import { config } from '../lib/config.ts';
import {
  runAuditAndFileRequests,
  runMiscRequests,
  runSettingsRequests,
} from './hotspots/requests-admin.ts';
import {
  type HotspotSetupData,
  runHealthChecks,
  runOrderRequests,
  runPageBuilderRequests,
  runProductRequests,
} from './hotspots/requests-core.ts';
import { runTenantRequests } from './hotspots/requests-tenants.ts';

export { options } from './hotspots/metrics.ts';
export { handleSummary } from './hotspots/summary.ts';

export function setup(): HotspotSetupData {
  const auth = authenticate();
  const headers = authHeaders(auth.accessToken);

  const userRes = http.get(`${config.baseUrl}/api/users/me`, { headers });
  const userId = JSON.parse(userRes.body as string).id;

  const productRes = http.post(
    `${config.baseUrl}/api/products`,
    JSON.stringify({ name: `k6-hotspot-product-${Date.now()}`, price: 9.99 }),
    { headers },
  );
  const productId = JSON.parse(productRes.body as string).id;

  return { accessToken: auth.accessToken, userId, productId };
}

export default function (data: HotspotSetupData) {
  const headers = authHeaders(data.accessToken);

  runHealthChecks(headers);
  runProductRequests(headers);
  runOrderRequests(headers, data);
  runPageBuilderRequests(headers);
  runSettingsRequests(headers);
  runAuditAndFileRequests(headers);
  runMiscRequests(headers);
  runTenantRequests(headers);

  sleep(0.5);
}

import http from 'k6/http';
import { config } from '../../lib/config.ts';
import { randomString } from '../../lib/helpers.ts';
import { endpoints } from './metrics.ts';

type Headers = Record<string, string>;

export function runTenantRequests(headers: Headers) {
  let res = http.get(`${config.baseUrl}/api/tenants`, { headers });
  endpoints.listTenants.add(res.timings.duration);

  const tenantSuffix = randomString(8);
  res = http.post(
    `${config.baseUrl}/api/tenants`,
    JSON.stringify({ name: `k6-hs-${tenantSuffix}`, slug: `k6hs${tenantSuffix}` }),
    { headers },
  );
  endpoints.createTenant.add(res.timings.duration);

  if (res.status === 201) {
    const tid = JSON.parse(res.body as string).id;

    res = http.get(`${config.baseUrl}/api/tenants/${tid}`, { headers });
    endpoints.getTenant.add(res.timings.duration);

    res = http.get(`${config.baseUrl}/api/tenants/${tid}/features`, { headers });
    endpoints.getTenantFeatures.add(res.timings.duration);

    res = http.del(`${config.baseUrl}/api/tenants/${tid}`, null, { headers });
    endpoints.deleteTenant.add(res.timings.duration);
  }
}

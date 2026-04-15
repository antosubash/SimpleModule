import http from 'k6/http';
import { config } from '../../lib/config.ts';
import { endpoints } from './metrics.ts';

type Headers = Record<string, string>;

export function runSettingsRequests(headers: Headers) {
  let res = http.get(`${config.baseUrl}/api/settings`, { headers });
  endpoints.listSettings.add(res.timings.duration);

  res = http.get(`${config.baseUrl}/api/settings/definitions`, { headers });
  endpoints.getDefinitions.add(res.timings.duration);

  res = http.get(`${config.baseUrl}/api/settings/menus`, { headers });
  endpoints.listMenus.add(res.timings.duration);

  res = http.get(`${config.baseUrl}/api/settings/me`, { headers });
  endpoints.getUserSettings.add(res.timings.duration);
}

export function runAuditAndFileRequests(headers: Headers) {
  let res = http.get(`${config.baseUrl}/api/users`, { headers });
  endpoints.listUsers.add(res.timings.duration);

  res = http.get(`${config.baseUrl}/api/audit-logs`, { headers });
  endpoints.queryAuditLogs.add(res.timings.duration);

  const now = new Date().toISOString();
  const yesterday = new Date(Date.now() - 86400000).toISOString();
  res = http.get(`${config.baseUrl}/api/audit-logs/stats?from=${yesterday}&to=${now}`, {
    headers,
  });
  endpoints.auditStats.add(res.timings.duration);

  res = http.get(`${config.baseUrl}/api/files`, { headers });
  endpoints.listFiles.add(res.timings.duration);

  res = http.get(`${config.baseUrl}/api/files/folders`, { headers });
  endpoints.listFolders.add(res.timings.duration);
}

export function runMiscRequests(headers: Headers) {
  let res = http.get(`${config.baseUrl}/api/marketplace?take=5`);
  endpoints.searchMarketplace.add(res.timings.duration);

  res = http.get(`${config.baseUrl}/api/jobs`, { headers });
  endpoints.listJobs.add(res.timings.duration);

  res = http.get(`${config.baseUrl}/api/jobs/recurring`, { headers });
  endpoints.listRecurring.add(res.timings.duration);

  res = http.get(`${config.baseUrl}/api/feature-flags`, { headers });
  endpoints.listFlags.add(res.timings.duration);

  if (res.status === 200) {
    try {
      const flags = JSON.parse(res.body as string);
      const items = Array.isArray(flags) ? flags : flags.items || [];
      if (items.length > 0) {
        res = http.get(`${config.baseUrl}/api/feature-flags/check/${items[0].name}`, { headers });
        endpoints.checkFlag.add(res.timings.duration);
      }
    } catch {
      // ignore
    }
  }
}

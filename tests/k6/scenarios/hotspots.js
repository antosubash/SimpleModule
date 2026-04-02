import { textSummary } from 'https://jslib.k6.io/k6-summary/0.1.0/index.js';
import { sleep } from 'k6';
import http from 'k6/http';
import { Trend } from 'k6/metrics';
import { authenticate, authHeaders } from '../lib/auth.js';
import { config, defaultThresholds, loadProfiles, tlsOptions } from '../lib/config.js';
import { randomInt, randomString } from '../lib/helpers.js';

const profile = __ENV.K6_PROFILE || 'load';

// Per-endpoint latency trends for hotspot identification
const endpoints = {
  // Health
  health: new Trend('endpoint_health', true),
  alive: new Trend('endpoint_alive', true),

  // Auth
  token: new Trend('endpoint_token', true),
  currentUser: new Trend('endpoint_current_user', true),

  // Products
  listProducts: new Trend('endpoint_list_products', true),
  createProduct: new Trend('endpoint_create_product', true),
  getProduct: new Trend('endpoint_get_product', true),
  updateProduct: new Trend('endpoint_update_product', true),
  deleteProduct: new Trend('endpoint_delete_product', true),

  // Orders
  listOrders: new Trend('endpoint_list_orders', true),
  createOrder: new Trend('endpoint_create_order', true),
  getOrder: new Trend('endpoint_get_order', true),
  deleteOrder: new Trend('endpoint_delete_order', true),

  // PageBuilder
  listPages: new Trend('endpoint_list_pages', true),
  createPage: new Trend('endpoint_create_page', true),
  getPage: new Trend('endpoint_get_page', true),
  updatePageContent: new Trend('endpoint_update_page_content', true),
  publishPage: new Trend('endpoint_publish_page', true),
  unpublishPage: new Trend('endpoint_unpublish_page', true),
  deletePage: new Trend('endpoint_delete_page', true),
  listTags: new Trend('endpoint_list_tags', true),
  listTemplates: new Trend('endpoint_list_templates', true),

  // Settings
  listSettings: new Trend('endpoint_list_settings', true),
  getDefinitions: new Trend('endpoint_get_definitions', true),
  listMenus: new Trend('endpoint_list_menus', true),
  getUserSettings: new Trend('endpoint_get_user_settings', true),

  // Users
  listUsers: new Trend('endpoint_list_users', true),

  // AuditLogs
  queryAuditLogs: new Trend('endpoint_query_audit_logs', true),
  auditStats: new Trend('endpoint_audit_stats', true),

  // FileStorage
  listFiles: new Trend('endpoint_list_files', true),
  listFolders: new Trend('endpoint_list_folders', true),

  // Marketplace
  searchMarketplace: new Trend('endpoint_search_marketplace', true),

  // BackgroundJobs
  listJobs: new Trend('endpoint_list_jobs', true),
  listRecurring: new Trend('endpoint_list_recurring', true),

  // FeatureFlags
  listFlags: new Trend('endpoint_list_flags', true),
  checkFlag: new Trend('endpoint_check_flag', true),

  // Tenants
  listTenants: new Trend('endpoint_list_tenants', true),
  createTenant: new Trend('endpoint_create_tenant', true),
  getTenant: new Trend('endpoint_get_tenant', true),
  getTenantFeatures: new Trend('endpoint_get_tenant_features', true),
  deleteTenant: new Trend('endpoint_delete_tenant', true),
};

export const options = {
  ...tlsOptions,
  stages: loadProfiles[profile]?.stages || loadProfiles.load.stages,
  thresholds: {
    ...defaultThresholds,
    // Flag any endpoint over 500ms at p95 as a potential hotspot
    endpoint_health: ['p(95)<200'],
    endpoint_alive: ['p(95)<200'],
    endpoint_token: ['p(95)<1000'],
    endpoint_current_user: ['p(95)<500'],
    endpoint_list_products: ['p(95)<500'],
    endpoint_create_product: ['p(95)<500'],
    endpoint_get_product: ['p(95)<500'],
    endpoint_update_product: ['p(95)<500'],
    endpoint_delete_product: ['p(95)<500'],
    endpoint_list_orders: ['p(95)<500'],
    endpoint_create_order: ['p(95)<500'],
    endpoint_get_order: ['p(95)<500'],
    endpoint_delete_order: ['p(95)<500'],
    endpoint_list_pages: ['p(95)<500'],
    endpoint_create_page: ['p(95)<500'],
    endpoint_get_page: ['p(95)<500'],
    endpoint_update_page_content: ['p(95)<500'],
    endpoint_publish_page: ['p(95)<500'],
    endpoint_unpublish_page: ['p(95)<500'],
    endpoint_delete_page: ['p(95)<500'],
    endpoint_list_tags: ['p(95)<500'],
    endpoint_list_templates: ['p(95)<500'],
    endpoint_list_settings: ['p(95)<500'],
    endpoint_get_definitions: ['p(95)<500'],
    endpoint_list_menus: ['p(95)<500'],
    endpoint_get_user_settings: ['p(95)<500'],
    endpoint_list_users: ['p(95)<500'],
    endpoint_query_audit_logs: ['p(95)<500'],
    endpoint_audit_stats: ['p(95)<500'],
    endpoint_list_files: ['p(95)<500'],
    endpoint_list_folders: ['p(95)<500'],
    endpoint_search_marketplace: ['p(95)<2000'],
    endpoint_list_jobs: ['p(95)<500'],
    endpoint_list_recurring: ['p(95)<500'],
    endpoint_list_flags: ['p(95)<500'],
    endpoint_check_flag: ['p(95)<300'],
    endpoint_list_tenants: ['p(95)<500'],
    endpoint_create_tenant: ['p(95)<500'],
    endpoint_get_tenant: ['p(95)<500'],
    endpoint_get_tenant_features: ['p(95)<500'],
    endpoint_delete_tenant: ['p(95)<500'],
  },
};

export function handleSummary(data) {
  // Extract endpoint metrics and sort by p95 (slowest first)
  const endpointMetrics = [];
  for (const [key, metric] of Object.entries(data.metrics)) {
    if (key.startsWith('endpoint_')) {
      endpointMetrics.push({
        name: key.replace('endpoint_', '').replace(/_/g, ' '),
        avg: metric.values.avg,
        med: metric.values.med,
        p90: metric.values['p(90)'],
        p95: metric.values['p(95)'],
        p99: metric.values['p(99)'],
        max: metric.values.max,
        count: metric.values.count,
      });
    }
  }

  endpointMetrics.sort((a, b) => b.p95 - a.p95);

  let hotspotReport = '\n====== HOTSPOT REPORT ======\n\n';
  hotspotReport += 'Endpoints sorted by p95 latency (slowest first):\n\n';
  hotspotReport += `${'Endpoint'.padEnd(30)} ${'Avg'.padStart(8)} ${'Med'.padStart(8)} ${'p90'.padStart(8)} ${'p95'.padStart(8)} ${'p99'.padStart(8)} ${'Max'.padStart(8)} ${'Count'.padStart(7)}\n`;
  hotspotReport += '-'.repeat(105) + '\n';

  for (const m of endpointMetrics) {
    const flag = m.p95 > 500 ? ' *** HOTSPOT' : m.p95 > 200 ? ' * SLOW' : '';
    hotspotReport += `${m.name.padEnd(30)} ${fmt(m.avg).padStart(8)} ${fmt(m.med).padStart(8)} ${fmt(m.p90).padStart(8)} ${fmt(m.p95).padStart(8)} ${fmt(m.p99).padStart(8)} ${fmt(m.max).padStart(8)} ${String(m.count).padStart(7)}${flag}\n`;
  }

  hotspotReport += '\n';
  hotspotReport += 'Legend: *** HOTSPOT = p95 > 500ms | * SLOW = p95 > 200ms\n';
  hotspotReport += '============================\n';

  return {
    stdout: textSummary(data, { indent: ' ', enableColors: true }) + hotspotReport,
    'tests/k6/results/hotspot-report.txt': hotspotReport,
  };
}

function fmt(ms) {
  if (ms >= 1000) return `${(ms / 1000).toFixed(2)}s`;
  return `${ms.toFixed(1)}ms`;
}

export function setup() {
  const auth = authenticate();
  const headers = authHeaders(auth.accessToken);

  // Get user ID and create a product for orders
  const userRes = http.get(`${config.baseUrl}/api/users/me`, { headers });
  const userId = JSON.parse(userRes.body).id;

  const productRes = http.post(
    `${config.baseUrl}/api/products`,
    JSON.stringify({ name: `k6-hotspot-product-${Date.now()}`, price: 9.99 }),
    { headers },
  );
  const productId = JSON.parse(productRes.body).id;

  return { accessToken: auth.accessToken, userId, productId };
}

export default function (data) {
  const headers = authHeaders(data.accessToken);
  let res;

  // --- Health ---
  res = http.get(`${config.baseUrl}/health`);
  endpoints.health.add(res.timings.duration);

  res = http.get(`${config.baseUrl}/alive`);
  endpoints.alive.add(res.timings.duration);

  // --- Auth ---
  res = http.get(`${config.baseUrl}/api/users/me`, { headers });
  endpoints.currentUser.add(res.timings.duration);

  // --- Products CRUD ---
  res = http.get(`${config.baseUrl}/api/products`, { headers });
  endpoints.listProducts.add(res.timings.duration);

  const product = { name: `k6-hs-${randomString()}`, price: randomInt(1, 100) };
  res = http.post(`${config.baseUrl}/api/products`, JSON.stringify(product), { headers });
  endpoints.createProduct.add(res.timings.duration);

  if (res.status === 201) {
    const pid = JSON.parse(res.body).id;

    res = http.get(`${config.baseUrl}/api/products/${pid}`, { headers });
    endpoints.getProduct.add(res.timings.duration);

    res = http.put(
      `${config.baseUrl}/api/products/${pid}`,
      JSON.stringify({ ...product, name: `k6-upd-${randomString()}` }),
      { headers },
    );
    endpoints.updateProduct.add(res.timings.duration);

    res = http.del(`${config.baseUrl}/api/products/${pid}`, null, { headers });
    endpoints.deleteProduct.add(res.timings.duration);
  }

  // --- Orders CRUD ---
  res = http.get(`${config.baseUrl}/api/orders`, { headers });
  endpoints.listOrders.add(res.timings.duration);

  res = http.post(
    `${config.baseUrl}/api/orders`,
    JSON.stringify({
      userId: data.userId,
      items: [{ productId: data.productId, quantity: randomInt(1, 5) }],
    }),
    { headers },
  );
  endpoints.createOrder.add(res.timings.duration);

  if (res.status === 201) {
    const oid = JSON.parse(res.body).id;

    res = http.get(`${config.baseUrl}/api/orders/${oid}`, { headers });
    endpoints.getOrder.add(res.timings.duration);

    res = http.del(`${config.baseUrl}/api/orders/${oid}`, null, { headers });
    endpoints.deleteOrder.add(res.timings.duration);
  }

  // --- PageBuilder ---
  res = http.get(`${config.baseUrl}/api/pagebuilder`, { headers });
  endpoints.listPages.add(res.timings.duration);

  const slug = `k6-hs-${randomString(10)}`;
  res = http.post(
    `${config.baseUrl}/api/pagebuilder`,
    JSON.stringify({ title: `K6 ${randomString()}`, slug }),
    { headers },
  );
  endpoints.createPage.add(res.timings.duration);

  if (res.status === 201) {
    const pgid = JSON.parse(res.body).id;

    res = http.get(`${config.baseUrl}/api/pagebuilder/${pgid}`, { headers });
    endpoints.getPage.add(res.timings.duration);

    res = http.put(
      `${config.baseUrl}/api/pagebuilder/${pgid}/content`,
      JSON.stringify({ content: `<p>${randomString(50)}</p>` }),
      { headers },
    );
    endpoints.updatePageContent.add(res.timings.duration);

    res = http.post(`${config.baseUrl}/api/pagebuilder/${pgid}/publish`, null, { headers });
    endpoints.publishPage.add(res.timings.duration);

    res = http.post(`${config.baseUrl}/api/pagebuilder/${pgid}/unpublish`, null, { headers });
    endpoints.unpublishPage.add(res.timings.duration);

    res = http.del(`${config.baseUrl}/api/pagebuilder/${pgid}`, null, { headers });
    endpoints.deletePage.add(res.timings.duration);
  }

  res = http.get(`${config.baseUrl}/api/pagebuilder/tags`, { headers });
  endpoints.listTags.add(res.timings.duration);

  res = http.get(`${config.baseUrl}/api/pagebuilder/templates`, { headers });
  endpoints.listTemplates.add(res.timings.duration);

  // --- Settings ---
  res = http.get(`${config.baseUrl}/api/settings`, { headers });
  endpoints.listSettings.add(res.timings.duration);

  res = http.get(`${config.baseUrl}/api/settings/definitions`, { headers });
  endpoints.getDefinitions.add(res.timings.duration);

  res = http.get(`${config.baseUrl}/api/settings/menus`, { headers });
  endpoints.listMenus.add(res.timings.duration);

  res = http.get(`${config.baseUrl}/api/settings/me`, { headers });
  endpoints.getUserSettings.add(res.timings.duration);

  // --- Users ---
  res = http.get(`${config.baseUrl}/api/users`, { headers });
  endpoints.listUsers.add(res.timings.duration);

  // --- Audit Logs ---
  res = http.get(`${config.baseUrl}/api/audit-logs`, { headers });
  endpoints.queryAuditLogs.add(res.timings.duration);

  const now = new Date().toISOString();
  const yesterday = new Date(Date.now() - 86400000).toISOString();
  res = http.get(`${config.baseUrl}/api/audit-logs/stats?from=${yesterday}&to=${now}`, { headers });
  endpoints.auditStats.add(res.timings.duration);

  // --- FileStorage ---
  res = http.get(`${config.baseUrl}/api/files`, { headers });
  endpoints.listFiles.add(res.timings.duration);

  res = http.get(`${config.baseUrl}/api/files/folders`, { headers });
  endpoints.listFolders.add(res.timings.duration);

  // --- Marketplace (anonymous) ---
  res = http.get(`${config.baseUrl}/api/marketplace?take=5`);
  endpoints.searchMarketplace.add(res.timings.duration);

  // --- BackgroundJobs ---
  res = http.get(`${config.baseUrl}/api/jobs`, { headers });
  endpoints.listJobs.add(res.timings.duration);

  res = http.get(`${config.baseUrl}/api/jobs/recurring`, { headers });
  endpoints.listRecurring.add(res.timings.duration);

  // --- FeatureFlags ---
  res = http.get(`${config.baseUrl}/api/feature-flags`, { headers });
  endpoints.listFlags.add(res.timings.duration);

  // Check a flag if any exist
  if (res.status === 200) {
    try {
      const flags = JSON.parse(res.body);
      const items = Array.isArray(flags) ? flags : flags.items || [];
      if (items.length > 0) {
        res = http.get(`${config.baseUrl}/api/feature-flags/check/${items[0].name}`, { headers });
        endpoints.checkFlag.add(res.timings.duration);
      }
    } catch (_) {
      // ignore
    }
  }

  // --- Tenants CRUD ---
  res = http.get(`${config.baseUrl}/api/tenants`, { headers });
  endpoints.listTenants.add(res.timings.duration);

  const tenantSuffix = randomString(8);
  res = http.post(
    `${config.baseUrl}/api/tenants`,
    JSON.stringify({
      name: `k6-hs-${tenantSuffix}`,
      identifier: `k6hs${tenantSuffix}`,
    }),
    { headers },
  );
  endpoints.createTenant.add(res.timings.duration);

  if (res.status === 201) {
    const tid = JSON.parse(res.body).id;

    res = http.get(`${config.baseUrl}/api/tenants/${tid}`, { headers });
    endpoints.getTenant.add(res.timings.duration);

    res = http.get(`${config.baseUrl}/api/tenants/${tid}/features`, { headers });
    endpoints.getTenantFeatures.add(res.timings.duration);

    res = http.del(`${config.baseUrl}/api/tenants/${tid}`, null, { headers });
    endpoints.deleteTenant.add(res.timings.duration);
  }

  sleep(0.5);
}

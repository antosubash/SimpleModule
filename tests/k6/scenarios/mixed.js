import http from 'k6/http';
import { sleep } from 'k6';
import { config, defaultThresholds, loadProfiles, tlsOptions } from '../lib/config.js';
import { authenticate, authHeaders } from '../lib/auth.js';
import { checkResponse, randomString, randomInt, jitterSleep } from '../lib/helpers.js';

const profile = __ENV.K6_PROFILE || 'load';

export const options = {
  ...tlsOptions,
  stages: loadProfiles[profile]?.stages || loadProfiles.load.stages,
  thresholds: {
    ...defaultThresholds,
    api_duration: ['p(95)<800', 'p(99)<2000'],
    api_errors: ['rate<0.05'],
  },
};

export function setup() {
  const auth = authenticate();
  const headers = authHeaders(auth.accessToken);

  // Get current user ID for order creation
  const userRes = http.get(`${config.baseUrl}/api/users/me`, { headers });
  const userId = JSON.parse(userRes.body).id;

  // Create a product to use in orders
  const productRes = http.post(
    `${config.baseUrl}/api/products`,
    JSON.stringify({ name: `k6-mixed-product-${Date.now()}`, price: 9.99 }),
    { headers },
  );
  const productId = JSON.parse(productRes.body).id;

  return { accessToken: auth.accessToken, userId, productId };
}

// Simulates realistic mixed traffic across all API modules
export default function (auth) {
  const headers = authHeaders(auth.accessToken);

  // Weighted random selection of actions (read-heavy, matching real-world usage)
  const action = weightedRandom([
    { weight: 25, fn: () => browseProducts(headers) },
    { weight: 15, fn: () => browseOrders(headers) },
    { weight: 15, fn: () => browsePages(headers) },
    { weight: 10, fn: () => browseMarketplace() },
    { weight: 10, fn: () => crudProduct(headers) },
    { weight: 10, fn: () => crudOrder(headers, auth.userId, auth.productId) },
    { weight: 5, fn: () => browseAuditLogs(headers) },
    { weight: 5, fn: () => browseFiles(headers) },
    { weight: 5, fn: () => getCurrentUser(headers) },
  ]);

  action();
  sleep(jitterSleep(0.5, 1.5));
}

function weightedRandom(items) {
  const total = items.reduce((sum, item) => sum + item.weight, 0);
  let random = Math.random() * total;
  for (const item of items) {
    random -= item.weight;
    if (random <= 0) return item.fn;
  }
  return items[0].fn;
}

// --- Action functions ---

function browseProducts(headers) {
  const res = http.get(`${config.baseUrl}/api/products`, {
    headers,
    tags: { name: 'browse-products' },
  });
  checkResponse(res, 'browse-products');
}

function browseOrders(headers) {
  const res = http.get(`${config.baseUrl}/api/orders`, {
    headers,
    tags: { name: 'browse-orders' },
  });
  checkResponse(res, 'browse-orders');
}

function browsePages(headers) {
  const res = http.get(`${config.baseUrl}/api/pagebuilder`, {
    headers,
    tags: { name: 'browse-pages' },
  });
  checkResponse(res, 'browse-pages');
}

function browseMarketplace() {
  // Marketplace endpoints are anonymous (no auth required)
  const res = http.get(`${config.baseUrl}/api/marketplace?take=5`, {
    tags: { name: 'browse-marketplace' },
  });
  checkResponse(res, 'browse-marketplace');
}

function browseAuditLogs(headers) {
  const res = http.get(`${config.baseUrl}/api/audit-logs`, {
    headers,
    tags: { name: 'browse-audit-logs' },
  });
  checkResponse(res, 'browse-audit-logs');
}

function browseFiles(headers) {
  const res = http.get(`${config.baseUrl}/api/files`, {
    headers,
    tags: { name: 'browse-files' },
  });
  checkResponse(res, 'browse-files');
}

function getCurrentUser(headers) {
  const res = http.get(`${config.baseUrl}/api/users/me`, {
    headers,
    tags: { name: 'get-current-user' },
  });
  checkResponse(res, 'get-current-user');
}

function crudProduct(headers) {
  const baseUrl = `${config.baseUrl}/api/products`;

  const createRes = http.post(
    baseUrl,
    JSON.stringify({
      name: `k6-mixed-${randomString()}`,
      description: 'Mixed load test product',
      price: randomInt(100, 10000) / 100,
    }),
    { headers, tags: { name: 'crud-create-product' } },
  );
  checkResponse(createRes, 'crud-create-product', 201);

  if (createRes.status === 201) {
    const id = JSON.parse(createRes.body).id;

    http.get(`${baseUrl}/${id}`, {
      headers,
      tags: { name: 'crud-get-product' },
    });

    http.del(`${baseUrl}/${id}`, null, {
      headers,
      tags: { name: 'crud-delete-product' },
    });
  }
}

function crudOrder(headers, userId, productId) {
  const baseUrl = `${config.baseUrl}/api/orders`;

  const createRes = http.post(
    baseUrl,
    JSON.stringify({
      userId: userId,
      items: [
        {
          productId: productId,
          quantity: randomInt(1, 5),
        },
      ],
    }),
    { headers, tags: { name: 'crud-create-order' } },
  );
  checkResponse(createRes, 'crud-create-order', 201);

  if (createRes.status === 201) {
    const id = JSON.parse(createRes.body).id;

    http.get(`${baseUrl}/${id}`, {
      headers,
      tags: { name: 'crud-get-order' },
    });

    http.del(`${baseUrl}/${id}`, null, {
      headers,
      tags: { name: 'crud-delete-order' },
    });
  }
}

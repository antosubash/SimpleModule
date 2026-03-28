import http from 'k6/http';
import { sleep } from 'k6';
import { config, defaultThresholds, loadProfiles } from '../lib/config.js';
import { authenticate, authHeaders } from '../lib/auth.js';
import { checkResponse, randomString, randomInt } from '../lib/helpers.js';

const profile = __ENV.K6_PROFILE || 'smoke';

export const options = {
  stages: loadProfiles[profile]?.stages || loadProfiles.smoke.stages,
  thresholds: {
    ...defaultThresholds,
    'http_req_duration{name:list-orders}': ['p(95)<500'],
    'http_req_duration{name:create-order}': ['p(95)<800'],
    'http_req_duration{name:get-order}': ['p(95)<500'],
    'http_req_duration{name:delete-order}': ['p(95)<500'],
  },
};

export function setup() {
  return authenticate();
}

export default function (auth) {
  const headers = authHeaders(auth.accessToken);
  const baseUrl = `${config.baseUrl}/api/orders`;

  // List orders
  const listRes = http.get(baseUrl, {
    headers,
    tags: { name: 'list-orders' },
  });
  checkResponse(listRes, 'list-orders');

  // Create an order
  const order = {
    customerName: `k6-customer-${randomString()}`,
    items: [
      {
        productName: `k6-item-${randomString()}`,
        quantity: randomInt(1, 10),
        unitPrice: randomInt(100, 5000) / 100,
      },
    ],
  };
  const createRes = http.post(baseUrl, JSON.stringify(order), {
    headers,
    tags: { name: 'create-order' },
  });
  checkResponse(createRes, 'create-order', 201);

  if (createRes.status === 201) {
    const created = JSON.parse(createRes.body);
    const orderId = created.id;

    // Get single order
    const getRes = http.get(`${baseUrl}/${orderId}`, {
      headers,
      tags: { name: 'get-order' },
    });
    checkResponse(getRes, 'get-order');

    // Delete order (cleanup)
    const deleteRes = http.del(`${baseUrl}/${orderId}`, null, {
      headers,
      tags: { name: 'delete-order' },
    });
    checkResponse(deleteRes, 'delete-order');
  }

  sleep(1);
}

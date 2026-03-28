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
    'http_req_duration{name:list-products}': ['p(95)<500'],
    'http_req_duration{name:create-product}': ['p(95)<800'],
    'http_req_duration{name:get-product}': ['p(95)<500'],
    'http_req_duration{name:update-product}': ['p(95)<800'],
    'http_req_duration{name:delete-product}': ['p(95)<500'],
  },
};

export function setup() {
  return authenticate();
}

export default function (auth) {
  const headers = authHeaders(auth.accessToken);
  const baseUrl = `${config.baseUrl}/api/products`;

  // List products
  const listRes = http.get(baseUrl, {
    headers,
    tags: { name: 'list-products' },
  });
  checkResponse(listRes, 'list-products');

  // Create a product
  const product = {
    name: `k6-product-${randomString()}`,
    description: `Load test product created by k6`,
    price: randomInt(100, 10000) / 100,
  };
  const createRes = http.post(baseUrl, JSON.stringify(product), {
    headers,
    tags: { name: 'create-product' },
  });
  checkResponse(createRes, 'create-product', 201);

  if (createRes.status === 201) {
    const created = JSON.parse(createRes.body);
    const productId = created.id;

    // Get single product
    const getRes = http.get(`${baseUrl}/${productId}`, {
      headers,
      tags: { name: 'get-product' },
    });
    checkResponse(getRes, 'get-product');

    // Update product
    const updateRes = http.put(
      `${baseUrl}/${productId}`,
      JSON.stringify({
        ...product,
        name: `k6-updated-${randomString()}`,
        price: randomInt(100, 10000) / 100,
      }),
      { headers, tags: { name: 'update-product' } },
    );
    checkResponse(updateRes, 'update-product');

    // Delete product (cleanup)
    const deleteRes = http.del(`${baseUrl}/${productId}`, null, {
      headers,
      tags: { name: 'delete-product' },
    });
    checkResponse(deleteRes, 'delete-product');
  }

  sleep(1);
}

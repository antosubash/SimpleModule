import { sleep } from 'k6';
import http from 'k6/http';
import { authenticate, authHeaders } from '../lib/auth.ts';
import { config, defaultThresholds, loadProfiles, tlsOptions } from '../lib/config.ts';
import { checkResponse, randomInt, randomString } from '../lib/helpers.ts';

const profile = __ENV.K6_PROFILE || 'smoke';

export const options = {
  ...tlsOptions,
  stages: loadProfiles[profile]?.stages || loadProfiles.smoke.stages,
  thresholds: {
    ...defaultThresholds,
    'http_req_duration{name:list-orders}': ['p(95)<500'],
    'http_req_duration{name:create-order}': ['p(95)<800'],
    'http_req_duration{name:get-order}': ['p(95)<500'],
    'http_req_duration{name:delete-order}': ['p(95)<500'],
  },
};

interface SetupData {
  accessToken: string;
  userId: string;
  productId: number;
}

export function setup(): SetupData {
  const auth = authenticate();
  const headers = authHeaders(auth.accessToken);

  const userRes = http.get(`${config.baseUrl}/api/users/me`, { headers });
  const userId = JSON.parse(userRes.body as string).id;

  const productRes = http.post(
    `${config.baseUrl}/api/products`,
    JSON.stringify({ name: `k6-order-product-${randomString()}`, price: 9.99 }),
    { headers },
  );
  const productId = JSON.parse(productRes.body as string).id;

  return { accessToken: auth.accessToken, userId, productId };
}

export default function (data: SetupData) {
  const headers = authHeaders(data.accessToken);
  const baseUrl = `${config.baseUrl}/api/orders`;

  const listRes = http.get(baseUrl, {
    headers,
    tags: { name: 'list-orders' },
  });
  checkResponse(listRes, 'list-orders');

  const order = {
    userId: data.userId,
    items: [{ productId: data.productId, quantity: randomInt(1, 10) }],
  };
  const createRes = http.post(baseUrl, JSON.stringify(order), {
    headers,
    tags: { name: 'create-order' },
  });
  checkResponse(createRes, 'create-order', 201);

  if (createRes.status === 201) {
    const created = JSON.parse(createRes.body as string);
    const orderId = created.id;

    const getRes = http.get(`${baseUrl}/${orderId}`, {
      headers,
      tags: { name: 'get-order' },
    });
    checkResponse(getRes, 'get-order');

    const deleteRes = http.del(`${baseUrl}/${orderId}`, null, {
      headers,
      tags: { name: 'delete-order' },
    });
    checkResponse(deleteRes, 'delete-order', 204);
  }

  sleep(1);
}

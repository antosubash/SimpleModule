import http from 'k6/http';
import { config } from '../../lib/config.ts';
import { randomInt, randomString } from '../../lib/helpers.ts';
import { endpoints } from './metrics.ts';

type Headers = Record<string, string>;

export interface HotspotSetupData {
  accessToken: string;
  userId: string;
  productId: number;
}

export function runHealthChecks(headers: Headers) {
  let res = http.get(`${config.baseUrl}/health`);
  endpoints.health.add(res.timings.duration);

  res = http.get(`${config.baseUrl}/alive`);
  endpoints.alive.add(res.timings.duration);

  res = http.get(`${config.baseUrl}/api/users/me`, { headers });
  endpoints.currentUser.add(res.timings.duration);
}

export function runProductRequests(headers: Headers) {
  let res = http.get(`${config.baseUrl}/api/products`, { headers });
  endpoints.listProducts.add(res.timings.duration);

  const product = { name: `k6-hs-${randomString()}`, price: randomInt(1, 100) };
  res = http.post(`${config.baseUrl}/api/products`, JSON.stringify(product), { headers });
  endpoints.createProduct.add(res.timings.duration);

  if (res.status === 201) {
    const pid = JSON.parse(res.body as string).id;

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
}

export function runOrderRequests(headers: Headers, data: HotspotSetupData) {
  let res = http.get(`${config.baseUrl}/api/orders`, { headers });
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
    const oid = JSON.parse(res.body as string).id;

    res = http.get(`${config.baseUrl}/api/orders/${oid}`, { headers });
    endpoints.getOrder.add(res.timings.duration);

    res = http.del(`${config.baseUrl}/api/orders/${oid}`, null, { headers });
    endpoints.deleteOrder.add(res.timings.duration);
  }
}

export function runPageBuilderRequests(headers: Headers) {
  let res = http.get(`${config.baseUrl}/api/pagebuilder`, { headers });
  endpoints.listPages.add(res.timings.duration);

  const slug = `k6-hs-${randomString(10)}`;
  res = http.post(
    `${config.baseUrl}/api/pagebuilder`,
    JSON.stringify({ title: `K6 ${randomString()}`, slug }),
    { headers },
  );
  endpoints.createPage.add(res.timings.duration);

  if (res.status === 201) {
    const pgid = JSON.parse(res.body as string).id;

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
}

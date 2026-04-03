import { sleep } from 'k6';
import http from 'k6/http';
import { type AuthResult, authenticate, authHeaders } from '../lib/auth.ts';
import { config, defaultThresholds, loadProfiles, tlsOptions } from '../lib/config.ts';
import { checkResponse, randomString } from '../lib/helpers.ts';

const profile = __ENV.K6_PROFILE || 'smoke';

export const options = {
  ...tlsOptions,
  stages: loadProfiles[profile]?.stages || loadProfiles.smoke.stages,
  thresholds: {
    ...defaultThresholds,
    'http_req_duration{name:list-settings}': ['p(95)<500'],
    'http_req_duration{name:get-definitions}': ['p(95)<500'],
    'http_req_duration{name:update-setting}': ['p(95)<500'],
    'http_req_duration{name:get-setting}': ['p(95)<500'],
    'http_req_duration{name:delete-setting}': ['p(95)<500'],
    'http_req_duration{name:get-user-settings}': ['p(95)<500'],
    'http_req_duration{name:list-menus}': ['p(95)<500'],
    'http_req_duration{name:create-menu}': ['p(95)<500'],
    'http_req_duration{name:delete-menu}': ['p(95)<500'],
    'http_req_duration{name:available-pages}': ['p(95)<500'],
  },
};

export function setup(): AuthResult {
  return authenticate();
}

export default function (auth: AuthResult) {
  const headers = authHeaders(auth.accessToken);
  const settingsUrl = `${config.baseUrl}/api/settings`;

  const listRes = http.get(settingsUrl, { headers, tags: { name: 'list-settings' } });
  checkResponse(listRes, 'list-settings');

  const defsRes = http.get(`${settingsUrl}/definitions`, {
    headers,
    tags: { name: 'get-definitions' },
  });
  checkResponse(defsRes, 'get-definitions');

  const settingKey = `k6.test.${randomString(6)}`;
  const updateRes = http.put(
    settingsUrl,
    JSON.stringify({ key: settingKey, value: 'k6-test-value', scope: 0 }),
    { headers, tags: { name: 'update-setting' } },
  );
  checkResponse(updateRes, 'update-setting', 204);

  const getRes = http.get(`${settingsUrl}/${settingKey}?scope=0`, {
    headers,
    tags: { name: 'get-setting' },
  });
  checkResponse(getRes, 'get-setting');

  const delRes = http.del(`${settingsUrl}/${settingKey}?scope=0`, null, {
    headers,
    tags: { name: 'delete-setting' },
  });
  checkResponse(delRes, 'delete-setting', 204);

  const userSettingsRes = http.get(`${settingsUrl}/me`, {
    headers,
    tags: { name: 'get-user-settings' },
  });
  checkResponse(userSettingsRes, 'get-user-settings', userSettingsRes.status === 401 ? 401 : 200);

  const menusUrl = `${settingsUrl}/menus`;

  const menusRes = http.get(menusUrl, { headers, tags: { name: 'list-menus' } });
  checkResponse(menusRes, 'list-menus');

  const pagesRes = http.get(`${menusUrl}/available-pages`, {
    headers,
    tags: { name: 'available-pages' },
  });
  checkResponse(pagesRes, 'available-pages');

  const createMenuRes = http.post(
    menusUrl,
    JSON.stringify({
      label: `k6-menu-${randomString(6)}`,
      url: '/k6-test',
      icon: '<svg></svg>',
      openInNewTab: false,
      isVisible: true,
      isHomePage: false,
    }),
    { headers, tags: { name: 'create-menu' } },
  );
  checkResponse(createMenuRes, 'create-menu', 201);

  if (createMenuRes.status === 201) {
    const menuId = JSON.parse(createMenuRes.body as string).id;
    const delMenuRes = http.del(`${menusUrl}/${menuId}`, null, {
      headers,
      tags: { name: 'delete-menu' },
    });
    checkResponse(delMenuRes, 'delete-menu', 204);
  }

  sleep(1);
}

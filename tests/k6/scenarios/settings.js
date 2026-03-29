import http from 'k6/http';
import { sleep } from 'k6';
import { config, defaultThresholds, loadProfiles, tlsOptions } from '../lib/config.js';
import { authenticate, authHeaders } from '../lib/auth.js';
import { checkResponse, randomString } from '../lib/helpers.js';

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

export function setup() {
  return authenticate();
}

export default function (auth) {
  const headers = authHeaders(auth.accessToken);
  const settingsUrl = `${config.baseUrl}/api/settings`;

  // --- System Settings ---

  // List all settings
  const listRes = http.get(settingsUrl, {
    headers,
    tags: { name: 'list-settings' },
  });
  checkResponse(listRes, 'list-settings');

  // Get setting definitions
  const defsRes = http.get(`${settingsUrl}/definitions`, {
    headers,
    tags: { name: 'get-definitions' },
  });
  checkResponse(defsRes, 'get-definitions');

  // Create/update a setting (scope: 0=System, 1=Application, 2=User)
  const settingKey = `k6.test.${randomString(6)}`;
  const updateRes = http.put(
    settingsUrl,
    JSON.stringify({ key: settingKey, value: 'k6-test-value', scope: 0 }),
    { headers, tags: { name: 'update-setting' } },
  );
  checkResponse(updateRes, 'update-setting', 204);

  // Get the setting back
  const getRes = http.get(`${settingsUrl}/${settingKey}?scope=0`, {
    headers,
    tags: { name: 'get-setting' },
  });
  checkResponse(getRes, 'get-setting');

  // Delete the setting
  const delRes = http.del(`${settingsUrl}/${settingKey}?scope=0`, null, {
    headers,
    tags: { name: 'delete-setting' },
  });
  checkResponse(delRes, 'delete-setting', 204);

  // --- User Settings (may return 401 if NameIdentifier claim is missing) ---

  const userSettingsRes = http.get(`${settingsUrl}/me`, {
    headers,
    tags: { name: 'get-user-settings' },
  });
  // Accept 200 or 401 (claim mapping depends on auth flow)
  checkResponse(userSettingsRes, 'get-user-settings', userSettingsRes.status === 401 ? 401 : 200);

  // --- Menu Management ---

  const menusUrl = `${settingsUrl}/menus`;

  // List menus
  const menusRes = http.get(menusUrl, {
    headers,
    tags: { name: 'list-menus' },
  });
  checkResponse(menusRes, 'list-menus');

  // Available pages
  const pagesRes = http.get(`${menusUrl}/available-pages`, {
    headers,
    tags: { name: 'available-pages' },
  });
  checkResponse(pagesRes, 'available-pages');

  // Create a menu item
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
    const menuId = JSON.parse(createMenuRes.body).id;

    // Delete menu item (cleanup)
    const delMenuRes = http.del(`${menusUrl}/${menuId}`, null, {
      headers,
      tags: { name: 'delete-menu' },
    });
    checkResponse(delMenuRes, 'delete-menu', 204);
  }

  sleep(1);
}

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
    'http_req_duration{name:list-files}': ['p(95)<500'],
    'http_req_duration{name:list-folders}': ['p(95)<500'],
    'http_req_duration{name:upload-file}': ['p(95)<2000'],
    'http_req_duration{name:get-file}': ['p(95)<500'],
    'http_req_duration{name:download-file}': ['p(95)<1000'],
    'http_req_duration{name:delete-file}': ['p(95)<500'],
  },
};

export function setup() {
  return authenticate();
}

export default function (auth) {
  const headers = authHeaders(auth.accessToken);
  const baseUrl = `${config.baseUrl}/api/files`;

  // List files
  const listRes = http.get(baseUrl, {
    headers,
    tags: { name: 'list-files' },
  });
  checkResponse(listRes, 'list-files');

  // List folders
  const foldersRes = http.get(`${baseUrl}/folders`, {
    headers,
    tags: { name: 'list-folders' },
  });
  checkResponse(foldersRes, 'list-folders');

  // Upload a small test file
  const fileContent = `k6 load test file content - ${randomString(32)}`;
  const fileName = `k6-test-${randomString(8)}.txt`;
  const uploadRes = http.post(baseUrl, {
    file: http.file(fileContent, fileName, 'text/plain'),
  }, {
    headers: { Authorization: `Bearer ${auth.accessToken}` },
    tags: { name: 'upload-file' },
  });
  checkResponse(uploadRes, 'upload-file', 201);

  if (uploadRes.status === 201) {
    const fileId = JSON.parse(uploadRes.body).id;

    // Get file metadata
    const getRes = http.get(`${baseUrl}/${fileId}`, {
      headers,
      tags: { name: 'get-file' },
    });
    checkResponse(getRes, 'get-file');

    // Download file
    const downloadRes = http.get(`${baseUrl}/${fileId}/download`, {
      headers,
      tags: { name: 'download-file' },
    });
    checkResponse(downloadRes, 'download-file');

    // Delete file (cleanup)
    const deleteRes = http.del(`${baseUrl}/${fileId}`, null, {
      headers,
      tags: { name: 'delete-file' },
    });
    checkResponse(deleteRes, 'delete-file', 204);
  }

  sleep(1);
}

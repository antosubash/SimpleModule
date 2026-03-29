import http from 'k6/http';
import { sleep } from 'k6';
import { config, defaultThresholds, loadProfiles, tlsOptions } from '../lib/config.js';
import { checkResponse, randomInt } from '../lib/helpers.js';

const profile = __ENV.K6_PROFILE || 'smoke';

// Marketplace endpoints are anonymous (no auth required)
export const options = {
  ...tlsOptions,
  stages: loadProfiles[profile]?.stages || loadProfiles.smoke.stages,
  thresholds: {
    ...defaultThresholds,
    'http_req_duration{name:search-packages}': ['p(95)<2000'],
    'http_req_duration{name:search-with-query}': ['p(95)<2000'],
    'http_req_duration{name:search-with-category}': ['p(95)<2000'],
    'http_req_duration{name:get-package}': ['p(95)<2000'],
  },
};

const searchQueries = [
  'simplemodule',
  'aspnet',
  'blazor',
  'entityframework',
  'authentication',
];

const categories = [
  'All',
  'Auth',
  'Storage',
  'UI',
  'Analytics',
  'Integration',
];

export default function () {
  const baseUrl = `${config.baseUrl}/api/marketplace`;

  // Search packages (no filter)
  const searchRes = http.get(`${baseUrl}?take=5`, {
    tags: { name: 'search-packages' },
  });
  checkResponse(searchRes, 'search-packages');

  // Search with query
  const query = searchQueries[randomInt(0, searchQueries.length)];
  const queryRes = http.get(`${baseUrl}?q=${query}&take=5`, {
    tags: { name: 'search-with-query' },
  });
  checkResponse(queryRes, 'search-with-query');

  // Search with category filter
  const category = categories[randomInt(0, categories.length)];
  const categoryRes = http.get(`${baseUrl}?category=${category}&take=5`, {
    tags: { name: 'search-with-category' },
  });
  checkResponse(categoryRes, 'search-with-category');

  // Get a specific package by ID (if search returned results)
  if (searchRes.status === 200) {
    try {
      const results = JSON.parse(searchRes.body);
      if (results.packages && results.packages.length > 0) {
        const packageId = results.packages[0].id;
        const detailRes = http.get(`${baseUrl}/${packageId}`, {
          tags: { name: 'get-package' },
        });
        checkResponse(detailRes, 'get-package');
      }
    } catch {
      // search may return empty or different format
    }
  }

  sleep(1);
}

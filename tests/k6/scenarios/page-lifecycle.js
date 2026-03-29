import http from 'k6/http';
import { sleep } from 'k6';
import { config, defaultThresholds, loadProfiles, tlsOptions } from '../lib/config.js';
import { authenticate, authHeaders } from '../lib/auth.js';
import { checkResponse, randomString } from '../lib/helpers.js';

const profile = __ENV.K6_PROFILE || 'smoke';

// Tests the full page lifecycle: create -> edit -> tag -> publish -> unpublish -> trash -> restore -> permanent delete
export const options = {
  ...tlsOptions,
  stages: loadProfiles[profile]?.stages || loadProfiles.smoke.stages,
  thresholds: {
    ...defaultThresholds,
    'http_req_duration{name:create-page}': ['p(95)<800'],
    'http_req_duration{name:update-page}': ['p(95)<500'],
    'http_req_duration{name:update-content}': ['p(95)<500'],
    'http_req_duration{name:publish-page}': ['p(95)<500'],
    'http_req_duration{name:unpublish-page}': ['p(95)<500'],
    'http_req_duration{name:delete-page}': ['p(95)<500'],
    'http_req_duration{name:list-trash}': ['p(95)<500'],
    'http_req_duration{name:restore-page}': ['p(95)<500'],
    'http_req_duration{name:permanent-delete}': ['p(95)<500'],
    'http_req_duration{name:list-tags}': ['p(95)<500'],
    'http_req_duration{name:add-tag}': ['p(95)<500'],
    'http_req_duration{name:remove-tag}': ['p(95)<500'],
    'http_req_duration{name:list-templates}': ['p(95)<500'],
    'http_req_duration{name:create-template}': ['p(95)<800'],
    'http_req_duration{name:delete-template}': ['p(95)<500'],
  },
};

export function setup() {
  return authenticate();
}

export default function (auth) {
  const headers = authHeaders(auth.accessToken);
  const baseUrl = `${config.baseUrl}/api/pagebuilder`;

  // --- Full Page Lifecycle ---

  // Create page
  const slug = `k6-lifecycle-${randomString(8)}`;
  const createRes = http.post(
    baseUrl,
    JSON.stringify({ title: `K6 Lifecycle ${randomString(6)}`, slug }),
    { headers, tags: { name: 'create-page' } },
  );
  checkResponse(createRes, 'create-page', 201);

  if (createRes.status === 201) {
    const page = JSON.parse(createRes.body);
    const pageId = page.id;

    // Update page metadata
    const updateRes = http.put(
      `${baseUrl}/${pageId}`,
      JSON.stringify({
        title: `K6 Updated ${randomString(6)}`,
        slug,
        order: 0,
        isPublished: false,
        metaDescription: 'k6 load test page',
      }),
      { headers, tags: { name: 'update-page' } },
    );
    checkResponse(updateRes, 'update-page', 204);

    // Update content
    const contentRes = http.put(
      `${baseUrl}/${pageId}/content`,
      JSON.stringify({ content: `<h1>Load Test</h1><p>${randomString(100)}</p>` }),
      { headers, tags: { name: 'update-content' } },
    );
    checkResponse(contentRes, 'update-content');

    // Publish
    const publishRes = http.post(`${baseUrl}/${pageId}/publish`, null, {
      headers,
      tags: { name: 'publish-page' },
    });
    checkResponse(publishRes, 'publish-page');

    // Unpublish
    const unpublishRes = http.post(`${baseUrl}/${pageId}/unpublish`, null, {
      headers,
      tags: { name: 'unpublish-page' },
    });
    checkResponse(unpublishRes, 'unpublish-page');

    // Soft delete (trash)
    const deleteRes = http.del(`${baseUrl}/${pageId}`, null, {
      headers,
      tags: { name: 'delete-page' },
    });
    checkResponse(deleteRes, 'delete-page', 204);

    // List trash
    const trashRes = http.get(`${baseUrl}/trash`, {
      headers,
      tags: { name: 'list-trash' },
    });
    checkResponse(trashRes, 'list-trash');

    // Restore from trash
    const restoreRes = http.post(`${baseUrl}/${pageId}/restore`, null, {
      headers,
      tags: { name: 'restore-page' },
    });
    checkResponse(restoreRes, 'restore-page');

    // Permanent delete
    const permDeleteRes = http.del(`${baseUrl}/${pageId}/permanent`, null, {
      headers,
      tags: { name: 'permanent-delete' },
    });
    checkResponse(permDeleteRes, 'permanent-delete', 204);
  }

  // --- Tags ---

  const tagsRes = http.get(`${baseUrl}/tags`, {
    headers,
    tags: { name: 'list-tags' },
  });
  checkResponse(tagsRes, 'list-tags');

  // Create another page for tag testing
  const tagSlug = `k6-tag-${randomString(8)}`;
  const tagPageRes = http.post(
    baseUrl,
    JSON.stringify({ title: `K6 Tag Page ${randomString(4)}`, slug: tagSlug }),
    { headers },
  );

  if (tagPageRes.status === 201) {
    const tagPageId = JSON.parse(tagPageRes.body).id;

    // Add tag
    const addTagRes = http.post(
      `${baseUrl}/${tagPageId}/tags`,
      JSON.stringify({ name: `k6-tag-${randomString(4)}` }),
      { headers, tags: { name: 'add-tag' } },
    );
    checkResponse(addTagRes, 'add-tag', 204);

    // Get updated tags to find tagId
    const updatedTags = http.get(`${baseUrl}/tags`, { headers });
    if (updatedTags.status === 200) {
      try {
        const allTags = JSON.parse(updatedTags.body);
        const ourTag = allTags.find((t) => t.name && t.name.startsWith('k6-tag-'));
        if (ourTag) {
          // Remove tag
          const removeTagRes = http.del(`${baseUrl}/${tagPageId}/tags/${ourTag.id}`, null, {
            headers,
            tags: { name: 'remove-tag' },
          });
          checkResponse(removeTagRes, 'remove-tag', 204);
        }
      } catch {
        // tag operations are best-effort in load tests
      }
    }

    // Cleanup
    http.del(`${baseUrl}/${tagPageId}/permanent`, null, { headers });
  }

  // --- Templates ---

  const templatesRes = http.get(`${baseUrl}/templates`, {
    headers,
    tags: { name: 'list-templates' },
  });
  checkResponse(templatesRes, 'list-templates');

  // Create template
  const createTemplateRes = http.post(
    `${baseUrl}/templates`,
    JSON.stringify({
      name: `k6-template-${randomString(8)}`,
      content: '{"blocks":[]}',
    }),
    { headers, tags: { name: 'create-template' } },
  );
  checkResponse(createTemplateRes, 'create-template', 201);

  if (createTemplateRes.status === 201) {
    const templateId = JSON.parse(createTemplateRes.body).id;
    const delTemplateRes = http.del(`${baseUrl}/templates/${templateId}`, null, {
      headers,
      tags: { name: 'delete-template' },
    });
    checkResponse(delTemplateRes, 'delete-template', 204);
  }

  sleep(1);
}

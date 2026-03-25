import { faker } from '@faker-js/faker';
import { expect, test } from '../../fixtures/base';

test.describe('Menu Manager - API', () => {
  test('full CRUD lifecycle via API', async ({ request }) => {
    const label = faker.commerce.department();
    const updatedLabel = faker.commerce.department();

    // Create
    const createResponse = await request.post('/api/settings/menus', {
      data: { label, url: `/${faker.helpers.slugify(label).toLowerCase()}`, icon: '', isVisible: true },
    });
    expect(createResponse.status()).toBe(201);
    const created = await createResponse.json();
    expect(created.label).toBe(label);
    expect(created.id).toBeGreaterThan(0);

    // Read
    const getResponse = await request.get('/api/settings/menus');
    expect(getResponse.ok()).toBeTruthy();
    const items = await getResponse.json();
    expect(Array.isArray(items)).toBeTruthy();
    const flatStr = JSON.stringify(items);
    expect(flatStr).toContain(label);

    // Update
    const updateResponse = await request.put(`/api/settings/menus/${created.id}`, {
      data: {
        label: updatedLabel,
        url: `/${faker.helpers.slugify(updatedLabel).toLowerCase()}`,
        icon: '',
        cssClass: '',
        openInNewTab: false,
        isVisible: true,
        isHomePage: false,
      },
    });
    expect(updateResponse.status()).toBe(204);

    // Delete
    const deleteResponse = await request.delete(`/api/settings/menus/${created.id}`);
    expect(deleteResponse.status()).toBe(204);

    // Verify deleted
    const getAfterDelete = await request.get('/api/settings/menus');
    const afterDelete = await getAfterDelete.json();
    const afterStr = JSON.stringify(afterDelete);
    expect(afterStr).not.toContain(updatedLabel);
  });

  test('create nested items via API', async ({ request }) => {
    const parentLabel = faker.commerce.department();
    const childLabel = faker.commerce.productAdjective();
    const grandchildLabel = faker.animal.dog();

    // Create parent
    const parentResp = await request.post('/api/settings/menus', {
      data: { label: parentLabel, url: `/${faker.helpers.slugify(parentLabel).toLowerCase()}`, icon: '', isVisible: true },
    });
    const parent = await parentResp.json();

    // Create child
    const childResp = await request.post('/api/settings/menus', {
      data: { label: childLabel, url: `/${faker.helpers.slugify(childLabel).toLowerCase()}`, parentId: parent.id, icon: '', isVisible: true },
    });
    expect(childResp.status()).toBe(201);
    const child = await childResp.json();
    expect(child.parentId).toBe(parent.id);

    // Create grandchild
    const grandchildResp = await request.post('/api/settings/menus', {
      data: {
        label: grandchildLabel,
        url: `/${faker.helpers.slugify(grandchildLabel).toLowerCase()}`,
        parentId: child.id,
        icon: '',
        isVisible: true,
      },
    });
    expect(grandchildResp.status()).toBe(201);

    // Verify depth limit — great-grandchild should fail (depth >= 3)
    const grandchild = await grandchildResp.json();
    const tooDeepResp = await request.post('/api/settings/menus', {
      data: {
        label: faker.word.noun(),
        url: '/toodeep',
        parentId: grandchild.id,
        icon: '',
        isVisible: true,
      },
    });
    expect(tooDeepResp.status()).toBeGreaterThanOrEqual(400);

    // Cleanup
    await request.delete(`/api/settings/menus/${grandchild.id}`);
    await request.delete(`/api/settings/menus/${child.id}`);
    await request.delete(`/api/settings/menus/${parent.id}`);
  });

  test('set and clear home page via API', async ({ request }) => {
    const label = faker.location.city();

    // Create item
    const createResp = await request.post('/api/settings/menus', {
      data: { label, url: `/${faker.helpers.slugify(label).toLowerCase()}`, icon: '', isVisible: true },
    });
    const item = await createResp.json();

    // Set as home page
    const setHomeResp = await request.put(`/api/settings/menus/${item.id}/home`);
    expect(setHomeResp.status()).toBe(204);

    // Verify it's set
    const getResp = await request.get('/api/settings/menus');
    const items = await getResp.json();
    const flatStr = JSON.stringify(items);
    expect(flatStr).toContain('"isHomePage":true');

    // Clear home page
    const clearResp = await request.delete('/api/settings/menus/home');
    expect(clearResp.status()).toBe(204);

    // Cleanup
    await request.delete(`/api/settings/menus/${item.id}`);
  });

  test('reorder items via API', async ({ request }) => {
    const label1 = faker.color.human();
    const label2 = faker.color.human();

    // Create two items
    const resp1 = await request.post('/api/settings/menus', {
      data: { label: label1, url: `/${faker.helpers.slugify(label1).toLowerCase()}`, icon: '', isVisible: true },
    });
    const item1 = await resp1.json();

    const resp2 = await request.post('/api/settings/menus', {
      data: { label: label2, url: `/${faker.helpers.slugify(label2).toLowerCase()}`, icon: '', isVisible: true },
    });
    const item2 = await resp2.json();

    // Reorder: swap their positions
    const reorderResp = await request.put('/api/settings/menus/reorder', {
      data: {
        items: [
          { id: item1.id, parentId: null, sortOrder: 1 },
          { id: item2.id, parentId: null, sortOrder: 0 },
        ],
      },
    });
    expect(reorderResp.status()).toBe(204);

    // Cleanup
    await request.delete(`/api/settings/menus/${item1.id}`);
    await request.delete(`/api/settings/menus/${item2.id}`);
  });

  test('available pages endpoint returns module pages', async ({ request }) => {
    const response = await request.get('/api/settings/menus/available-pages');
    expect(response.ok()).toBeTruthy();
    const pages = await response.json();
    expect(Array.isArray(pages)).toBeTruthy();
    expect(pages.length).toBeGreaterThan(0);

    const first = pages[0];
    expect(first).toHaveProperty('pageRoute');
    expect(first).toHaveProperty('viewPrefix');
    expect(first).toHaveProperty('module');
  });
});

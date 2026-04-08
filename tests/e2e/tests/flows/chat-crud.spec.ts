import { faker } from '@faker-js/faker';
import { expect, test } from '../../fixtures/base';

test.describe('Chat conversations CRUD', () => {
  test('create, rename, list, delete a conversation', async ({ page, request }) => {
    // Discover a registered agent
    const agentsRes = await request.get('/api/agents/');
    expect(agentsRes.ok()).toBeTruthy();
    const agents = (await agentsRes.json()) as Array<{ name: string }>;
    test.skip(agents.length === 0, 'No registered agents; skipping chat flow.');

    const agentName = agents[0].name;
    const title = `e2e-chat-${faker.string.alphanumeric(6)}`;
    const renamed = `${title}-renamed`;

    // Create via API
    const createRes = await request.post('/api/chat/conversations', {
      data: { agentName, title },
    });
    expect(createRes.ok()).toBeTruthy();
    const conv = await createRes.json();
    expect(conv.id).toBeTruthy();

    // List should include it
    const listRes = await request.get('/api/chat/conversations');
    expect(listRes.ok()).toBeTruthy();
    const list = (await listRes.json()) as Array<{ id: string; title: string }>;
    expect(list.find((c) => c.id === conv.id)?.title).toBe(title);

    // UI: browse page shows the conversation title
    await page.goto('/chat');
    await expect(page.getByText(title)).toBeVisible();

    // Conversation page loads
    await page.goto(`/chat/${conv.id}`);
    await expect(page.locator('body')).toBeVisible();

    // Rename via API (PATCH)
    const renameRes = await request.patch(`/api/chat/conversations/${conv.id}`, {
      data: { title: renamed },
    });
    expect(renameRes.ok()).toBeTruthy();
    const afterRename = await renameRes.json();
    expect(afterRename.title).toBe(renamed);

    // Delete
    const deleteRes = await request.delete(`/api/chat/conversations/${conv.id}`);
    expect(deleteRes.ok()).toBeTruthy();

    // Verify gone via list
    const afterRes = await request.get('/api/chat/conversations');
    const after = (await afterRes.json()) as Array<{ id: string }>;
    expect(after.find((c) => c.id === conv.id)).toBeFalsy();
  });
});

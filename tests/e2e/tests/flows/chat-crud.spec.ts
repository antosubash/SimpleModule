import { faker } from '@faker-js/faker';
import { expect, test } from '../../fixtures/base';
import { ChatBrowsePage } from '../../pages/chat/browse.page';

test.describe('Chat CRUD flows', () => {
  test('create, list, rename and delete a conversation via API', async ({ page, request }) => {
    const title = `e2e-chat-${faker.string.alphanumeric(8)}`;
    const renamed = `${title}-renamed`;

    // API: create a conversation. Use a registered agent name from the codebase.
    const createRes = await request.post('/api/chat/conversations', {
      data: { agentName: 'product-search', title },
    });
    expect(createRes.ok()).toBeTruthy();
    const created = await createRes.json();
    expect(created.title).toBe(title);
    expect(created.agentName).toBe('product-search');
    const id = created.id;

    // API: verify in list
    const listRes = await request.get('/api/chat/conversations');
    expect(listRes.ok()).toBeTruthy();
    const conversations = await listRes.json();
    expect(conversations.some((c: { id: string }) => c.id === id)).toBeTruthy();

    // UI: browse page shows the conversation
    const browse = new ChatBrowsePage(page);
    await browse.goto();
    await expect(browse.conversationByTitle(title)).toBeVisible();

    // API: rename via PATCH
    const renameRes = await request.patch(`/api/chat/conversations/${id}`, {
      data: { title: renamed },
    });
    expect(renameRes.ok()).toBeTruthy();

    // API: verify rename
    const getRes = await request.get(`/api/chat/conversations/${id}`);
    expect(getRes.ok()).toBeTruthy();
    const got = await getRes.json();
    expect(got.title).toBe(renamed);

    // API: delete
    const deleteRes = await request.delete(`/api/chat/conversations/${id}`);
    expect(deleteRes.ok()).toBeTruthy();

    // API: verify gone from list
    const afterRes = await request.get('/api/chat/conversations');
    const after = await afterRes.json();
    expect(after.some((c: { id: string }) => c.id === id)).toBeFalsy();
  });

  test('fetch messages for a new conversation returns empty array', async ({ request }) => {
    const createRes = await request.post('/api/chat/conversations', {
      data: { agentName: 'product-search', title: 'messages-test' },
    });
    const created = await createRes.json();
    const id = created.id;

    const messagesRes = await request.get(`/api/chat/conversations/${id}/messages`);
    expect(messagesRes.ok()).toBeTruthy();
    const messages = await messagesRes.json();
    expect(Array.isArray(messages)).toBe(true);
    expect(messages.length).toBe(0);

    // Cleanup
    await request.delete(`/api/chat/conversations/${id}`);
  });

  test('create conversation via UI and verify it shows in list', async ({ page, request }) => {
    const title = `ui-chat-${faker.string.alphanumeric(6)}`;
    const browse = new ChatBrowsePage(page);
    await browse.goto();

    await browse.titleInput.fill(title);

    // Click "New chat" and wait for the POST to complete
    const [response] = await Promise.all([
      page.waitForResponse(
        (resp) =>
          resp.url().includes('/api/chat/conversations') && resp.request().method() === 'POST',
      ),
      browse.newChatButton.click(),
    ]);
    expect(response.ok()).toBeTruthy();
    const created = await response.json();

    // After creation, UI navigates to /chat/{id}. Navigate back and verify the new conversation.
    await browse.goto();
    await expect(browse.conversationByTitle(title)).toBeVisible();

    // Cleanup
    await request.delete(`/api/chat/conversations/${created.id}`);
  });

  test('create conversation requires a non-empty agent name', async ({ request }) => {
    const res = await request.post('/api/chat/conversations', {
      data: { agentName: '', title: 'bad' },
    });
    expect(res.status()).toBe(400);
  });
});

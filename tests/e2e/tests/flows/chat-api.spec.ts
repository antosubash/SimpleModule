import { expect, test } from '../../fixtures/base';

test.describe('Chat API', () => {
  test('GET /api/chat/conversations returns an array', async ({ request }) => {
    const res = await request.get('/api/chat/conversations');
    expect(res.ok()).toBeTruthy();
    const body = await res.json();
    expect(Array.isArray(body)).toBeTruthy();
  });

  test('create and delete a conversation via API', async ({ request }) => {
    const agents = (await (await request.get('/api/agents/')).json()) as Array<{ name: string }>;
    test.skip(agents.length === 0, 'No registered agents');

    const createRes = await request.post('/api/chat/conversations', {
      data: { agentName: agents[0].name, title: 'e2e api conversation' },
    });
    expect(createRes.ok()).toBeTruthy();
    const created = await createRes.json();
    expect(created.id).toBeTruthy();
    expect(created.agentName).toBe(agents[0].name);

    const getRes = await request.get(`/api/chat/conversations/${created.id}`);
    expect(getRes.ok()).toBeTruthy();

    const deleteRes = await request.delete(`/api/chat/conversations/${created.id}`);
    expect([200, 204]).toContain(deleteRes.status());

    // Verify gone
    const afterRes = await request.get(`/api/chat/conversations/${created.id}`);
    expect(afterRes.status()).toBe(404);
  });
});

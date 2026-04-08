import { expect, test } from '../../fixtures/base';

test.describe('Agents registry API', () => {
  test('GET /api/agents/ returns a list of registered agents', async ({ request }) => {
    const res = await request.get('/api/agents/');
    expect(res.ok()).toBeTruthy();
    const agents = await res.json();
    expect(Array.isArray(agents)).toBeTruthy();

    if (agents.length > 0) {
      const agent = agents[0];
      expect(typeof agent.name).toBe('string');
      expect(agent.name.length).toBeGreaterThan(0);
      // AgentInfo(name, description, moduleName)
      expect('description' in agent).toBeTruthy();
    }
  });
});

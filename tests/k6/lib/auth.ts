import { check, fail } from 'k6';
import http from 'k6/http';
import { config } from './config.ts';

export interface AuthResult {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}

export function authenticate(username?: string, password?: string): AuthResult {
  const res = http.post(
    `${config.baseUrl}${config.tokenEndpoint}`,
    {
      grant_type: 'password',
      client_id: config.clientId,
      username: username || config.username,
      password: password || config.password,
      scope: 'openid profile email roles',
    },
    {
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      tags: { name: 'auth' },
    },
  );

  const success = check(res, {
    'auth: status 200': (r) => r.status === 200,
    'auth: has access_token': (r) => {
      try {
        return JSON.parse(r.body as string).access_token !== undefined;
      } catch {
        return false;
      }
    },
  });

  if (!success) {
    fail(`Authentication failed: ${res.status} ${res.body}`);
  }

  const body = JSON.parse(res.body as string);
  return {
    accessToken: body.access_token,
    refreshToken: body.refresh_token,
    expiresIn: body.expires_in,
  };
}

export function authHeaders(token: string): Record<string, string> {
  return {
    Authorization: `Bearer ${token}`,
    'Content-Type': 'application/json',
  };
}

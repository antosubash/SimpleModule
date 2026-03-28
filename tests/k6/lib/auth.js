import http from 'k6/http';
import { check, fail } from 'k6';
import { config } from './config.js';

// Authenticate via OAuth2 password grant and return access token.
// Requires OpenIddict:AllowPasswordGrant=true (enabled in Development).
export function authenticate(username, password) {
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
        return JSON.parse(r.body).access_token !== undefined;
      } catch {
        return false;
      }
    },
  });

  if (!success) {
    fail(`Authentication failed: ${res.status} ${res.body}`);
  }

  const body = JSON.parse(res.body);
  return {
    accessToken: body.access_token,
    refreshToken: body.refresh_token,
    expiresIn: body.expires_in,
  };
}

// Return standard auth headers for API requests
export function authHeaders(token) {
  return {
    Authorization: `Bearer ${token}`,
    'Content-Type': 'application/json',
  };
}

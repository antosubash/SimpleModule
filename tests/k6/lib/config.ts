import type { Options } from 'k6/options';

interface AppConfig {
  baseUrl: string;
  clientId: string;
  username: string;
  password: string;
  tokenEndpoint: string;
}

export const config: AppConfig = {
  baseUrl: __ENV.K6_BASE_URL || 'https://localhost:5001',
  clientId: __ENV.K6_CLIENT_ID || 'simplemodule-client',
  username: __ENV.K6_USERNAME || 'admin@simplemodule.dev',
  password: __ENV.K6_PASSWORD || 'Admin123!',
  tokenEndpoint: '/connect/token',
};

export const tlsOptions: Pick<Options, 'insecureSkipTLSVerify'> = {
  insecureSkipTLSVerify: true,
};

export const defaultThresholds: Options['thresholds'] = {
  http_req_duration: ['p(95)<500', 'p(99)<1500'],
  http_req_failed: ['rate<0.01'],
};

interface LoadStage {
  duration: string;
  target: number;
}

interface LoadProfile {
  stages: LoadStage[];
}

export const loadProfiles: Record<string, LoadProfile> = {
  smoke: {
    stages: [
      { duration: '30s', target: 1 },
      { duration: '1m', target: 1 },
      { duration: '10s', target: 0 },
    ],
  },
  load: {
    stages: [
      { duration: '1m', target: 10 },
      { duration: '3m', target: 10 },
      { duration: '1m', target: 20 },
      { duration: '3m', target: 20 },
      { duration: '2m', target: 0 },
    ],
  },
  stress: {
    stages: [
      { duration: '1m', target: 20 },
      { duration: '2m', target: 50 },
      { duration: '2m', target: 100 },
      { duration: '2m', target: 100 },
      { duration: '2m', target: 0 },
    ],
  },
  spike: {
    stages: [
      { duration: '30s', target: 5 },
      { duration: '10s', target: 100 },
      { duration: '30s', target: 100 },
      { duration: '10s', target: 5 },
      { duration: '1m', target: 0 },
    ],
  },
  soak: {
    stages: [
      { duration: '2m', target: 20 },
      { duration: '30m', target: 20 },
      { duration: '2m', target: 0 },
    ],
  },
};

import { check } from 'k6';
import type { RefinedResponse, ResponseType } from 'k6/http';
import { Counter, Rate, Trend } from 'k6/metrics';

export const apiDuration = new Trend('api_duration', true);
export const apiErrors = new Rate('api_errors');
export const apiRequests = new Counter('api_requests');

export function checkResponse(
  res: RefinedResponse<ResponseType>,
  name: string,
  expectedStatus = 200,
): boolean {
  apiRequests.add(1);
  apiDuration.add(res.timings.duration);

  const passed = check(res, {
    [`${name}: status ${expectedStatus}`]: (r) => r.status === expectedStatus,
    [`${name}: response time < 1s`]: (r) => r.timings.duration < 1000,
  });

  if (!passed) {
    apiErrors.add(1);
  }

  return passed;
}

export function randomString(length = 8): string {
  const chars = 'abcdefghijklmnopqrstuvwxyz0123456789';
  let result = '';
  for (let i = 0; i < length; i++) {
    result += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  return result;
}

export function randomInt(min: number, max: number): number {
  return Math.floor(Math.random() * (max - min)) + min;
}

export function jitterSleep(baseSec: number, jitterSec = 1): number {
  return baseSec + Math.random() * jitterSec;
}

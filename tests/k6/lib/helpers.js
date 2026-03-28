import { check, group } from 'k6';
import { Trend, Rate, Counter } from 'k6/metrics';

// Custom metrics
export const apiDuration = new Trend('api_duration', true);
export const apiErrors = new Rate('api_errors');
export const apiRequests = new Counter('api_requests');

// Check a response and track custom metrics
export function checkResponse(res, name, expectedStatus = 200) {
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

// Generate a random string for unique test data
export function randomString(length = 8) {
  const chars = 'abcdefghijklmnopqrstuvwxyz0123456789';
  let result = '';
  for (let i = 0; i < length; i++) {
    result += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  return result;
}

// Generate a random integer between min (inclusive) and max (exclusive)
export function randomInt(min, max) {
  return Math.floor(Math.random() * (max - min)) + min;
}

// Sleep with random jitter to avoid thundering herd
export function jitterSleep(baseSec, jitterSec = 1) {
  const duration = baseSec + Math.random() * jitterSec;
  return duration;
}

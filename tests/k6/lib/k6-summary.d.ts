declare module 'https://jslib.k6.io/k6-summary/0.1.0/index.js' {
  export function textSummary(
    data: Record<string, unknown>,
    options?: { indent?: string; enableColors?: boolean },
  ): string;
}

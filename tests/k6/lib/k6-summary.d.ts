declare module 'https://jslib.k6.io/k6-summary/0.1.0/index.js' {
  // biome-ignore lint/suspicious/noExplicitAny: k6 summary data is untyped
  export function textSummary(
    data: any,
    options?: { indent?: string; enableColors?: boolean },
  ): string;
}

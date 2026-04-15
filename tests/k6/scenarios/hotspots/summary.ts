import { textSummary } from 'https://jslib.k6.io/k6-summary/0.1.0/index.js';

interface MetricEntry {
  name: string;
  avg: number;
  med: number;
  p90: number;
  p95: number;
  p99: number;
  max: number;
  count: number;
}

function fmt(ms: number): string {
  if (ms >= 1000) return `${(ms / 1000).toFixed(2)}s`;
  return `${ms.toFixed(1)}ms`;
}

// biome-ignore lint/suspicious/noExplicitAny: k6 handleSummary data has no typed definition
export function handleSummary(data: any) {
  const endpointMetrics: MetricEntry[] = [];
  for (const [key, metric] of Object.entries(data.metrics)) {
    if (key.startsWith('endpoint_')) {
      const m = metric as { values: Record<string, number> };
      if (m.values.count === 0) continue;
      endpointMetrics.push({
        name: key.replace('endpoint_', '').replace(/_/g, ' '),
        avg: m.values.avg,
        med: m.values.med,
        p90: m.values['p(90)'],
        p95: m.values['p(95)'],
        p99: m.values['p(99)'],
        max: m.values.max,
        count: m.values.count,
      });
    }
  }

  endpointMetrics.sort((a, b) => b.p95 - a.p95);

  let report = '\n====== HOTSPOT REPORT ======\n\n';
  report += 'Endpoints sorted by p95 latency (slowest first):\n\n';
  report += `${'Endpoint'.padEnd(30)} ${'Avg'.padStart(8)} ${'Med'.padStart(8)} ${'p90'.padStart(8)} ${'p95'.padStart(8)} ${'p99'.padStart(8)} ${'Max'.padStart(8)} ${'Count'.padStart(7)}\n`;
  report += `${'-'.repeat(105)}\n`;

  for (const m of endpointMetrics) {
    const flag = m.p95 > 500 ? ' *** HOTSPOT' : m.p95 > 200 ? ' * SLOW' : '';
    report += `${m.name.padEnd(30)} ${fmt(m.avg).padStart(8)} ${fmt(m.med).padStart(8)} ${fmt(m.p90).padStart(8)} ${fmt(m.p95).padStart(8)} ${fmt(m.p99).padStart(8)} ${fmt(m.max).padStart(8)} ${String(m.count).padStart(7)}${flag}\n`;
  }

  report += '\nLegend: *** HOTSPOT = p95 > 500ms | * SLOW = p95 > 200ms\n';
  report += '============================\n';

  return {
    stdout: textSummary(data, { indent: ' ', enableColors: true }) + report,
    'tests/k6/results/hotspot-report.txt': report,
  };
}

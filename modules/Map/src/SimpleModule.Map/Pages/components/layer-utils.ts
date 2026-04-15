export function reorder<T extends { order: number }>(items: T[], idx: number, delta: number): T[] {
  const target = idx + delta;
  if (target < 0 || target >= items.length) return items;
  const next = [...items];
  [next[idx], next[target]] = [next[target], next[idx]];
  return next.map((item, i) => ({ ...item, order: i }));
}

export function removeAt<T extends { order: number }>(items: T[], idx: number): T[] {
  return items.filter((_, i) => i !== idx).map((item, i) => ({ ...item, order: i }));
}

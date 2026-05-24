import { useEffect, useRef, useState } from 'react';

export function validateRequired(value: unknown): string | null {
  if (value === null || value === undefined || value === '') {
    return 'This field is required.';
  }
  return null;
}

export function validatePattern(value: string, pattern: string): string | null {
  try {
    const re = new RegExp(pattern);
    if (!re.test(value)) return 'Value does not match the required pattern.';
  } catch {
    // malformed pattern — skip
  }
  return null;
}

export function validateRange(value: number, min?: number, max?: number): string | null {
  if (min !== undefined && value < min) return `Value must be at least ${min}.`;
  if (max !== undefined && value > max) return `Value must be at most ${max}.`;
  return null;
}

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
export function validateEmail(value: string): string | null {
  if (!EMAIL_RE.test(value)) return 'Enter a valid email address.';
  return null;
}

export function validateUrl(value: string): string | null {
  try {
    new URL(value);
    return null;
  } catch {
    return 'Enter a valid URL.';
  }
}

const COLOR_RE = /^#[0-9a-fA-F]{6}$/;
export function validateColor(value: string): string | null {
  if (!COLOR_RE.test(value)) return 'Enter a valid hex color (e.g. #3b82f6).';
  return null;
}

export function useSyncedLocal<T>(initial: T): [T, (v: T) => void] {
  const [local, setLocal] = useState<T>(initial);
  const lastRef = useRef(initial);
  useEffect(() => {
    if (lastRef.current !== initial) {
      lastRef.current = initial;
      setLocal(initial);
    }
  }, [initial]);
  return [local, setLocal];
}

export function useSavedFlash(): [boolean, () => void] {
  const [saved, setSaved] = useState(false);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const flash = () => {
    if (timerRef.current) clearTimeout(timerRef.current);
    setSaved(true);
    timerRef.current = setTimeout(() => {
      setSaved(false);
      timerRef.current = null;
    }, 1500);
  };

  useEffect(
    () => () => {
      if (timerRef.current) clearTimeout(timerRef.current);
    },
    [],
  );

  return [saved, flash];
}

import { useTranslation } from '@simplemodule/client/use-translation';
import { Tooltip, TooltipContent, TooltipTrigger } from '@simplemodule/ui';
import { useEffect, useState } from 'react';
import { AuditLogsKeys } from '@/Locales/keys';

export function LabeledField({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <dt className="text-sm text-text-muted">{label}</dt>
      <dd className="mt-1 text-sm font-medium text-text">{children || '\u2014'}</dd>
    </div>
  );
}

export function formatJson(raw: string): string {
  try {
    return JSON.stringify(JSON.parse(raw), null, 2);
  } catch {
    return raw;
  }
}

export interface ChangeEntry {
  field: string;
  old?: unknown;
  new?: unknown;
  value?: unknown;
}

export function parseChanges(raw: string): ChangeEntry[] {
  try {
    const parsed = JSON.parse(raw);
    if (Array.isArray(parsed)) {
      return (parsed as Array<Record<string, unknown>>).map((item) => {
        if ('old' in item || 'new' in item) {
          return { field: String(item.field ?? ''), old: item.old, new: item.new };
        }
        if ('oldValue' in item || 'newValue' in item) {
          return { field: String(item.field ?? ''), old: item.oldValue, new: item.newValue };
        }
        return { field: String(item.field ?? ''), value: item.value };
      });
    }
    if (typeof parsed === 'object' && parsed !== null) {
      return Object.entries(parsed).map(([field, val]) => {
        if (
          typeof val === 'object' &&
          val !== null &&
          ('old' in val || 'new' in val || 'oldValue' in val || 'newValue' in val)
        ) {
          const v = val as { old?: unknown; new?: unknown; oldValue?: unknown; newValue?: unknown };
          return { field, old: v.old ?? v.oldValue, new: v.new ?? v.newValue };
        }
        return { field, value: val };
      });
    }
    return [];
  } catch {
    return [];
  }
}

export function hasUpdateStyle(changes: ChangeEntry[]): boolean {
  return changes.some((c) => 'old' in c || 'new' in c);
}

export function CopyButton({ text }: { text: string }) {
  const { t } = useTranslation('AuditLogs');
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    if (!copied) return;
    const id = setTimeout(() => setCopied(false), 2000);
    return () => clearTimeout(id);
  }, [copied]);

  function handleCopy() {
    navigator.clipboard.writeText(text).catch(() => {});
    setCopied(true);
  }

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <button
          type="button"
          onClick={handleCopy}
          className="ml-2 inline-flex items-center rounded p-1 text-text-muted hover:bg-surface-secondary hover:text-text"
        >
          {copied ? (
            <svg
              className="h-3.5 w-3.5"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <path d="M20 6 9 17l-5-5" />
            </svg>
          ) : (
            <svg
              className="h-3.5 w-3.5"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <rect x="9" y="9" width="13" height="13" rx="2" />
              <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
            </svg>
          )}
        </button>
      </TooltipTrigger>
      <TooltipContent>
        {copied ? t(AuditLogsKeys.Detail.CopyCopied) : t(AuditLogsKeys.Detail.CopyToClipboard)}
      </TooltipContent>
    </Tooltip>
  );
}

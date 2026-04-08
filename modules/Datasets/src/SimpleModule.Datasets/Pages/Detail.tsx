import { router } from '@inertiajs/react';
import { Button } from '@simplemodule/ui';
import { useState } from 'react';
import { FORMAT_NAMES, isRaster, isVector, STATUS_NAMES } from './labels';

interface DatasetDto {
  id: string;
  name: string;
  originalFileName: string;
  format: number;
  status: number;
  sourceSrid: number | null;
  srid: number | null;
  boundingBox: { minX: number; minY: number; maxX: number; maxY: number } | null;
  featureCount: number | null;
  sizeBytes: number;
  errorMessage: string | null;
  metadata: unknown;
  createdAt: string;
  processedAt: string | null;
}

interface Props {
  dataset: DatasetDto;
}

export default function Detail({ dataset }: Props) {
  const [busy, setBusy] = useState(false);
  const vector = isVector(dataset.format);
  const raster = isRaster(dataset.format);

  async function convertTo(target: string) {
    setBusy(true);
    const res = await fetch(`/api/datasets/${dataset.id}/convert`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ targetFormat: target }),
    });
    setBusy(false);
    if (res.ok) {
      router.reload();
    } else {
      alert(`Conversion enqueue failed: ${await res.text()}`);
    }
  }

  async function handleDelete() {
    if (!confirm(`Delete ${dataset.name}?`)) return;
    await fetch(`/api/datasets/${dataset.id}`, { method: 'DELETE' });
    router.visit('/datasets');
  }

  return (
    <div className="max-w-4xl mx-auto p-6 space-y-6">
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-semibold">{dataset.name}</h1>
          <div className="text-text-muted text-sm">{dataset.originalFileName}</div>
        </div>
        <div className="flex gap-2">
          <Button
            variant="secondary"
            onClick={() =>
              window.open(`/api/datasets/${dataset.id}/download?variant=original`, '_blank')
            }
          >
            Download original
          </Button>
          {vector && (
            <Button
              variant="secondary"
              onClick={() =>
                window.open(`/api/datasets/${dataset.id}/download?variant=GeoJson`, '_blank')
              }
            >
              Download GeoJSON
            </Button>
          )}
          {vector && (
            <Button onClick={() => convertTo('PmTiles')} disabled={busy}>
              Convert → PMTiles
            </Button>
          )}
          {raster && (
            <Button onClick={() => convertTo('Cog')} disabled={busy}>
              Convert → COG
            </Button>
          )}
          <Button variant="danger" onClick={handleDelete}>
            Delete
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <Field label="Format" value={FORMAT_NAMES[dataset.format] ?? String(dataset.format)} />
        <Field label="Status" value={STATUS_NAMES[dataset.status] ?? String(dataset.status)} />
        <Field label="Source SRID" value={dataset.sourceSrid?.toString() ?? '—'} />
        <Field label="Target SRID" value={dataset.srid?.toString() ?? '—'} />
        <Field label="Feature count" value={dataset.featureCount?.toString() ?? '—'} />
        <Field label="Size" value={`${dataset.sizeBytes.toLocaleString()} bytes`} />
        <Field label="Created" value={new Date(dataset.createdAt).toLocaleString()} />
        <Field
          label="Processed"
          value={dataset.processedAt ? new Date(dataset.processedAt).toLocaleString() : '—'}
        />
        {dataset.boundingBox && (
          <Field
            label="Bounding box"
            value={`${dataset.boundingBox.minX.toFixed(4)}, ${dataset.boundingBox.minY.toFixed(4)} → ${dataset.boundingBox.maxX.toFixed(4)}, ${dataset.boundingBox.maxY.toFixed(4)}`}
          />
        )}
      </div>

      {dataset.errorMessage && (
        <div className="bg-red-50 text-red-800 p-3 rounded">
          <strong>Error:</strong> {dataset.errorMessage}
        </div>
      )}

      {dataset.metadata ? (
        <details className="border rounded p-4" open>
          <summary className="font-medium cursor-pointer">Full metadata</summary>
          <pre className="mt-2 text-xs overflow-auto bg-muted p-3 rounded">
            {JSON.stringify(dataset.metadata, null, 2)}
          </pre>
        </details>
      ) : null}
    </div>
  );
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <div className="text-xs text-text-muted uppercase tracking-wide">{label}</div>
      <div className="text-sm">{value}</div>
    </div>
  );
}

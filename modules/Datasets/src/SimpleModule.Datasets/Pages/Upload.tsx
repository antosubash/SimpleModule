import { router } from '@inertiajs/react';
import { Button } from '@simplemodule/ui';
import { useRef, useState } from 'react';
import { DATASET_STATUS } from './labels';

export default function Upload() {
  const inputRef = useRef<HTMLInputElement>(null);
  const [status, setStatus] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function pollUntilComplete(id: string) {
    for (let i = 0; i < 120; i++) {
      const res = await fetch(`/api/datasets/${id}`);
      if (!res.ok) break;
      const dto = await res.json();
      if (dto.status === DATASET_STATUS.Ready || dto.status === DATASET_STATUS.Failed) {
        router.visit(`/datasets/${id}`);
        return;
      }
      setStatus(`Processing… (${i + 1}s)`);
      await new Promise((r) => setTimeout(r, 1000));
    }
    setStatus('Timed out waiting for processing.');
  }

  async function handleUpload(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;
    setBusy(true);
    setStatus(`Uploading ${file.name}…`);
    const form = new FormData();
    form.append('file', file);
    const res = await fetch('/api/datasets', { method: 'POST', body: form });
    if (!res.ok) {
      setStatus(`Upload failed: ${await res.text()}`);
      setBusy(false);
      return;
    }
    const dto = await res.json();
    setStatus(`Uploaded. Waiting for processing…`);
    await pollUntilComplete(dto.id);
    setBusy(false);
  }

  return (
    <div className="max-w-2xl mx-auto p-6 space-y-4">
      <h1 className="text-2xl font-semibold">Upload GIS Dataset</h1>
      <p className="text-text-muted">
        Supported formats: GeoJSON (.geojson, .json), Shapefile (.zip), KML/KMZ, GeoPackage (.gpkg),
        PMTiles (.pmtiles), COG (.tif/.tiff).
      </p>
      <input
        ref={inputRef}
        type="file"
        className="hidden"
        accept=".geojson,.json,.zip,.kml,.kmz,.gpkg,.pmtiles,.tif,.tiff"
        onChange={handleUpload}
      />
      <Button onClick={() => inputRef.current?.click()} disabled={busy}>
        Choose file
      </Button>
      {status && <div className="text-sm text-text-muted">{status}</div>}
    </div>
  );
}

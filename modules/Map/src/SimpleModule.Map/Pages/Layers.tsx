import { router } from '@inertiajs/react';
import {
  Button,
  Card,
  CardContent,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Field,
  FieldGroup,
  Input,
  Label,
  PageShell,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Textarea,
} from '@simplemodule/ui';
import { useState } from 'react';
import type { Basemap, CreateBasemapRequest, CreateLayerSourceRequest, LayerSource } from '@/types';
import { LayerSourceType } from './lib/layer-builders';

interface Props {
  sources: LayerSource[];
  basemaps: Basemap[];
}

const blankBasemap: CreateBasemapRequest = {
  name: '',
  description: '',
  styleUrl: '',
  attribution: '',
  thumbnailUrl: '',
};

const TYPE_LABELS: Record<number, string> = {
  [LayerSourceType.Wms]: 'WMS',
  [LayerSourceType.Wmts]: 'WMTS',
  [LayerSourceType.Wfs]: 'WFS',
  [LayerSourceType.Xyz]: 'XYZ tiles',
  [LayerSourceType.VectorTile]: 'Vector tiles',
  [LayerSourceType.PmTiles]: 'PMTiles',
  [LayerSourceType.Cog]: 'COG (cloud-optimized GeoTIFF)',
  [LayerSourceType.GeoJson]: 'GeoJSON',
};

const blankRequest: CreateLayerSourceRequest = {
  name: '',
  description: '',
  type: LayerSourceType.Wms,
  url: '',
  attribution: '',
  minZoom: null,
  maxZoom: null,
  bounds: null,
  metadata: {},
};

export default function Layers({ sources, basemaps }: Props) {
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState<CreateLayerSourceRequest>(blankRequest);
  const [metaText, setMetaText] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [bmOpen, setBmOpen] = useState(false);
  const [bmForm, setBmForm] = useState<CreateBasemapRequest>(blankBasemap);
  const [bmSubmitting, setBmSubmitting] = useState(false);
  const [bmError, setBmError] = useState<string | null>(null);

  async function handleBasemapSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setBmSubmitting(true);
    setBmError(null);
    try {
      const res = await fetch('/api/map/basemaps', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(bmForm),
      });
      if (!res.ok) {
        setBmError((await res.text()) || `Server returned ${res.status}`);
        return;
      }
      setBmOpen(false);
      setBmForm(blankBasemap);
      router.reload({ only: ['sources', 'basemaps'] });
    } finally {
      setBmSubmitting(false);
    }
  }

  async function handleBasemapDelete(id: string) {
    if (!window.confirm('Delete this basemap?')) return;
    await fetch(`/api/map/basemaps/${id}`, { method: 'DELETE' });
    router.reload({ only: ['sources', 'basemaps'] });
  }

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      const metadata: Record<string, string> = {};
      if (metaText.trim()) {
        try {
          Object.assign(metadata, JSON.parse(metaText));
        } catch {
          setError('Metadata must be valid JSON.');
          setSubmitting(false);
          return;
        }
      }
      const res = await fetch('/api/map/sources', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ...form, metadata }),
      });
      if (!res.ok) {
        const text = await res.text();
        setError(text || `Server returned ${res.status}`);
        setSubmitting(false);
        return;
      }
      setOpen(false);
      setForm(blankRequest);
      setMetaText('');
      router.reload({ only: ['sources', 'basemaps'] });
    } finally {
      setSubmitting(false);
    }
  }

  async function handleDelete(id: string) {
    if (!window.confirm('Delete this layer source?')) return;
    await fetch(`/api/map/sources/${id}`, { method: 'DELETE' });
    router.reload({ only: ['sources', 'basemaps'] });
  }

  return (
    <PageShell
      title="Map catalog"
      description="Manage basemaps and overlay sources (WMS, WFS, WMTS, PMTiles, COG, tiles)."
    >
      <div className="flex items-center justify-between mb-2">
        <h2 className="text-lg font-semibold">Basemaps</h2>
        <Button onClick={() => setBmOpen(true)} variant="secondary">
          Add basemap
        </Button>
      </div>
      <div className="space-y-2 mb-6">
        {basemaps.length === 0 && (
          <Card>
            <CardContent className="text-center text-text-muted py-6">No basemaps yet.</CardContent>
          </Card>
        )}
        {basemaps.map((b) => (
          <Card key={b.id} data-testid="basemap-card">
            <CardContent className="flex justify-between items-center">
              <div>
                <div className="font-medium">{b.name}</div>
                <div className="text-sm text-text-muted">{b.styleUrl}</div>
                {b.attribution && <div className="text-xs text-text-muted">{b.attribution}</div>}
              </div>
              <Button variant="danger" onClick={() => handleBasemapDelete(b.id)}>
                Delete
              </Button>
            </CardContent>
          </Card>
        ))}
      </div>

      <div className="flex items-center justify-between mb-2">
        <h2 className="text-lg font-semibold">Overlay sources</h2>
        <Button onClick={() => setOpen(true)}>Add layer source</Button>
      </div>

      <div className="space-y-2 sm:space-y-3">
        {sources.length === 0 && (
          <Card>
            <CardContent className="text-center text-text-muted py-8">
              No layer sources yet. Add one above.
            </CardContent>
          </Card>
        )}
        {sources.map((s) => (
          <Card key={s.id} data-testid="layer-source-card">
            <CardContent className="flex justify-between items-center">
              <div>
                <div className="font-medium">{s.name}</div>
                <div className="text-sm text-text-muted">
                  {TYPE_LABELS[s.type as unknown as number] ?? 'Unknown'} · {s.url}
                </div>
                {s.attribution && <div className="text-xs text-text-muted">{s.attribution}</div>}
              </div>
              <Button variant="danger" onClick={() => handleDelete(s.id)}>
                Delete
              </Button>
            </CardContent>
          </Card>
        ))}
      </div>

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Add layer source</DialogTitle>
          </DialogHeader>
          <form onSubmit={handleSubmit}>
            <FieldGroup>
              <Field>
                <Label htmlFor="name">Name</Label>
                <Input
                  id="name"
                  required
                  value={form.name}
                  onChange={(e) => setForm({ ...form, name: e.currentTarget.value })}
                />
              </Field>
              <Field>
                <Label htmlFor="type">Type</Label>
                <Select
                  value={String(form.type)}
                  onValueChange={(v) => setForm({ ...form, type: Number(v) as never })}
                >
                  <SelectTrigger id="type">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {Object.entries(TYPE_LABELS).map(([value, label]) => (
                      <SelectItem key={value} value={value}>
                        {label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </Field>
              <Field>
                <Label htmlFor="url">URL</Label>
                <Input
                  id="url"
                  required
                  value={form.url}
                  onChange={(e) => setForm({ ...form, url: e.currentTarget.value })}
                  placeholder="https://example.com/wms"
                />
              </Field>
              <Field>
                <Label htmlFor="attribution">Attribution</Label>
                <Input
                  id="attribution"
                  value={form.attribution ?? ''}
                  onChange={(e) => setForm({ ...form, attribution: e.currentTarget.value })}
                />
              </Field>
              <Field>
                <Label htmlFor="metadata">Metadata (JSON)</Label>
                <Textarea
                  id="metadata"
                  rows={4}
                  value={metaText}
                  onChange={(e) => setMetaText(e.currentTarget.value)}
                  placeholder='{"layers":"OSM-WMS","format":"image/png","crs":"EPSG:3857"}'
                />
              </Field>
              {error && <div className="text-sm text-danger">{error}</div>}
            </FieldGroup>
            <DialogFooter className="mt-4">
              <Button variant="secondary" type="button" onClick={() => setOpen(false)}>
                Cancel
              </Button>
              <Button type="submit" disabled={submitting}>
                {submitting ? 'Saving…' : 'Save'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={bmOpen} onOpenChange={setBmOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Add basemap</DialogTitle>
          </DialogHeader>
          <form onSubmit={handleBasemapSubmit}>
            <FieldGroup>
              <Field>
                <Label htmlFor="bm-name">Name</Label>
                <Input
                  id="bm-name"
                  required
                  value={bmForm.name}
                  onChange={(e) => setBmForm({ ...bmForm, name: e.currentTarget.value })}
                />
              </Field>
              <Field>
                <Label htmlFor="bm-style">MapLibre style URL</Label>
                <Input
                  id="bm-style"
                  required
                  value={bmForm.styleUrl}
                  onChange={(e) => setBmForm({ ...bmForm, styleUrl: e.currentTarget.value })}
                  placeholder="https://demotiles.maplibre.org/style.json"
                />
              </Field>
              <Field>
                <Label htmlFor="bm-attr">Attribution</Label>
                <Input
                  id="bm-attr"
                  value={bmForm.attribution ?? ''}
                  onChange={(e) => setBmForm({ ...bmForm, attribution: e.currentTarget.value })}
                />
              </Field>
              <Field>
                <Label htmlFor="bm-thumb">Thumbnail URL</Label>
                <Input
                  id="bm-thumb"
                  value={bmForm.thumbnailUrl ?? ''}
                  onChange={(e) => setBmForm({ ...bmForm, thumbnailUrl: e.currentTarget.value })}
                />
              </Field>
              {bmError && <div className="text-sm text-danger">{bmError}</div>}
            </FieldGroup>
            <DialogFooter className="mt-4">
              <Button variant="secondary" type="button" onClick={() => setBmOpen(false)}>
                Cancel
              </Button>
              <Button type="submit" disabled={bmSubmitting}>
                {bmSubmitting ? 'Saving…' : 'Save'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </PageShell>
  );
}

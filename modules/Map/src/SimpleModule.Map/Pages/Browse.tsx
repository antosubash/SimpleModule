import { router } from '@inertiajs/react';
import {
  Button,
  Card,
  CardContent,
  Container,
  Field,
  FieldGroup,
  Input,
  Label,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Switch,
} from '@simplemodule/ui';
import type { Map as MapLibreMap } from 'maplibre-gl';
import { useMemo, useRef, useState } from 'react';
import type {
  Basemap,
  LayerSource,
  MapBasemap,
  MapLayer,
  SavedMap,
  UpdateDefaultMapRequest,
} from '@/types';
import MapCanvas from './components/MapCanvas';

interface Props {
  map: SavedMap;
  sources: LayerSource[];
  basemaps: Basemap[];
  defaultStyleUrl: string;
  maxLayers: number;
  enableMeasure: boolean;
  enableExportPng: boolean;
  enableGeolocate: boolean;
}

export default function Browse({
  map,
  sources,
  basemaps,
  defaultStyleUrl,
  maxLayers,
  enableExportPng,
  enableGeolocate,
}: Props) {
  const [layers, setLayers] = useState<MapLayer[]>(map.layers);
  const [mapBasemaps, setMapBasemaps] = useState<MapBasemap[]>(map.basemaps);
  const [styleUrl, setStyleUrl] = useState(map.baseStyleUrl || defaultStyleUrl);
  const [pickerSourceId, setPickerSourceId] = useState<string>('');
  const [pickerBasemapId, setPickerBasemapId] = useState<string>('');
  const [availableSources, setAvailableSources] = useState<LayerSource[]>(sources);
  const [datasets, setDatasets] = useState<Array<{ id: string; name: string }>>([]);
  const [pickerDatasetId, setPickerDatasetId] = useState<string>('');
  const [datasetsLoaded, setDatasetsLoaded] = useState(false);
  const [saving, setSaving] = useState(false);

  const mapInstanceRef = useRef<MapLibreMap | null>(null);

  // Resolved style URL — prefer the first selected basemap, fall back to the
  // configured base style, then the host default.
  const activeStyleUrl = useMemo(() => {
    const byId = new Map(basemaps.map((b) => [b.id, b]));
    const first = [...mapBasemaps].sort((a, b) => a.order - b.order)[0];
    return first ? (byId.get(first.basemapId)?.styleUrl ?? styleUrl) : styleUrl;
  }, [mapBasemaps, basemaps, styleUrl]);

  const sourceById = useMemo(
    () => new Map(availableSources.map((s) => [s.id, s])),
    [availableSources],
  );
  const basemapById = useMemo(() => new Map(basemaps.map((b) => [b.id, b])), [basemaps]);

  async function loadDatasets() {
    if (datasetsLoaded) return;
    const res = await fetch('/api/datasets/', { headers: { Accept: 'application/json' } });
    if (!res.ok) return;
    const list = (await res.json()) as Array<{ id: string; name: string }>;
    setDatasets(list);
    setDatasetsLoaded(true);
  }

  async function addFromDataset() {
    if (!pickerDatasetId || layers.length >= maxLayers) return;
    const res = await fetch('/api/map/sources/from-dataset', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ datasetId: pickerDatasetId }),
    });
    if (!res.ok) return;
    const created = (await res.json()) as LayerSource;
    setAvailableSources((prev) => [...prev, created]);
    setLayers([
      ...layers,
      {
        layerSourceId: created.id,
        order: layers.length,
        visible: true,
        opacity: 1,
        styleOverrides: {},
      },
    ]);
    setPickerDatasetId('');
  }

  function addLayer() {
    if (!pickerSourceId || layers.length >= maxLayers) return;
    setLayers([
      ...layers,
      {
        layerSourceId: pickerSourceId,
        order: layers.length,
        visible: true,
        opacity: 1,
        styleOverrides: {},
      },
    ]);
    setPickerSourceId('');
  }

  function removeLayer(idx: number) {
    const next = layers.filter((_, i) => i !== idx);
    next.forEach((l, i) => {
      l.order = i;
    });
    setLayers(next);
  }

  function moveLayer(idx: number, delta: number) {
    const next = [...layers];
    const target = idx + delta;
    if (target < 0 || target >= next.length) return;
    [next[idx], next[target]] = [next[target], next[idx]];
    next.forEach((l, i) => {
      l.order = i;
    });
    setLayers(next);
  }

  function patchLayer(idx: number, patch: Partial<MapLayer>) {
    setLayers(layers.map((l, i) => (i === idx ? { ...l, ...patch } : l)));
  }

  function addBasemap() {
    if (!pickerBasemapId) return;
    if (mapBasemaps.some((b) => b.basemapId === pickerBasemapId)) return;
    setMapBasemaps([...mapBasemaps, { basemapId: pickerBasemapId, order: mapBasemaps.length }]);
    setPickerBasemapId('');
  }

  function removeBasemap(idx: number) {
    const next = mapBasemaps.filter((_, i) => i !== idx);
    next.forEach((b, i) => {
      b.order = i;
    });
    setMapBasemaps(next);
  }

  function moveBasemap(idx: number, delta: number) {
    const next = [...mapBasemaps];
    const target = idx + delta;
    if (target < 0 || target >= next.length) return;
    [next[idx], next[target]] = [next[target], next[idx]];
    next.forEach((b, i) => {
      b.order = i;
    });
    setMapBasemaps(next);
  }

  async function handleSave() {
    setSaving(true);
    try {
      const body: UpdateDefaultMapRequest = {
        centerLng: map.centerLng,
        centerLat: map.centerLat,
        zoom: map.zoom,
        pitch: map.pitch,
        bearing: map.bearing,
        baseStyleUrl: styleUrl,
        layers,
        basemaps: mapBasemaps,
      };
      const res = await fetch('/api/map/default', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      });
      if (res.ok) {
        router.reload({ only: ['map'] });
      }
    } finally {
      setSaving(false);
    }
  }

  function exportPng() {
    const m = mapInstanceRef.current;
    if (!m) return;
    m.getCanvas().toBlob((blob) => {
      if (!blob) return;
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'default-map.png';
      a.click();
      URL.revokeObjectURL(url);
    });
  }

  return (
    <Container className="space-y-3">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-bold">Default map</h1>
          <p className="text-sm text-text-muted">
            Add layers, basemaps and tools to the application's single shared map.
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="secondary" onClick={() => router.visit('/map/layers')}>
            Manage catalog
          </Button>
          <Button onClick={handleSave} disabled={saving}>
            {saving ? 'Saving…' : 'Save'}
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-3">
        <div className="lg:col-span-1 space-y-3">
          <Card>
            <CardContent className="p-4">
              <FieldGroup>
                <Field>
                  <Label htmlFor="style">Fallback base style URL</Label>
                  <Input
                    id="style"
                    value={styleUrl}
                    onChange={(e) => setStyleUrl(e.currentTarget.value)}
                  />
                </Field>
              </FieldGroup>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-4 space-y-2">
              <div className="font-medium">Basemaps ({mapBasemaps.length})</div>
              {mapBasemaps.length === 0 && (
                <div className="text-xs text-text-muted">
                  No basemaps yet. The fallback style URL above will be used.
                </div>
              )}
              {mapBasemaps.map((mb, idx) => {
                const def = basemapById.get(mb.basemapId);
                return (
                  <div
                    // biome-ignore lint/suspicious/noArrayIndexKey: order is the stable identity here
                    key={`${mb.basemapId}-${idx}`}
                    className="border border-border rounded p-2 flex items-center justify-between"
                  >
                    <div className="font-medium text-sm truncate">
                      {def?.name ?? 'Unknown basemap'}
                      {idx === 0 && <span className="ml-2 text-xs text-text-muted">(default)</span>}
                    </div>
                    <div className="flex gap-1">
                      <Button size="sm" variant="secondary" onClick={() => moveBasemap(idx, -1)}>
                        ↑
                      </Button>
                      <Button size="sm" variant="secondary" onClick={() => moveBasemap(idx, 1)}>
                        ↓
                      </Button>
                      <Button size="sm" variant="danger" onClick={() => removeBasemap(idx)}>
                        ×
                      </Button>
                    </div>
                  </div>
                );
              })}
              <div className="flex gap-2 pt-2">
                <Select value={pickerBasemapId} onValueChange={setPickerBasemapId}>
                  <SelectTrigger className="flex-1">
                    <SelectValue placeholder="Add basemap…" />
                  </SelectTrigger>
                  <SelectContent>
                    {basemaps
                      .filter((b) => !mapBasemaps.some((mb) => mb.basemapId === b.id))
                      .map((b) => (
                        <SelectItem key={b.id} value={b.id}>
                          {b.name}
                        </SelectItem>
                      ))}
                  </SelectContent>
                </Select>
                <Button onClick={addBasemap} disabled={!pickerBasemapId}>
                  Add
                </Button>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-4 space-y-2">
              <div className="font-medium">Layers ({layers.length})</div>
              {layers.length === 0 && (
                <div className="text-xs text-text-muted">No layers yet. Add one below.</div>
              )}
              {layers.map((layer, idx) => {
                const src = sourceById.get(layer.layerSourceId);
                return (
                  <div
                    // biome-ignore lint/suspicious/noArrayIndexKey: layer order is the stable identity here
                    key={`${layer.layerSourceId}-${idx}`}
                    className="border border-border rounded p-2 space-y-1"
                  >
                    <div className="flex items-center justify-between">
                      <div className="font-medium text-sm truncate">
                        {src?.name ?? 'Unknown source'}
                      </div>
                      <div className="flex gap-1">
                        <Button size="sm" variant="secondary" onClick={() => moveLayer(idx, -1)}>
                          ↑
                        </Button>
                        <Button size="sm" variant="secondary" onClick={() => moveLayer(idx, 1)}>
                          ↓
                        </Button>
                        <Button size="sm" variant="danger" onClick={() => removeLayer(idx)}>
                          ×
                        </Button>
                      </div>
                    </div>
                    <div className="flex items-center gap-2 text-xs">
                      <Switch
                        checked={layer.visible}
                        onCheckedChange={(v) => patchLayer(idx, { visible: v })}
                      />
                      <span>Visible</span>
                      <Label htmlFor={`opacity-${idx}`} className="ml-auto">
                        Opacity
                      </Label>
                      <Input
                        id={`opacity-${idx}`}
                        type="number"
                        min={0}
                        max={1}
                        step={0.05}
                        value={layer.opacity}
                        onChange={(e) =>
                          patchLayer(idx, { opacity: Number(e.currentTarget.value) })
                        }
                        className="w-16"
                      />
                    </div>
                  </div>
                );
              })}
              <div className="flex gap-2 pt-2">
                <Select value={pickerSourceId} onValueChange={setPickerSourceId}>
                  <SelectTrigger className="flex-1">
                    <SelectValue placeholder="Add source…" />
                  </SelectTrigger>
                  <SelectContent>
                    {availableSources.map((s) => (
                      <SelectItem key={s.id} value={s.id}>
                        {s.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <Button onClick={addLayer} disabled={!pickerSourceId || layers.length >= maxLayers}>
                  Add
                </Button>
              </div>
              <div className="flex gap-2 pt-2">
                <Select
                  value={pickerDatasetId}
                  onValueChange={setPickerDatasetId}
                  onOpenChange={(open) => {
                    if (open) loadDatasets();
                  }}
                >
                  <SelectTrigger className="flex-1">
                    <SelectValue placeholder="Add from dataset…" />
                  </SelectTrigger>
                  <SelectContent>
                    {datasets.map((d) => (
                      <SelectItem key={d.id} value={d.id}>
                        {d.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <Button
                  onClick={addFromDataset}
                  disabled={!pickerDatasetId || layers.length >= maxLayers}
                >
                  Add
                </Button>
              </div>
            </CardContent>
          </Card>

          {enableExportPng && (
            <Card>
              <CardContent className="p-4">
                <Button variant="secondary" onClick={exportPng} className="w-full">
                  Export PNG
                </Button>
              </CardContent>
            </Card>
          )}
        </div>

        <div className="lg:col-span-2" style={{ height: '70vh' }}>
          <div className="rounded border border-border h-full" data-testid="map-canvas-container">
            <MapCanvas
              styleUrl={activeStyleUrl}
              center={[map.centerLng, map.centerLat]}
              zoom={map.zoom}
              pitch={map.pitch}
              bearing={map.bearing}
              layers={layers}
              sources={availableSources}
              enableGeolocate={enableGeolocate}
              onMapReady={(m) => {
                mapInstanceRef.current = m;
              }}
            />
          </div>
        </div>
      </div>
    </Container>
  );
}

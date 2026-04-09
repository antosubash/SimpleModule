import { router } from '@inertiajs/react';
import {
  Button,
  Card,
  CardContent,
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
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

// Moves the item at `idx` by `delta` positions, rewriting `.order` on every
// element so it matches its new array index. Returns a new array; returns the
// original reference when the move would go out of bounds.
function reorder<T extends { order: number }>(items: T[], idx: number, delta: number): T[] {
  const target = idx + delta;
  if (target < 0 || target >= items.length) return items;
  const next = [...items];
  [next[idx], next[target]] = [next[target], next[idx]];
  return next.map((item, i) => ({ ...item, order: i }));
}

function removeAt<T extends { order: number }>(items: T[], idx: number): T[] {
  return items.filter((_, i) => i !== idx).map((item, i) => ({ ...item, order: i }));
}

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
  const [panelOpen, setPanelOpen] = useState(true);
  const [advancedOpen, setAdvancedOpen] = useState(false);

  const mapInstanceRef = useRef<MapLibreMap | null>(null);

  const basemapById = useMemo(() => new Map(basemaps.map((b) => [b.id, b])), [basemaps]);
  const sourceById = useMemo(
    () => new Map(availableSources.map((s) => [s.id, s])),
    [availableSources],
  );

  const availableBasemaps = useMemo(() => {
    return [...mapBasemaps]
      .sort((a, b) => a.order - b.order)
      .map((mb) => basemapById.get(mb.basemapId))
      .filter((b): b is Basemap => Boolean(b));
  }, [mapBasemaps, basemapById]);

  // Local selection for the floating basemap chip switcher; does not persist.
  // The persisted default is the first entry in mapBasemaps.
  const [activeBasemapId, setActiveBasemapId] = useState<string | undefined>(
    () => availableBasemaps[0]?.id,
  );
  const activeBasemap =
    availableBasemaps.find((b) => b.id === activeBasemapId) ?? availableBasemaps[0];
  const resolvedStyleUrl = activeBasemap?.styleUrl ?? styleUrl;

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

  function patchLayer(idx: number, patch: Partial<MapLayer>) {
    setLayers(layers.map((l, i) => (i === idx ? { ...l, ...patch } : l)));
  }

  function addBasemap() {
    if (!pickerBasemapId) return;
    if (mapBasemaps.some((b) => b.basemapId === pickerBasemapId)) return;
    setMapBasemaps([...mapBasemaps, { basemapId: pickerBasemapId, order: mapBasemaps.length }]);
    if (!activeBasemapId) {
      setActiveBasemapId(pickerBasemapId);
    }
    setPickerBasemapId('');
  }

  function removeBasemap(idx: number) {
    const removedId = mapBasemaps[idx]?.basemapId;
    const next = removeAt(mapBasemaps, idx);
    setMapBasemaps(next);
    if (removedId && activeBasemapId === removedId) {
      setActiveBasemapId(next[0]?.basemapId);
    }
  }

  async function handleSave() {
    setSaving(true);
    try {
      // Read the current viewport from MapLibre so panning/zooming is persisted.
      const live = mapInstanceRef.current;
      const center = live?.getCenter();
      const body: UpdateDefaultMapRequest = {
        centerLng: center?.lng ?? map.centerLng,
        centerLat: center?.lat ?? map.centerLat,
        zoom: live?.getZoom() ?? map.zoom,
        pitch: live?.getPitch() ?? map.pitch,
        bearing: live?.getBearing() ?? map.bearing,
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

  const visibleLayerCount = layers.filter((l) => l.visible).length;

  return (
    <Container size="full" className="space-y-3 py-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="min-w-0">
          <h1 className="text-xl font-bold leading-tight">Default map</h1>
          <p className="text-xs text-text-muted">
            {visibleLayerCount} of {layers.length} layer{layers.length === 1 ? '' : 's'} visible
            {availableBasemaps.length > 0 && ` · ${availableBasemaps.length} basemap(s)`}
          </p>
        </div>
        <div className="flex gap-2">
          <Button
            variant="secondary"
            size="sm"
            onClick={() => setPanelOpen((v) => !v)}
            aria-label={panelOpen ? 'Hide configuration panel' : 'Show configuration panel'}
          >
            {panelOpen ? '◀ Hide panel' : '▶ Show panel'}
          </Button>
          <Button variant="secondary" size="sm" onClick={() => router.visit('/map/layers')}>
            Manage catalog
          </Button>
          <Button size="sm" onClick={handleSave} disabled={saving}>
            {saving ? 'Saving…' : 'Save'}
          </Button>
        </div>
      </div>

      <div className="flex gap-3" style={{ height: 'calc(100vh - 160px)', minHeight: '520px' }}>
        {panelOpen && (
          <aside
            className="w-80 shrink-0 overflow-y-auto space-y-3 pr-1"
            data-testid="map-side-panel"
          >
            <Card>
              <CardContent className="p-3 space-y-2">
                <div className="flex items-center justify-between">
                  <div className="font-medium text-sm">Basemaps ({mapBasemaps.length})</div>
                </div>
                {mapBasemaps.length === 0 && (
                  <div className="text-xs text-text-muted">
                    No basemaps yet. The fallback style URL will be used.
                  </div>
                )}
                {mapBasemaps.map((mb, idx) => {
                  const def = basemapById.get(mb.basemapId);
                  const isActive = def?.id === activeBasemapId;
                  return (
                    <div
                      key={mb.basemapId}
                      className={`border rounded p-2 flex items-center justify-between ${
                        isActive ? 'border-primary bg-primary/5' : 'border-border'
                      }`}
                    >
                      <div className="font-medium text-sm truncate min-w-0">
                        {def?.name ?? 'Unknown basemap'}
                        {idx === 0 && (
                          <span className="ml-2 text-xs text-text-muted">(default)</span>
                        )}
                      </div>
                      <div className="flex gap-1 shrink-0">
                        <Button
                          size="sm"
                          variant="secondary"
                          onClick={() => setMapBasemaps(reorder(mapBasemaps, idx, -1))}
                        >
                          ↑
                        </Button>
                        <Button
                          size="sm"
                          variant="secondary"
                          onClick={() => setMapBasemaps(reorder(mapBasemaps, idx, 1))}
                        >
                          ↓
                        </Button>
                        <Button size="sm" variant="danger" onClick={() => removeBasemap(idx)}>
                          ×
                        </Button>
                      </div>
                    </div>
                  );
                })}
                <div className="flex gap-2 pt-1">
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
                  <Button size="sm" onClick={addBasemap} disabled={!pickerBasemapId}>
                    Add
                  </Button>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardContent className="p-3 space-y-2">
                <div className="font-medium text-sm">Layers ({layers.length})</div>
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
                        <div className="font-medium text-sm truncate min-w-0">
                          {src?.name ?? 'Unknown source'}
                        </div>
                        <div className="flex gap-1 shrink-0">
                          <Button
                            size="sm"
                            variant="secondary"
                            onClick={() => setLayers(reorder(layers, idx, -1))}
                          >
                            ↑
                          </Button>
                          <Button
                            size="sm"
                            variant="secondary"
                            onClick={() => setLayers(reorder(layers, idx, 1))}
                          >
                            ↓
                          </Button>
                          <Button
                            size="sm"
                            variant="danger"
                            onClick={() => setLayers(removeAt(layers, idx))}
                          >
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
                <div className="flex gap-2 pt-1">
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
                  <Button
                    size="sm"
                    onClick={addLayer}
                    disabled={!pickerSourceId || layers.length >= maxLayers}
                  >
                    Add
                  </Button>
                </div>
                <div className="flex gap-2">
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
                    size="sm"
                    onClick={addFromDataset}
                    disabled={!pickerDatasetId || layers.length >= maxLayers}
                  >
                    Add
                  </Button>
                </div>
              </CardContent>
            </Card>

            <Collapsible open={advancedOpen} onOpenChange={setAdvancedOpen}>
              <Card>
                <CardContent className="p-3 space-y-2">
                  <CollapsibleTrigger asChild>
                    <button
                      type="button"
                      className="font-medium text-sm w-full text-left flex items-center justify-between"
                    >
                      <span>Advanced</span>
                      <span className="text-xs text-text-muted">{advancedOpen ? '▲' : '▼'}</span>
                    </button>
                  </CollapsibleTrigger>
                  <CollapsibleContent>
                    <FieldGroup>
                      <Field>
                        <Label htmlFor="style">Fallback base style URL</Label>
                        <Input
                          id="style"
                          value={styleUrl}
                          onChange={(e) => setStyleUrl(e.currentTarget.value)}
                        />
                        <p className="text-xs text-text-muted">Used when no basemap is selected.</p>
                      </Field>
                    </FieldGroup>
                  </CollapsibleContent>
                </CardContent>
              </Card>
            </Collapsible>
          </aside>
        )}

        <div className="flex-1 relative rounded border border-border overflow-hidden bg-surface-muted">
          <MapCanvas
            styleUrl={resolvedStyleUrl}
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

          <div className="pointer-events-none absolute top-3 left-3 bg-surface/90 backdrop-blur rounded-md shadow px-3 py-1.5 text-xs text-text-muted border border-border">
            {visibleLayerCount} / {layers.length} layer{layers.length === 1 ? '' : 's'}
            {activeBasemap && ` · ${activeBasemap.name}`}
          </div>

          {layers.length === 0 && availableBasemaps.length === 0 && (
            <div className="pointer-events-none absolute inset-0 flex items-center justify-center">
              <div className="bg-surface/90 backdrop-blur rounded-lg shadow-lg border border-border px-6 py-4 text-center">
                <div className="font-medium text-sm">This map is empty</div>
                <div className="text-xs text-text-muted mt-1">
                  Add a basemap or layer from the side panel to get started.
                </div>
              </div>
            </div>
          )}

          {availableBasemaps.length > 1 && (
            <div className="absolute bottom-3 left-3 bg-surface/90 backdrop-blur rounded-md shadow border border-border p-1.5 flex gap-1 flex-wrap max-w-[60%]">
              {availableBasemaps.map((b) => (
                <Button
                  key={b.id}
                  size="sm"
                  variant={b.id === activeBasemap?.id ? 'primary' : 'secondary'}
                  onClick={() => setActiveBasemapId(b.id)}
                >
                  {b.name}
                </Button>
              ))}
            </div>
          )}

          {enableExportPng && (
            <div className="absolute bottom-3 right-3">
              <Button size="sm" variant="secondary" onClick={exportPng} className="shadow">
                Export PNG
              </Button>
            </div>
          )}
        </div>
      </div>
    </Container>
  );
}

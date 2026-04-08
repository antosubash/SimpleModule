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
import { useState } from 'react';
import type {
  Basemap,
  LayerSource,
  MapBasemap,
  MapLayer,
  SavedMap,
  UpdateMapRequest,
} from '@/types';
import MapCanvas from './components/MapCanvas';

interface Props {
  map: SavedMap;
  sources: LayerSource[];
  basemaps: Basemap[];
  defaultStyleUrl: string;
  maxLayers: number;
}

export default function Edit({ map, sources, basemaps, defaultStyleUrl, maxLayers }: Props) {
  const [name, setName] = useState(map.name);
  const [description, setDescription] = useState(map.description ?? '');
  const [styleUrl, setStyleUrl] = useState(map.baseStyleUrl || defaultStyleUrl);
  const [layers, setLayers] = useState<MapLayer[]>(map.layers);
  const [mapBasemaps, setMapBasemaps] = useState<MapBasemap[]>(map.basemaps);
  const [pickerSourceId, setPickerSourceId] = useState<string>('');
  const [pickerBasemapId, setPickerBasemapId] = useState<string>('');
  const [saving, setSaving] = useState(false);

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

  function addLayer() {
    if (!pickerSourceId) return;
    if (layers.length >= maxLayers) return;
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

  function patchLayer(idx: number, patch: Partial<MapLayer>) {
    const next = layers.map((l, i) => (i === idx ? { ...l, ...patch } : l));
    setLayers(next);
  }

  async function handleSave() {
    setSaving(true);
    const body: UpdateMapRequest = {
      name,
      description,
      centerLng: map.centerLng,
      centerLat: map.centerLat,
      zoom: map.zoom,
      pitch: map.pitch,
      bearing: map.bearing,
      baseStyleUrl: styleUrl,
      layers,
      basemaps: mapBasemaps,
    };
    await fetch(`/api/map/maps/${map.id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
    setSaving(false);
    router.visit(`/map/${map.id}`);
  }

  const sourceById = new Map(sources.map((s) => [s.id, s]));
  const basemapById = new Map(basemaps.map((b) => [b.id, b]));

  return (
    <Container className="space-y-3">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-bold">Edit map</h1>
        <div className="flex gap-2">
          <Button variant="secondary" onClick={() => router.visit('/map')}>
            Cancel
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
                  <Label htmlFor="name">Name</Label>
                  <Input id="name" value={name} onChange={(e) => setName(e.currentTarget.value)} />
                </Field>
                <Field>
                  <Label htmlFor="description">Description</Label>
                  <Input
                    id="description"
                    value={description}
                    onChange={(e) => setDescription(e.currentTarget.value)}
                  />
                </Field>
                <Field>
                  <Label htmlFor="style">Base style URL</Label>
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
                    {sources.map((s) => (
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
            </CardContent>
          </Card>
        </div>

        <div className="lg:col-span-2" style={{ height: '70vh' }}>
          <div className="rounded border border-border h-full">
            <MapCanvas
              styleUrl={styleUrl}
              center={[map.centerLng, map.centerLat]}
              zoom={map.zoom}
              pitch={map.pitch}
              bearing={map.bearing}
              layers={layers}
              sources={sources}
            />
          </div>
        </div>
      </div>
    </Container>
  );
}

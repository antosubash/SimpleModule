import { router } from '@inertiajs/react';
import { Button, Container } from '@simplemodule/ui';
import type { Map as MapLibreMap } from 'maplibre-gl';
import { useMemo, useRef, useState } from 'react';
import type { Basemap, LayerSource, SavedMap } from '@/types';
import MapCanvas from './components/MapCanvas';

interface Props {
  map: SavedMap;
  sources: LayerSource[];
  basemaps: Basemap[];
  defaultStyleUrl: string;
  enableExportPng: boolean;
  enableGeolocate: boolean;
}

export default function View({
  map,
  sources,
  basemaps,
  defaultStyleUrl,
  enableExportPng,
  enableGeolocate,
}: Props) {
  const availableBasemaps = useMemo(() => {
    const byId = new Map(basemaps.map((b) => [b.id, b]));
    return [...map.basemaps]
      .sort((a, b) => a.order - b.order)
      .map((mb) => byId.get(mb.basemapId))
      .filter((b): b is Basemap => Boolean(b));
  }, [map.basemaps, basemaps]);

  const [activeBasemapId, setActiveBasemapId] = useState<string | undefined>(
    () => availableBasemaps[0]?.id,
  );
  const activeBasemap = availableBasemaps.find((b) => b.id === activeBasemapId);
  const styleUrl = activeBasemap?.styleUrl ?? map.baseStyleUrl ?? defaultStyleUrl;

  const mapInstanceRef = useRef<MapLibreMap | null>(null);

  function exportPng() {
    const m = mapInstanceRef.current;
    if (!m) return;
    m.getCanvas().toBlob((blob) => {
      if (!blob) return;
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${map.name}.png`;
      a.click();
      URL.revokeObjectURL(url);
    });
  }

  return (
    <Container className="space-y-3">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-bold">{map.name}</h1>
          {map.description && <p className="text-sm text-text-muted">{map.description}</p>}
        </div>
        <div className="flex gap-2">
          <Button variant="secondary" onClick={() => router.visit('/map')}>
            Back
          </Button>
          <Button onClick={() => router.visit(`/map/${map.id}/edit`)}>Edit</Button>
        </div>
      </div>

      {availableBasemaps.length > 0 && (
        <div className="flex flex-wrap gap-2 items-center">
          <span className="text-sm text-text-muted">Basemap:</span>
          {availableBasemaps.map((b) => (
            <Button
              key={b.id}
              size="sm"
              variant={b.id === activeBasemapId ? 'primary' : 'secondary'}
              onClick={() => setActiveBasemapId(b.id)}
            >
              {b.name}
            </Button>
          ))}
        </div>
      )}

      <div style={{ height: '70vh', width: '100%' }} className="rounded border border-border">
        <MapCanvas
          styleUrl={styleUrl}
          center={[map.centerLng, map.centerLat]}
          zoom={map.zoom}
          pitch={map.pitch}
          bearing={map.bearing}
          layers={map.layers}
          sources={sources}
          enableGeolocate={enableGeolocate}
          onMapReady={(m) => {
            mapInstanceRef.current = m;
          }}
        />
      </div>

      {enableExportPng && (
        <Button variant="secondary" onClick={exportPng}>
          Export PNG
        </Button>
      )}
    </Container>
  );
}

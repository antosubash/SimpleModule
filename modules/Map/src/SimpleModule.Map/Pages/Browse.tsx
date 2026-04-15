import { router } from '@inertiajs/react';
import type { Map as MapLibreMap } from 'maplibre-gl';
import { type CSSProperties, useMemo, useRef, useState } from 'react';
import type {
  Basemap,
  LayerSource,
  MapBasemap,
  MapLayer,
  SavedMap,
  UpdateDefaultMapRequest,
} from '@/types';
import { BasemapsPanel } from './components/BasemapsPanel';
import { LayersPanel } from './components/LayersPanel';
import { removeAt } from './components/layer-utils';
import { MapBottomControls } from './components/MapBottomControls';
import MapCanvas from './components/MapCanvas';
import { MapTopControls } from './components/MapTopControls';
import { useViewportInsets } from './components/useViewportInsets';

// Inline styles for positioning — deliberately NOT Tailwind utility classes.
// The Tailwind utilities top-3/bottom-3/right-3/etc. aren't always generated
// by the host's CSS pipeline when only module .tsx sources change, so the map
// layout regressed in CI. Inline styles make the positioning survive that.
const controlOverlayStyle: CSSProperties = {
  position: 'absolute',
  inset: 0,
  zIndex: 1000,
  pointerEvents: 'none',
};

const floatingControlStyle: CSSProperties = {
  position: 'absolute',
  pointerEvents: 'auto',
};

const floatingPanelStyle: CSSProperties = {
  ...floatingControlStyle,
};

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

  // Single-value state enforces mutual exclusion — two panels cannot be open at once.
  const [openPanel, setOpenPanel] = useState<'layers' | 'basemaps' | null>(null);
  const layersPanelOpen = openPanel === 'layers';
  const basemapsPanelOpen = openPanel === 'basemaps';

  const mapInstanceRef = useRef<MapLibreMap | null>(null);
  const insets = useViewportInsets();

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
    <div
      style={{
        position: 'fixed',
        top: insets.top,
        left: insets.left,
        right: 0,
        bottom: 0,
      }}
    >
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

      <div style={controlOverlayStyle}>
        <MapTopControls
          floatingControlStyle={floatingControlStyle}
          layersPanelOpen={layersPanelOpen}
          basemapsPanelOpen={basemapsPanelOpen}
          visibleLayerCount={visibleLayerCount}
          totalLayers={layers.length}
          basemapsCount={mapBasemaps.length}
          saving={saving}
          onToggleLayers={() => setOpenPanel(layersPanelOpen ? null : 'layers')}
          onToggleBasemaps={() => setOpenPanel(basemapsPanelOpen ? null : 'basemaps')}
          onSave={handleSave}
        />

        {layersPanelOpen && (
          <LayersPanel
            style={{ ...floatingPanelStyle, top: 56, left: 12 }}
            layers={layers}
            setLayers={setLayers}
            sourceById={sourceById}
            availableSources={availableSources}
            datasets={datasets}
            pickerSourceId={pickerSourceId}
            setPickerSourceId={setPickerSourceId}
            pickerDatasetId={pickerDatasetId}
            setPickerDatasetId={setPickerDatasetId}
            maxLayers={maxLayers}
            loadDatasets={loadDatasets}
            addLayer={addLayer}
            addFromDataset={addFromDataset}
          />
        )}

        {basemapsPanelOpen && (
          <BasemapsPanel
            style={{ ...floatingPanelStyle, top: 56, left: 12 }}
            mapBasemaps={mapBasemaps}
            setMapBasemaps={setMapBasemaps}
            basemaps={basemaps}
            basemapById={basemapById}
            activeBasemapId={activeBasemapId}
            pickerBasemapId={pickerBasemapId}
            setPickerBasemapId={setPickerBasemapId}
            styleUrl={styleUrl}
            setStyleUrl={setStyleUrl}
            addBasemap={addBasemap}
            removeBasemap={removeBasemap}
          />
        )}

        <MapBottomControls
          floatingControlStyle={floatingControlStyle}
          availableBasemaps={availableBasemaps}
          activeBasemapId={activeBasemap?.id}
          setActiveBasemapId={setActiveBasemapId}
          enableExportPng={enableExportPng}
          onExportPng={exportPng}
        />
      </div>
    </div>
  );
}

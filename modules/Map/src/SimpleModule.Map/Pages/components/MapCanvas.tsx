import maplibregl, { type Map as MapLibreMap } from 'maplibre-gl';
import 'maplibre-gl/dist/maplibre-gl.css';
import { useEffect, useRef } from 'react';
import type { LayerSource, MapLayer } from '@/types';
import { buildMapLibreLayer } from '../lib/layer-builders';
import { registerMapProtocols } from '../lib/protocols';

export interface MapCanvasProps {
  styleUrl: string;
  center: [number, number];
  zoom: number;
  pitch?: number;
  bearing?: number;
  layers: MapLayer[];
  sources: LayerSource[];
  enableGeolocate?: boolean;
  className?: string;
  onMapReady?: (map: MapLibreMap) => void;
}

export default function MapCanvas({
  styleUrl,
  center,
  zoom,
  pitch = 0,
  bearing = 0,
  layers,
  sources,
  enableGeolocate = true,
  className,
  onMapReady,
}: MapCanvasProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const mapRef = useRef<MapLibreMap | null>(null);
  const lastStyleUrlRef = useRef<string>(styleUrl);

  // biome-ignore lint/correctness/useExhaustiveDependencies: intentional one-time init; subsequent prop changes flow through dedicated effects
  useEffect(() => {
    if (!containerRef.current) return;
    registerMapProtocols();

    const map = new maplibregl.Map({
      container: containerRef.current,
      style: styleUrl,
      center,
      zoom,
      pitch,
      bearing,
    });
    mapRef.current = map;

    map.addControl(new maplibregl.NavigationControl({ visualizePitch: true }), 'top-right');
    map.addControl(new maplibregl.ScaleControl({ maxWidth: 120, unit: 'metric' }), 'bottom-left');
    map.addControl(new maplibregl.FullscreenControl(), 'top-right');
    if (enableGeolocate) {
      map.addControl(
        new maplibregl.GeolocateControl({
          positionOptions: { enableHighAccuracy: true },
          trackUserLocation: true,
        }),
        'top-right',
      );
    }
    map.addControl(new maplibregl.AttributionControl({ compact: true }));

    map.on('load', () => {
      onMapReady?.(map);
    });

    return () => {
      map.remove();
      mapRef.current = null;
    };
  }, []);

  useEffect(() => {
    const map = mapRef.current;
    if (!map) return;

    const apply = () => {
      const styleLayers = map.getStyle().layers ?? [];
      const existingLayerIds = styleLayers.map((l) => l.id).filter((id) => id.startsWith('layer-'));
      const existingSourceIds = Object.keys(map.getStyle().sources ?? {}).filter((id) =>
        id.startsWith('layer-source-'),
      );

      for (const id of existingLayerIds) {
        if (map.getLayer(id)) map.removeLayer(id);
      }
      for (const id of existingSourceIds) {
        if (map.getSource(id)) map.removeSource(id);
      }

      const sortedLayers = [...layers].sort((a, b) => a.order - b.order);
      const sourceById = new Map(sources.map((s) => [s.id, s]));

      for (const layer of sortedLayers) {
        const source = sourceById.get(layer.layerSourceId);
        if (!source) continue;
        const built = buildMapLibreLayer(source, layer);
        if (!built) continue;
        if (!map.getSource(built.sourceId)) {
          map.addSource(built.sourceId, built.source);
        }
        map.addLayer(built.layer);
      }
    };

    if (map.isStyleLoaded()) {
      apply();
    } else {
      map.once('load', apply);
    }

    // setStyle() wipes user-added sources/layers, so re-apply after every basemap swap.
    map.on('style.load', apply);
    return () => {
      map.off('style.load', apply);
    };
  }, [layers, sources]);

  useEffect(() => {
    const map = mapRef.current;
    if (!map) return;
    if (lastStyleUrlRef.current === styleUrl) return;
    lastStyleUrlRef.current = styleUrl;
    map.setStyle(styleUrl);
  }, [styleUrl]);

  useEffect(() => {
    const map = mapRef.current;
    if (!map) return;
    map.jumpTo({ center, zoom, pitch, bearing });
  }, [center, zoom, pitch, bearing]);

  return <div ref={containerRef} className={className} style={{ width: '100%', height: '100%' }} />;
}

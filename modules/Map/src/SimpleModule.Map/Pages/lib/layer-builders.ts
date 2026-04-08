import type { LayerSpecification, SourceSpecification } from 'maplibre-gl';
import type { LayerSource, MapLayer } from '@/types';

/**
 * LayerSourceType numeric values match the C# enum in
 * `SimpleModule.Map.Contracts.LayerSourceType`. Keep in sync.
 */
export const LayerSourceType = {
  Wms: 0,
  Wmts: 1,
  Wfs: 2,
  Xyz: 3,
  VectorTile: 4,
  PmTiles: 5,
  Cog: 6,
  GeoJson: 7,
  Dataset: 8,
} as const;

export type BuiltLayer = {
  sourceId: string;
  source: SourceSpecification;
  layer: LayerSpecification;
};

function buildWmsTileUrl(base: string, meta: Record<string, string>): string {
  const sep = base.includes('?') ? '&' : '?';
  const params = new URLSearchParams({
    service: 'WMS',
    request: 'GetMap',
    version: meta.version ?? '1.3.0',
    layers: meta.layers ?? '',
    styles: meta.styles ?? '',
    format: meta.format ?? 'image/png',
    transparent: meta.transparent ?? 'true',
    crs: meta.crs ?? 'EPSG:3857',
    width: '256',
    height: '256',
    bbox: '{bbox-epsg-3857}',
  });
  return `${base}${sep}${params.toString()}`;
}

/**
 * Translate a server-side {@link LayerSource} + composition overrides ({@link MapLayer})
 * into a MapLibre source/layer pair ready to be added to the map.
 *
 * Returns `null` for unsupported types — the caller should log and skip.
 */
export function buildMapLibreLayer(source: LayerSource, composition: MapLayer): BuiltLayer | null {
  const sourceId = `layer-source-${source.id}`;
  const layerId = `layer-${source.id}`;
  const meta = source.metadata ?? {};
  const visibility = composition.visible ? 'visible' : 'none';
  const opacity = composition.opacity;
  // biome-ignore lint/suspicious/noExplicitAny: maplibre type narrowing across source types
  const t = source.type as any as number;

  switch (t) {
    case LayerSourceType.Wms: {
      const tileUrl = buildWmsTileUrl(source.url, meta);
      return {
        sourceId,
        source: {
          type: 'raster',
          tiles: [tileUrl],
          tileSize: 256,
          attribution: source.attribution ?? undefined,
          minzoom: source.minZoom ?? undefined,
          maxzoom: source.maxZoom ?? undefined,
        } as SourceSpecification,
        layer: {
          id: layerId,
          type: 'raster',
          source: sourceId,
          paint: { 'raster-opacity': opacity },
          layout: { visibility },
        } as LayerSpecification,
      };
    }
    case LayerSourceType.Wmts:
    case LayerSourceType.Xyz: {
      return {
        sourceId,
        source: {
          type: 'raster',
          tiles: [source.url],
          tileSize: 256,
          attribution: source.attribution ?? undefined,
          minzoom: source.minZoom ?? undefined,
          maxzoom: source.maxZoom ?? undefined,
        } as SourceSpecification,
        layer: {
          id: layerId,
          type: 'raster',
          source: sourceId,
          paint: { 'raster-opacity': opacity },
          layout: { visibility },
        } as LayerSpecification,
      };
    }
    case LayerSourceType.VectorTile: {
      return {
        sourceId,
        source: {
          type: 'vector',
          tiles: [source.url],
          attribution: source.attribution ?? undefined,
          minzoom: source.minZoom ?? undefined,
          maxzoom: source.maxZoom ?? undefined,
        } as SourceSpecification,
        layer: {
          id: layerId,
          type: 'line',
          source: sourceId,
          'source-layer': meta.sourceLayer ?? 'default',
          paint: { 'line-opacity': opacity },
          layout: { visibility },
        } as LayerSpecification,
      };
    }
    case LayerSourceType.Wfs:
    case LayerSourceType.GeoJson:
    case LayerSourceType.Dataset: {
      return {
        sourceId,
        source: {
          type: 'geojson',
          data: source.url,
          attribution: source.attribution ?? undefined,
        } as SourceSpecification,
        layer: {
          id: layerId,
          type: 'circle',
          source: sourceId,
          paint: {
            'circle-radius': 4,
            'circle-color': meta.color ?? '#3b82f6',
            'circle-opacity': opacity,
          },
          layout: { visibility },
        } as LayerSpecification,
      };
    }
    case LayerSourceType.PmTiles: {
      const isVector = (meta.tileType ?? 'vector') === 'vector';
      return {
        sourceId,
        source: {
          type: isVector ? 'vector' : 'raster',
          url: `pmtiles://${source.url}`,
          attribution: source.attribution ?? undefined,
        } as SourceSpecification,
        layer: isVector
          ? ({
              id: layerId,
              type: 'fill',
              source: sourceId,
              'source-layer': meta.sourceLayer ?? 'default',
              paint: { 'fill-opacity': opacity },
              layout: { visibility },
            } as LayerSpecification)
          : ({
              id: layerId,
              type: 'raster',
              source: sourceId,
              paint: { 'raster-opacity': opacity },
              layout: { visibility },
            } as LayerSpecification),
      };
    }
    case LayerSourceType.Cog: {
      return {
        sourceId,
        source: {
          type: 'raster',
          url: `cog://${source.url}`,
          tileSize: 256,
          attribution: source.attribution ?? undefined,
        } as SourceSpecification,
        layer: {
          id: layerId,
          type: 'raster',
          source: sourceId,
          paint: { 'raster-opacity': opacity },
          layout: { visibility },
        } as LayerSpecification,
      };
    }
    default:
      return null;
  }
}

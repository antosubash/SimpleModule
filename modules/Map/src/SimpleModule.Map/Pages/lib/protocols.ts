import cogProtocol from '@geomatico/maplibre-cog-protocol';
import maplibregl from 'maplibre-gl';
import { Protocol as PMTilesProtocol } from 'pmtiles';

let registered = false;

/**
 * Register the PMTiles and COG (cloud-optimized GeoTIFF) protocols with MapLibre
 * exactly once per browser session. After this runs, MapLibre sources can use
 * `pmtiles://<url>` and `cog://<url>` URL schemes.
 */
export function registerMapProtocols(): void {
  if (registered) return;
  registered = true;

  const pmtiles = new PMTilesProtocol();
  maplibregl.addProtocol('pmtiles', pmtiles.tile);

  maplibregl.addProtocol('cog', cogProtocol);
}

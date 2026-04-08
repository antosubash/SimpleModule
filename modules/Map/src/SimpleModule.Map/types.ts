// Auto-generated from [Dto] types — do not edit
export interface Basemap {
  name: string;
  description: string;
  styleUrl: string;
  attribution: string;
  thumbnailUrl: string;
  createdBy: string;
  updatedBy: string;
  id: string;
  createdAt: string;
  updatedAt: string;
  concurrencyStamp: string;
}

export interface CreateBasemapRequest {
  name: string;
  description: string;
  styleUrl: string;
  attribution: string;
  thumbnailUrl: string;
}

export interface CreateLayerSourceRequest {
  name: string;
  description: string;
  type: any;
  url: string;
  attribution: string;
  minZoom: number | null;
  maxZoom: number | null;
  bounds: any;
  metadata: Record<string, string>;
}

export interface CreateMapRequest {
  name: string;
  description: string;
  centerLng: number;
  centerLat: number;
  zoom: number;
  pitch: number;
  bearing: number;
  baseStyleUrl: string;
  layers: MapLayer[];
  basemaps: MapBasemap[];
}

export interface LayerSource {
  name: string;
  description: string;
  type: any;
  url: string;
  attribution: string;
  minZoom: number | null;
  maxZoom: number | null;
  bounds: any;
  metadata: Record<string, string>;
  createdBy: string;
  updatedBy: string;
  id: string;
  createdAt: string;
  updatedAt: string;
  concurrencyStamp: string;
}

export interface MapBasemap {
  basemapId: string;
  order: number;
}

export interface MapLayer {
  layerSourceId: string;
  order: number;
  visible: boolean;
  opacity: number;
  styleOverrides: Record<string, string>;
}

export interface SavedMap {
  name: string;
  description: string;
  centerLng: number;
  centerLat: number;
  zoom: number;
  pitch: number;
  bearing: number;
  baseStyleUrl: string;
  layers: MapLayer[];
  basemaps: MapBasemap[];
  createdBy: string;
  updatedBy: string;
  id: string;
  createdAt: string;
  updatedAt: string;
  concurrencyStamp: string;
}

export interface UpdateBasemapRequest {
  name: string;
  description: string;
  styleUrl: string;
  attribution: string;
  thumbnailUrl: string;
}

export interface UpdateLayerSourceRequest {
  name: string;
  description: string;
  type: any;
  url: string;
  attribution: string;
  minZoom: number | null;
  maxZoom: number | null;
  bounds: any;
  metadata: Record<string, string>;
}

export interface UpdateMapRequest {
  name: string;
  description: string;
  centerLng: number;
  centerLat: number;
  zoom: number;
  pitch: number;
  bearing: number;
  baseStyleUrl: string;
  layers: MapLayer[];
  basemaps: MapBasemap[];
}


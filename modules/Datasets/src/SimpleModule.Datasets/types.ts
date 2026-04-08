// Auto-generated from [Dto] types — do not edit
export interface BoundingBoxDto {
  minX: number;
  minY: number;
  maxX: number;
  maxY: number;
}

export interface DatasetDto {
  id: string;
  name: string;
  originalFileName: string;
  format: any;
  status: any;
  sourceSrid: number | null;
  srid: number | null;
  boundingBox: BoundingBoxDto;
  featureCount: number | null;
  sizeBytes: number;
  errorMessage: string;
  metadata: DatasetMetadata;
  createdAt: string;
  processedAt: string | null;
}

export interface DatasetFeatureDto {
  id: string;
  geometryGeoJson: string;
  properties: Record<string, string>;
}

export interface DatasetMetadata {
  common: CommonMetadata;
  vector: VectorMetadata;
  raster: RasterMetadata;
  tiles: TileMetadata;
  derivatives: DatasetDerivative[];
}

export interface CommonMetadata {
  sourceFormat: string;
  originalFileName: string;
  sizeBytes: number;
  contentHash: string;
  sourceSrid: number | null;
  targetSrid: number | null;
  boundingBox: BoundingBoxDto;
  processingDurationMs: number;
  processorVersion: string;
}

export interface VectorMetadata {
  featureCount: number;
  geometryTypes: string[];
  attributeSchema: AttributeField[];
  layerNames: string[];
  encoding: string;
  crsWkt: string;
}

export interface AttributeField {
  name: string;
  type: string;
  sampleValues: string[];
}

export interface RasterMetadata {
  width: number;
  height: number;
  bandCount: number;
  bandTypes: string[];
  noDataValue: number | null;
  pixelSizeX: number;
  pixelSizeY: number;
  overviewLevels: number[];
  compression: string;
  crsWkt: string;
}

export interface TileMetadata {
  tileFormat: string;
  minZoom: number;
  maxZoom: number;
  centerLon: number;
  centerLat: number;
  tileCount: number;
  headerVersion: number;
  layerNames: string[];
}

export interface DatasetDerivative {
  format: any;
  storagePath: string;
  sizeBytes: number;
  createdAt: string;
}

export interface DatasetsPermissions {
}


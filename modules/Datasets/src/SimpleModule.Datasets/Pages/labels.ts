export const FORMAT_NAMES = [
  'Unknown',
  'GeoJSON',
  'Shapefile',
  'KML',
  'KMZ',
  'GeoPackage',
  'PMTiles',
  'COG',
];

export const STATUS_NAMES = ['Pending', 'Processing', 'Ready', 'Failed'];

export const DATASET_STATUS = { Pending: 0, Processing: 1, Ready: 2, Failed: 3 } as const;

export const DATASET_FORMAT = {
  Unknown: 0,
  GeoJson: 1,
  Shapefile: 2,
  Kml: 3,
  Kmz: 4,
  GeoPackage: 5,
  PmTiles: 6,
  Cog: 7,
} as const;

export function isVector(format: number): boolean {
  return format >= DATASET_FORMAT.GeoJson && format <= DATASET_FORMAT.GeoPackage;
}

export function isRaster(format: number): boolean {
  return format === DATASET_FORMAT.Cog;
}

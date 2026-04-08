import { router } from '@inertiajs/react';
import { Button, Card, CardContent, PageShell } from '@simplemodule/ui';
import type { LayerSource, MapLayer, SavedMap } from '@/types';
import MapCanvas from './components/MapCanvas';

interface Props {
  maps: SavedMap[];
  sources: LayerSource[];
  defaultStyleUrl: string;
}

export default function Browse({ maps, sources, defaultStyleUrl }: Props) {
  // Render every seeded layer source stacked on top of the default basemap so
  // the preview shows the full catalog out of the box.
  const previewLayers: MapLayer[] = sources.map((s, index) => ({
    layerSourceId: s.id,
    order: index,
    visible: true,
    opacity: 1,
    styleOverrides: {},
  }));

  return (
    <PageShell title="Maps" description="Saved map compositions and viewers.">
      <div className="flex justify-end mb-3">
        <Button onClick={() => router.visit('/map/layers')} variant="secondary">
          Manage layer sources
        </Button>
      </div>
      <Card className="mb-4 overflow-hidden">
        <MapCanvas
          styleUrl={defaultStyleUrl}
          center={[0, 20]}
          zoom={1.5}
          layers={previewLayers}
          sources={sources}
          className="h-[420px] w-full"
        />
      </Card>
      <div className="space-y-2 sm:space-y-3">
        {maps.length === 0 && (
          <Card>
            <CardContent className="text-center text-text-muted py-8">
              No saved maps yet. Add layer sources first, then create a map composition.
            </CardContent>
          </Card>
        )}
        {maps.map((m) => (
          <Card key={m.id} data-testid="map-card">
            <CardContent className="flex justify-between items-center">
              <div>
                <div className="font-medium">{m.name}</div>
                {m.description && <div className="text-sm text-text-muted">{m.description}</div>}
                <div className="text-xs text-text-muted">
                  {m.layers.length} layer{m.layers.length === 1 ? '' : 's'} · zoom {m.zoom}
                </div>
              </div>
              <div className="flex gap-2">
                <Button onClick={() => router.visit(`/map/${m.id}`)}>View</Button>
                <Button onClick={() => router.visit(`/map/${m.id}/edit`)} variant="secondary">
                  Edit
                </Button>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    </PageShell>
  );
}

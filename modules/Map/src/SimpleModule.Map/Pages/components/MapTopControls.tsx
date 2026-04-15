import { router } from '@inertiajs/react';
import { Button } from '@simplemodule/ui';
import type { CSSProperties } from 'react';

interface Props {
  floatingControlStyle: CSSProperties;
  layersPanelOpen: boolean;
  basemapsPanelOpen: boolean;
  visibleLayerCount: number;
  totalLayers: number;
  basemapsCount: number;
  saving: boolean;
  onToggleLayers: () => void;
  onToggleBasemaps: () => void;
  onSave: () => void;
}

export function MapTopControls({
  floatingControlStyle,
  layersPanelOpen,
  basemapsPanelOpen,
  visibleLayerCount,
  totalLayers,
  basemapsCount,
  saving,
  onToggleLayers,
  onToggleBasemaps,
  onSave,
}: Props) {
  return (
    <>
      <div style={{ ...floatingControlStyle, top: 12, left: 12 }} className="flex gap-2">
        <Button
          size="sm"
          variant={layersPanelOpen ? 'primary' : 'secondary'}
          className="shadow"
          onClick={onToggleLayers}
          data-testid="layers-toggle"
        >
          Layers ({visibleLayerCount}/{totalLayers})
        </Button>
        <Button
          size="sm"
          variant={basemapsPanelOpen ? 'primary' : 'secondary'}
          className="shadow"
          onClick={onToggleBasemaps}
          data-testid="basemaps-toggle"
        >
          Basemaps ({basemapsCount})
        </Button>
      </div>

      <div style={{ ...floatingControlStyle, top: 12, right: 12 }} className="flex gap-2">
        <Button
          variant="secondary"
          size="sm"
          className="shadow"
          onClick={() => router.visit('/map/layers')}
        >
          Manage catalog
        </Button>
        <Button size="sm" className="shadow" onClick={onSave} disabled={saving}>
          {saving ? 'Saving…' : 'Save'}
        </Button>
      </div>
    </>
  );
}

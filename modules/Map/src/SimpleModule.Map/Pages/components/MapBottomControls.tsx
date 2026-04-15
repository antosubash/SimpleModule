import { Button } from '@simplemodule/ui';
import type { CSSProperties } from 'react';
import type { Basemap } from '@/types';

interface Props {
  floatingControlStyle: CSSProperties;
  availableBasemaps: Basemap[];
  activeBasemapId: string | undefined;
  setActiveBasemapId: (id: string) => void;
  enableExportPng: boolean;
  onExportPng: () => void;
}

export function MapBottomControls({
  floatingControlStyle,
  availableBasemaps,
  activeBasemapId,
  setActiveBasemapId,
  enableExportPng,
  onExportPng,
}: Props) {
  return (
    <>
      {availableBasemaps.length > 1 && (
        <div
          style={{ ...floatingControlStyle, bottom: 12, left: 12 }}
          className="bg-surface/90 backdrop-blur rounded-md shadow border border-border p-1.5 flex gap-1 flex-wrap max-w-[60%]"
        >
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

      {enableExportPng && (
        <div style={{ ...floatingControlStyle, bottom: 12, right: 12 }}>
          <Button size="sm" variant="secondary" onClick={onExportPng} className="shadow">
            Export PNG
          </Button>
        </div>
      )}
    </>
  );
}

import {
  Button,
  Input,
  Label,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@simplemodule/ui';
import type { CSSProperties } from 'react';
import type { Basemap, MapBasemap } from '@/types';
import { reorder } from './layer-utils';

interface Props {
  style: CSSProperties;
  mapBasemaps: MapBasemap[];
  setMapBasemaps: (bs: MapBasemap[]) => void;
  basemaps: Basemap[];
  basemapById: Map<string, Basemap>;
  activeBasemapId: string | undefined;
  pickerBasemapId: string;
  setPickerBasemapId: (id: string) => void;
  styleUrl: string;
  setStyleUrl: (url: string) => void;
  addBasemap: () => void;
  removeBasemap: (idx: number) => void;
}

export function BasemapsPanel({
  style,
  mapBasemaps,
  setMapBasemaps,
  basemaps,
  basemapById,
  activeBasemapId,
  pickerBasemapId,
  setPickerBasemapId,
  styleUrl,
  setStyleUrl,
  addBasemap,
  removeBasemap,
}: Props) {
  return (
    <div
      style={style}
      className="w-80 max-h-[60vh] overflow-y-auto bg-surface/95 backdrop-blur rounded-lg shadow-lg border border-border p-3 space-y-2"
      data-testid="basemaps-panel"
    >
      <div className="font-medium text-sm">Basemaps ({mapBasemaps.length})</div>
      {mapBasemaps.length === 0 && (
        <div className="text-xs text-text-muted">
          No basemaps yet. The fallback style URL will be used.
        </div>
      )}
      {mapBasemaps.map((mb, idx) => {
        const def = basemapById.get(mb.basemapId);
        const isActive = def?.id === activeBasemapId;
        return (
          <div
            key={mb.basemapId}
            className={`border rounded p-2 flex items-center justify-between ${
              isActive ? 'border-primary bg-primary/5' : 'border-border'
            }`}
          >
            <div className="font-medium text-sm truncate min-w-0">
              {def?.name ?? 'Unknown basemap'}
              {idx === 0 && <span className="ml-2 text-xs text-text-muted">(default)</span>}
            </div>
            <div className="flex gap-1 shrink-0">
              <Button
                size="sm"
                variant="secondary"
                onClick={() => setMapBasemaps(reorder(mapBasemaps, idx, -1))}
              >
                ↑
              </Button>
              <Button
                size="sm"
                variant="secondary"
                onClick={() => setMapBasemaps(reorder(mapBasemaps, idx, 1))}
              >
                ↓
              </Button>
              <Button size="sm" variant="danger" onClick={() => removeBasemap(idx)}>
                ×
              </Button>
            </div>
          </div>
        );
      })}
      <div className="flex gap-2 pt-1">
        <Select value={pickerBasemapId} onValueChange={setPickerBasemapId}>
          <SelectTrigger className="flex-1">
            <SelectValue placeholder="Add basemap…" />
          </SelectTrigger>
          <SelectContent>
            {basemaps
              .filter((b) => !mapBasemaps.some((mb) => mb.basemapId === b.id))
              .map((b) => (
                <SelectItem key={b.id} value={b.id}>
                  {b.name}
                </SelectItem>
              ))}
          </SelectContent>
        </Select>
        <Button size="sm" onClick={addBasemap} disabled={!pickerBasemapId}>
          Add
        </Button>
      </div>

      <div className="border-t border-border pt-2 mt-2">
        <Label htmlFor="fallback-style" className="text-xs text-text-muted">
          Fallback style URL
        </Label>
        <Input
          id="fallback-style"
          value={styleUrl}
          onChange={(e) => setStyleUrl(e.currentTarget.value)}
          className="mt-1"
        />
      </div>
    </div>
  );
}

import {
  Button,
  Input,
  Label,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Switch,
} from '@simplemodule/ui';
import type { CSSProperties } from 'react';
import type { LayerSource, MapLayer } from '@/types';
import { removeAt, reorder } from './layer-utils';

interface Props {
  style: CSSProperties;
  layers: MapLayer[];
  setLayers: (layers: MapLayer[]) => void;
  sourceById: Map<string, LayerSource>;
  availableSources: LayerSource[];
  datasets: Array<{ id: string; name: string }>;
  pickerSourceId: string;
  setPickerSourceId: (id: string) => void;
  pickerDatasetId: string;
  setPickerDatasetId: (id: string) => void;
  maxLayers: number;
  loadDatasets: () => void;
  addLayer: () => void;
  addFromDataset: () => void;
}

export function LayersPanel({
  style,
  layers,
  setLayers,
  sourceById,
  availableSources,
  datasets,
  pickerSourceId,
  setPickerSourceId,
  pickerDatasetId,
  setPickerDatasetId,
  maxLayers,
  loadDatasets,
  addLayer,
  addFromDataset,
}: Props) {
  function patchLayer(idx: number, patch: Partial<MapLayer>) {
    setLayers(layers.map((l, i) => (i === idx ? { ...l, ...patch } : l)));
  }

  return (
    <div
      style={style}
      className="w-80 max-h-[60vh] overflow-y-auto bg-surface/95 backdrop-blur rounded-lg shadow-lg border border-border p-3 space-y-2"
      data-testid="layers-panel"
    >
      <div className="font-medium text-sm">Layers ({layers.length})</div>
      {layers.length === 0 && (
        <div className="text-xs text-text-muted">No layers yet. Add one below.</div>
      )}
      {layers.map((layer, idx) => {
        const src = sourceById.get(layer.layerSourceId);
        return (
          <div
            // biome-ignore lint/suspicious/noArrayIndexKey: layer order is the stable identity here
            key={`${layer.layerSourceId}-${idx}`}
            className="border border-border rounded p-2 space-y-1"
          >
            <div className="flex items-center justify-between">
              <div className="font-medium text-sm truncate min-w-0">
                {src?.name ?? 'Unknown source'}
              </div>
              <div className="flex gap-1 shrink-0">
                <Button
                  size="sm"
                  variant="secondary"
                  onClick={() => setLayers(reorder(layers, idx, -1))}
                >
                  ↑
                </Button>
                <Button
                  size="sm"
                  variant="secondary"
                  onClick={() => setLayers(reorder(layers, idx, 1))}
                >
                  ↓
                </Button>
                <Button size="sm" variant="danger" onClick={() => setLayers(removeAt(layers, idx))}>
                  ×
                </Button>
              </div>
            </div>
            <div className="flex items-center gap-2 text-xs">
              <Switch
                checked={layer.visible}
                onCheckedChange={(v) => patchLayer(idx, { visible: v })}
              />
              <span>Visible</span>
              <Label htmlFor={`opacity-${idx}`} className="ml-auto">
                Opacity
              </Label>
              <Input
                id={`opacity-${idx}`}
                type="number"
                min={0}
                max={1}
                step={0.05}
                value={layer.opacity}
                onChange={(e) => patchLayer(idx, { opacity: Number(e.currentTarget.value) })}
                className="w-16"
              />
            </div>
          </div>
        );
      })}
      <div className="flex gap-2 pt-1">
        <Select value={pickerSourceId} onValueChange={setPickerSourceId}>
          <SelectTrigger className="flex-1">
            <SelectValue placeholder="Add source…" />
          </SelectTrigger>
          <SelectContent>
            {availableSources.map((s) => (
              <SelectItem key={s.id} value={s.id}>
                {s.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Button
          size="sm"
          onClick={addLayer}
          disabled={!pickerSourceId || layers.length >= maxLayers}
        >
          Add
        </Button>
      </div>
      <div className="flex gap-2">
        <Select
          value={pickerDatasetId}
          onValueChange={setPickerDatasetId}
          onOpenChange={(open) => {
            if (open) loadDatasets();
          }}
        >
          <SelectTrigger className="flex-1">
            <SelectValue placeholder="Add from dataset…" />
          </SelectTrigger>
          <SelectContent>
            {datasets.map((d) => (
              <SelectItem key={d.id} value={d.id}>
                {d.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Button
          size="sm"
          onClick={addFromDataset}
          disabled={!pickerDatasetId || layers.length >= maxLayers}
        >
          Add
        </Button>
      </div>
    </div>
  );
}

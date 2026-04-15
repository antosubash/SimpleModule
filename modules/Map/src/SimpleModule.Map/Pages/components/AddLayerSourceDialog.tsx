import {
  Button,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Field,
  FieldGroup,
  Input,
  Label,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Textarea,
} from '@simplemodule/ui';
import type { CreateLayerSourceRequest } from '@/types';
import { LayerSourceType } from '../lib/layer-builders';

export const TYPE_LABELS: Record<number, string> = {
  [LayerSourceType.Wms]: 'WMS',
  [LayerSourceType.Wmts]: 'WMTS',
  [LayerSourceType.Wfs]: 'WFS',
  [LayerSourceType.Xyz]: 'XYZ tiles',
  [LayerSourceType.VectorTile]: 'Vector tiles',
  [LayerSourceType.PmTiles]: 'PMTiles',
  [LayerSourceType.Cog]: 'COG (cloud-optimized GeoTIFF)',
  [LayerSourceType.GeoJson]: 'GeoJSON',
  [LayerSourceType.Dataset]: 'Dataset',
};

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  form: CreateLayerSourceRequest;
  setForm: (form: CreateLayerSourceRequest) => void;
  metaText: string;
  setMetaText: (v: string) => void;
  submitting: boolean;
  error: string | null;
  onSubmit: (e: React.FormEvent<HTMLFormElement>) => void;
}

export function AddLayerSourceDialog({
  open,
  onOpenChange,
  form,
  setForm,
  metaText,
  setMetaText,
  submitting,
  error,
  onSubmit,
}: Props) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Add layer source</DialogTitle>
        </DialogHeader>
        <form onSubmit={onSubmit}>
          <FieldGroup>
            <Field>
              <Label htmlFor="name">Name</Label>
              <Input
                id="name"
                required
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.currentTarget.value })}
              />
            </Field>
            <Field>
              <Label htmlFor="type">Type</Label>
              <Select
                value={String(form.type)}
                onValueChange={(v) => setForm({ ...form, type: Number(v) as never })}
              >
                <SelectTrigger id="type">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {Object.entries(TYPE_LABELS).map(([value, label]) => (
                    <SelectItem key={value} value={value}>
                      {label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </Field>
            <Field>
              <Label htmlFor="url">URL</Label>
              <Input
                id="url"
                required
                value={form.url}
                onChange={(e) => setForm({ ...form, url: e.currentTarget.value })}
                placeholder="https://example.com/wms"
              />
            </Field>
            <Field>
              <Label htmlFor="attribution">Attribution</Label>
              <Input
                id="attribution"
                value={form.attribution ?? ''}
                onChange={(e) => setForm({ ...form, attribution: e.currentTarget.value })}
              />
            </Field>
            <Field>
              <Label htmlFor="metadata">Metadata (JSON)</Label>
              <Textarea
                id="metadata"
                rows={4}
                value={metaText}
                onChange={(e) => setMetaText(e.currentTarget.value)}
                placeholder='{"layers":"OSM-WMS","format":"image/png","crs":"EPSG:3857"}'
              />
            </Field>
            {error && <div className="text-sm text-danger">{error}</div>}
          </FieldGroup>
          <DialogFooter className="mt-4">
            <Button variant="secondary" type="button" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={submitting}>
              {submitting ? 'Saving…' : 'Save'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

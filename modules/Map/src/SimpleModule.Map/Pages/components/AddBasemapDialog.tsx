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
} from '@simplemodule/ui';
import type { CreateBasemapRequest } from '@/types';

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  form: CreateBasemapRequest;
  setForm: (form: CreateBasemapRequest) => void;
  submitting: boolean;
  error: string | null;
  onSubmit: (e: React.FormEvent<HTMLFormElement>) => void;
}

export function AddBasemapDialog({
  open,
  onOpenChange,
  form,
  setForm,
  submitting,
  error,
  onSubmit,
}: Props) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Add basemap</DialogTitle>
        </DialogHeader>
        <form onSubmit={onSubmit}>
          <FieldGroup>
            <Field>
              <Label htmlFor="bm-name">Name</Label>
              <Input
                id="bm-name"
                required
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.currentTarget.value })}
              />
            </Field>
            <Field>
              <Label htmlFor="bm-style">MapLibre style URL</Label>
              <Input
                id="bm-style"
                required
                value={form.styleUrl}
                onChange={(e) => setForm({ ...form, styleUrl: e.currentTarget.value })}
                placeholder="https://demotiles.maplibre.org/style.json"
              />
            </Field>
            <Field>
              <Label htmlFor="bm-attr">Attribution</Label>
              <Input
                id="bm-attr"
                value={form.attribution ?? ''}
                onChange={(e) => setForm({ ...form, attribution: e.currentTarget.value })}
              />
            </Field>
            <Field>
              <Label htmlFor="bm-thumb">Thumbnail URL</Label>
              <Input
                id="bm-thumb"
                value={form.thumbnailUrl ?? ''}
                onChange={(e) => setForm({ ...form, thumbnailUrl: e.currentTarget.value })}
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

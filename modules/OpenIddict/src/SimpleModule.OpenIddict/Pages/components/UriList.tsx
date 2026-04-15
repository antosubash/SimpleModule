import { useTranslation } from '@simplemodule/client/use-translation';
import { Button, Field, Input, Label } from '@simplemodule/ui';
import { useState } from 'react';
import { OpenIddictKeys } from '@/Locales/keys';

export function UriList({
  label,
  name,
  values,
}: {
  label: string;
  name: string;
  values: string[];
}) {
  const { t } = useTranslation('OpenIddict');
  const [uris, setUris] = useState(values.length > 0 ? values : ['']);

  function addUri() {
    setUris([...uris, '']);
  }

  function removeUri(index: number) {
    setUris(uris.filter((_, i) => i !== index));
  }

  function updateUri(index: number, value: string) {
    const updated = [...uris];
    updated[index] = value;
    setUris(updated);
  }

  return (
    <Field>
      <Label>{label}</Label>
      <div className="space-y-2">
        {uris.map((uri, index) => (
          // biome-ignore lint/suspicious/noArrayIndexKey: URIs can be duplicated, no stable ID
          <div key={index} className="flex gap-2">
            <Input
              name={name}
              value={uri}
              onChange={(e) => updateUri(index, e.target.value)}
              placeholder={t(OpenIddictKeys.ClientsEdit.UriPlaceholder)}
            />
            <Button type="button" variant="danger" size="sm" onClick={() => removeUri(index)}>
              {t(OpenIddictKeys.ClientsEdit.UriRemoveButton)}
            </Button>
          </div>
        ))}
      </div>
      <Button type="button" variant="ghost" size="sm" className="mt-2" onClick={addUri}>
        {t(OpenIddictKeys.ClientsEdit.UriAddButton)}
      </Button>
    </Field>
  );
}

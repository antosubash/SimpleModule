import type React from 'react';
import type { SettingDefinition } from './fields';
import {
  BoolField,
  ColorField,
  DateTimeField,
  EmailField,
  JsonField,
  MultilineTextField,
  NumberField,
  PasswordField,
  SelectField,
  TextField,
  UrlField,
} from './fields';

export interface SettingFieldProps {
  definition: SettingDefinition;
  value: unknown;
  onSave: (value: unknown) => Promise<void>;
  onDirty?: (isDirty: boolean) => void;
  disabled?: boolean;
  autoFocus?: boolean;
}

const fieldMap = {
  0: TextField,
  1: NumberField,
  2: BoolField,
  3: JsonField,
  4: SelectField,
  5: ColorField,
  6: UrlField,
  7: EmailField,
  8: PasswordField,
  9: MultilineTextField,
  10: DateTimeField,
} as const;

export default function SettingField(props: SettingFieldProps): React.JSX.Element {
  const Field = fieldMap[props.definition.type] ?? TextField;
  return (
    <div className="space-y-2">
      {props.definition.description && (
        <p className="text-sm text-muted-foreground">{props.definition.description}</p>
      )}
      <Field {...props} />
    </div>
  );
}

export type { SettingDefinition };

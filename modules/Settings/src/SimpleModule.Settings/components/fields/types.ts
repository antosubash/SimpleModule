export type SettingType =
  | 0 // Text
  | 1 // Number
  | 2 // Bool
  | 3 // Json
  | 4 // Select
  | 5 // Color
  | 6 // Url
  | 7 // Email
  | 8 // Password
  | 9 // MultilineText
  | 10; // DateTime

export type SettingScope = 0 /* System */ | 1 /* Application */ | 2 /* User */;

export interface SettingDefinition {
  key: string;
  displayName: string;
  description?: string;
  group?: string;
  scope: SettingScope;
  defaultValue?: string;
  type: SettingType;
  allowedValues?: string[];
  min?: number;
  max?: number;
  pattern?: string;
  required: boolean;
  sensitive: boolean;
  order: number;
  placeholder?: string;
}

export interface FieldProps {
  definition: SettingDefinition;
  value: unknown;
  onSave: (value: unknown) => Promise<void>;
  onDirty?: (isDirty: boolean) => void;
  disabled?: boolean;
  autoFocus?: boolean;
}

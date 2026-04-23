---
outline: deep
---

# Localization

SimpleModule includes a built-in localization system that provides multi-language support across both the .NET backend and the React frontend. Translations are stored as embedded JSON resources in each module and automatically discovered at startup.

## How It Works

1. Each module embeds locale JSON files (e.g., `en.json`, `es.json`) as assembly resources
2. At startup, the framework scans all module assemblies and loads translations into frozen (immutable) dictionaries
3. On each request, middleware resolves the user's preferred locale
4. Translations are injected into Inertia shared props, making them available to all React components

## Adding Translations to a Module

### 1. Create Locale Files

Add a `Locales/` directory to your module with JSON files named by locale code:

```
modules/Products/src/Products/
├── Locales/
│   ├── en.json
│   └── es.json
```

Translation keys use dot notation:

```json
// Locales/en.json
{
  "Browse.Title": "Products",
  "Browse.Description": "Browse the product catalog.",
  "Manage.DeleteConfirm": "Are you sure you want to delete \"{name}\"?"
}
```

```json
// Locales/es.json
{
  "Browse.Title": "Productos",
  "Browse.Description": "Navegar el catálogo de productos.",
  "Manage.DeleteConfirm": "¿Estás seguro de que deseas eliminar \"{name}\"?"
}
```

### 2. Embed as Resources

In your module's `.csproj`, mark the locale files as embedded resources:

```xml
<ItemGroup>
  <EmbeddedResource Include="Locales/*.json" />
</ItemGroup>
```

### 3. Use Translations in React

Import the `useTranslation` hook from `@simplemodule/client`:

```tsx
import { useTranslation } from '@simplemodule/client';

export default function Browse({ products }) {
  const { t, locale } = useTranslation('Products');

  return (
    <div>
      <h1>{t('Browse.Title')}</h1>
      <p>{t('Browse.Description')}</p>
    </div>
  );
}
```

The hook accepts a namespace (your module name) and returns:
- **`t(key, params?)`** -- translates a key, with optional parameter interpolation
- **`locale`** -- the current locale string (e.g., `"en"`, `"es"`)

### Parameter Interpolation

Use `{paramName}` placeholders in translation values:

```tsx
// Translation: "Are you sure you want to delete \"{name}\"?"
t('Manage.DeleteConfirm', { name: product.name })
```

### 4. Type-Safe Keys (Optional)

Create a `keys.ts` file for compile-time key safety:

```typescript
// Locales/keys.ts
export const ProductsKeys = {
  Browse: {
    Title: 'Browse.Title',
    Description: 'Browse.Description',
  },
  Manage: {
    DeleteConfirm: 'Manage.DeleteConfirm',
  },
} as const;
```

Then use it in components:

```tsx
import { ProductsKeys } from '../Locales/keys';

const { t } = useTranslation('Products');
t(ProductsKeys.Browse.Title);
```

## Locale Resolution

The `LocaleResolutionMiddleware` determines the user's locale using this priority order:

1. **User setting** -- the `app.language` setting stored in the database (cached for 5 minutes)
2. **Accept-Language header** -- parsed with quality values (cached for 30 minutes)
3. **Configuration default** -- `Localization:DefaultLocale` from `appsettings.json`
4. **Hardcoded fallback** -- `"en"`

::: tip Claim lookup
The middleware reads the user ID from `ClaimTypes.NameIdentifier` only. If your auth pipeline issues just the OpenIddict `sub` claim without also mapping it to `NameIdentifier`, the user-setting lookup is skipped and locale resolution falls through to Accept-Language / the configured default.
:::

## Backend Usage

The localization system integrates with .NET's `IStringLocalizer`:

```csharp
public class MyService(IStringLocalizer<MyService> localizer)
{
    public string GetWelcome(string name) =>
        localizer["welcome", name];
}
```

## Fallback Behavior

- If a key is missing for the requested locale, the system falls back to English (`"en"`)
- If the key is missing in English too, behavior depends on where the lookup runs:
  - **React (`useTranslation`)** returns the **un-prefixed** key you passed in (e.g. `t('Browse.Title')` returns `"Browse.Title"`, not `"Products.Browse.Title"`)
  - **.NET (`TranslationLoader.GetTranslation`)** returns `null`. Code paths that go through `IStringLocalizer` may wrap that into a `LocalizedString` whose `Name` is the key and whose `ResourceNotFound` is `true`

## Configuration

```json
{
  "Localization": {
    "DefaultLocale": "en"
  }
}
```

## Contract Interface

Other modules can access translations programmatically through `ILocalizationContracts`:

```csharp
public interface ILocalizationContracts
{
    string? GetTranslation(string key, string locale);
    IReadOnlyDictionary<string, string> GetAllTranslations(string locale);
    IReadOnlyList<string> GetSupportedLocales();
}
```

## Next Steps

- [Settings](/guide/settings) -- user-scoped settings that store language preferences
- [Inertia.js Integration](/guide/inertia) -- how shared props deliver translations to React
- [Modules](/guide/modules) -- module structure and embedded resources

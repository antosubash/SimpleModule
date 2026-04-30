import { usePage } from '@inertiajs/react';
import { useMemo } from 'react';

interface TranslationResult {
  t: (key: string, params?: Record<string, string>) => string;
  locale: string;
}

interface SharedProps {
  locale?: string;
  translations?: Record<string, string>;
  [key: string]: unknown;
}

/**
 * Hook for accessing localized translations within a module namespace.
 *
 * @param namespace - The module namespace (e.g., 'Products'). Keys are
 *   automatically prefixed with this namespace when looking up translations.
 *
 * @example
 * ```tsx
 * import { ProductsKeys } from '../Locales/keys';
 *
 * const { t, locale } = useTranslation('Products');
 * <h1>{t(ProductsKeys.Browse.Title)}</h1>
 * <p>{t(ProductsKeys.Manage.DeleteConfirm, { name: product.name })}</p>
 * ```
 */
const warnedNamespaces = new Set<string>();

export function useTranslation(namespace: string): TranslationResult {
  const { props } = usePage<SharedProps>();
  const locale = props.locale ?? 'en';
  const translations = props.translations ?? {};

  // If translations were never populated, the SimpleModule.Localization module
  // is probably not installed (its middleware is what writes props.translations).
  // Without it, every t(...) call falls back to the bare key and the UI renders
  // raw identifiers like "Users.Title". Warn loudly in dev once per namespace.
  if (
    process.env.NODE_ENV !== 'production' &&
    props.translations === undefined &&
    !warnedNamespaces.has(namespace)
  ) {
    warnedNamespaces.add(namespace);
    console.warn(
      `[useTranslation] Module "${namespace}" is asking for translations but ` +
        'Inertia shared data has no "translations" key. Is SimpleModule.Localization installed? ' +
        'Without it, translation keys will render as-is.',
    );
  }

  const t = useMemo(() => {
    const prefix = `${namespace}.`;

    return (key: string, params?: Record<string, string>): string => {
      const fullKey = `${prefix}${key}`;
      let value = translations[fullKey];

      if (value === undefined) {
        return key;
      }

      if (params) {
        for (const [paramKey, paramValue] of Object.entries(params)) {
          value = value.replaceAll(`{${paramKey}}`, paramValue);
        }
      }

      return value;
    };
  }, [namespace, translations]);

  return useMemo(() => ({ t, locale }), [t, locale]);
}

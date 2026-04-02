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
export function useTranslation(namespace: string): TranslationResult {
  const { props } = usePage<SharedProps>();
  const locale = props.locale ?? 'en';
  const translations = props.translations ?? {};

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

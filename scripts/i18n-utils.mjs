// Shared utilities for i18n tooling scripts.

import { readdirSync, existsSync, statSync } from 'fs';
import { join } from 'path';

/**
 * Find all module Locales directories, skipping *.Contracts projects.
 * Returns [{ moduleName, localesDir, project }].
 */
export function findModuleLocales(modulesDir) {
  const results = [];
  if (!existsSync(modulesDir)) return results;

  for (const moduleDir of readdirSync(modulesDir)) {
    const srcDir = join(modulesDir, moduleDir, 'src');
    if (!existsSync(srcDir) || !statSync(srcDir).isDirectory()) continue;

    for (const project of readdirSync(srcDir)) {
      if (project.endsWith('.Contracts')) continue;

      const localesDir = join(srcDir, project, 'Locales');
      if (existsSync(localesDir) && statSync(localesDir).isDirectory()) {
        results.push({ moduleName: moduleDir, localesDir, project });
      }
    }
  }

  return results;
}

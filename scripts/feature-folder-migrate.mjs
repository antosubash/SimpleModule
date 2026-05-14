// scripts/feature-folder-migrate.mjs
import { posix as path } from 'node:path';

/**
 * Compute the C# namespace for a file based on its position relative to the
 * project root, using the folder-equals-namespace convention.
 *
 * @param {object} args
 * @param {string} args.assemblyName  e.g. "SimpleModule.Notifications"
 * @param {string} args.projectRoot   path to the .csproj directory (forward-slash)
 * @param {string} args.filePath      path to the .cs file (forward-slash)
 * @returns {string} the dotted namespace, e.g. "SimpleModule.Notifications.Features.Notifications.List"
 */
export function deriveNamespace({ assemblyName, projectRoot, filePath }) {
  const normalizedRoot = path.normalize(projectRoot).replace(/\/+$/, '') + '/';
  const normalizedFile = path.normalize(filePath);
  if (!normalizedFile.startsWith(normalizedRoot)) {
    throw new Error(
      `File ${filePath} is not under project root ${projectRoot}`,
    );
  }
  const relative = normalizedFile.slice(normalizedRoot.length);
  const dir = path.dirname(relative);
  if (dir === '.' || dir === '') {
    return assemblyName;
  }
  const segments = dir.split('/').filter((s) => s.length > 0);
  return [assemblyName, ...segments].join('.');
}

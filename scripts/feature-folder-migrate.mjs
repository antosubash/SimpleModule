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

import { execFileSync } from 'node:child_process';
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const FILE_SCOPED_NS = /^namespace\s+([A-Za-z_][\w.]*)\s*;/m;
const BLOCK_SCOPED_NS = /^namespace\s+[A-Za-z_][\w.]*\s*\{/m;

/**
 * Replace the file-scoped namespace declaration in a .cs source string.
 * Throws if the file uses a block-scoped namespace or has none at all.
 */
export function rewriteNamespace(source, newNamespace) {
  const match = source.match(FILE_SCOPED_NS);
  if (!match) {
    if (BLOCK_SCOPED_NS.test(source)) {
      throw new Error(
        'rewriteNamespace requires a file-scoped namespace declaration (got block-scoped)',
      );
    }
    throw new Error('rewriteNamespace: no file-scoped namespace declaration found');
  }
  if (match[1] === newNamespace) {
    return source;
  }
  return source.replace(FILE_SCOPED_NS, `namespace ${newNamespace};`);
}

/**
 * Parse a TSV manifest. Returns an array of move directives.
 */
export function parseManifest(text) {
  return text
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter((line) => line.length > 0 && !line.startsWith('#'))
    .map((line, idx) => {
      const cols = line.split('\t');
      if (cols.length !== 3 && cols.length !== 4) {
        throw new Error(
          `line ${idx + 1}: expected 3 or 4 tab-separated columns, got ${cols.length}`,
        );
      }
      return {
        oldPath: cols[0],
        newPath: cols[1],
        assemblyName: cols[2],
        projectRoot: cols[3],
      };
    });
}

/**
 * Infer the .csproj directory by walking up from oldPath until we find a folder
 * named exactly assemblyName.
 */
function inferProjectRoot(oldPath, assemblyName) {
  const parts = path.normalize(oldPath).split('/');
  for (let i = parts.length - 1; i > 0; i -= 1) {
    if (parts[i] === assemblyName) {
      return parts.slice(0, i + 1).join('/');
    }
  }
  throw new Error(
    `cannot infer projectRoot for ${oldPath}; no path segment matches assembly ${assemblyName}`,
  );
}

function applyMove(directive) {
  const projectRoot =
    directive.projectRoot ?? inferProjectRoot(directive.oldPath, directive.assemblyName);
  const newDir = path.dirname(directive.newPath);
  mkdirSync(newDir, { recursive: true });
  execFileSync('git', ['mv', directive.oldPath, directive.newPath], { stdio: 'inherit' });

  if (directive.newPath.endsWith('.cs')) {
    const newNamespace = deriveNamespace({
      assemblyName: directive.assemblyName,
      projectRoot,
      filePath: directive.newPath,
    });
    const source = readFileSync(directive.newPath, 'utf8');
    const rewritten = rewriteNamespace(source, newNamespace);
    if (rewritten !== source) {
      writeFileSync(directive.newPath, rewritten);
      execFileSync('git', ['add', directive.newPath], { stdio: 'inherit' });
    }
  }
}

function main(argv) {
  const manifestPath = argv[2];
  if (!manifestPath) {
    console.error('Usage: node scripts/feature-folder-migrate.mjs <manifest.tsv>');
    process.exit(2);
  }
  const text = readFileSync(manifestPath, 'utf8');
  const directives = parseManifest(text);
  for (const d of directives) {
    applyMove(d);
  }
  console.log(`Applied ${directives.length} move(s) from ${manifestPath}`);
}

const isCli = process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1];
if (isCli) {
  main(process.argv);
}

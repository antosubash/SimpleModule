import { spawn } from 'child_process';
import path from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';
import { createLogger, discoverBuildableWorkspaces } from './orchestrator-utils.mjs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const rootDir = path.resolve(__dirname, '..');

const mode = process.env.VITE_MODE || 'dev';
const log = createLogger();

function runBuild(workspacePath, workspaceName) {
  return new Promise((resolve, reject) => {
    log('build', `Building ${workspaceName}...`);
    const env = { ...process.env, VITE_MODE: mode };
    const buildScript = mode === 'prod' ? 'build' : 'build:dev';

    const child = spawn('npm', ['run', buildScript], {
      cwd: path.resolve(rootDir, workspacePath),
      stdio: 'inherit',
      shell: true,
      env,
    });

    child.on('error', (err) => {
      log(workspaceName, `Error: ${err.message}`);
      reject(err);
    });

    child.on('close', (code) => {
      if (code === 0) {
        log(workspaceName, 'Build completed successfully');
        resolve();
      } else {
        reject(new Error(`Build failed for ${workspaceName} with code ${code}`));
      }
    });
  });
}

async function runWithConcurrency(items, concurrency, fn) {
  const results = [];
  let index = 0;

  async function worker() {
    while (index < items.length) {
      const i = index++;
      results[i] = await fn(items[i]);
    }
  }

  await Promise.all(Array.from({ length: Math.min(concurrency, items.length) }, () => worker()));
  return results;
}

async function main() {
  try {
    log('startup', `Starting production build (VITE_MODE=${mode})...`);

    const workspaces = discoverBuildableWorkspaces(rootDir);
    log('startup', `Found ${workspaces.length} workspaces to build`);

    // Limit concurrency to avoid OOM in memory-constrained environments (e.g. Docker)
    const concurrency = parseInt(process.env.BUILD_CONCURRENCY, 10) || 4;
    await runWithConcurrency(workspaces, concurrency, (workspace) => {
      const workspaceName = path.basename(workspace);
      return runBuild(workspace, workspaceName);
    });

    log('complete', 'All workspaces built successfully!');
    process.exit(0);
  } catch (err) {
    log('error', `Build failed: ${err.message}`);
    process.exit(1);
  }
}

main();

import { spawn } from 'child_process';
import path from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';
import { readdirSync, existsSync, readFileSync } from 'fs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const rootDir = path.resolve(__dirname, '..');

const mode = process.env.VITE_MODE || 'dev';

function log(prefix, message) {
  console.log(`\x1b[36m[${prefix}]\x1b[0m ${message}`);
}

// Dynamically discover all buildable workspaces (those with build scripts)
function discoverWorkspaces() {
  const workspaces = [];

  // Discover module workspaces
  const modulesDir = path.resolve(rootDir, 'modules');
  const modules = readdirSync(modulesDir);
  for (const moduleName of modules) {
    const srcDir = path.resolve(modulesDir, moduleName, 'src');
    if (existsSync(srcDir)) {
      const moduleWorkspaces = readdirSync(srcDir);
      for (const workspace of moduleWorkspaces) {
        const pkgPath = path.resolve(srcDir, workspace, 'package.json');
        if (existsSync(pkgPath)) {
          // Only add if it has a build script
          const pkgContent = JSON.parse(readFileSync(pkgPath, 'utf8'));
          if (pkgContent.scripts && pkgContent.scripts.build) {
            workspaces.push(`modules/${moduleName}/src/${workspace}`);
          }
        }
      }
    }
  }

  // Add ClientApp (it definitely has build script)
  const clientAppPath = path.resolve(rootDir, 'template/SimpleModule.Host/ClientApp/package.json');
  if (existsSync(clientAppPath)) {
    workspaces.push('template/SimpleModule.Host/ClientApp');
  }

  return workspaces;
}

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

async function main() {
  try {
    log('startup', `Starting production build (VITE_MODE=${mode})...`);

    const workspaces = discoverWorkspaces();
    log('startup', `Found ${workspaces.length} workspaces to build`);

    // Build all workspaces sequentially
    for (const workspace of workspaces) {
      const workspaceName = path.basename(workspace);
      await runBuild(workspace, workspaceName);
    }

    log('complete', 'All workspaces built successfully!');
    process.exit(0);
  } catch (err) {
    log('error', `Build failed: ${err.message}`);
    process.exit(1);
  }
}

main();

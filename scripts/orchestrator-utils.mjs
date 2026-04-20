/**
 * Shared utilities for dev and build orchestrators
 */
import { spawn } from 'child_process';
import path from 'path';
import { readdirSync, existsSync, readFileSync } from 'fs';

/**
 * Create a colored console logger with prefix formatting
 */
export function createLogger() {
  return (prefix, message) => {
    console.log(`\x1b[36m[${prefix}]\x1b[0m ${message}`);
  };
}

/**
 * Spawn a child process with consistent error handling and logging
 */
export function spawnProcess(command, args, options, logPrefix, log) {
  return new Promise((resolve, reject) => {
    const proc = spawn(command, args, options);

    proc.on('error', (err) => {
      log(logPrefix, `Error: ${err.message}`);
      reject(err);
    });

    proc.on('close', (code) => {
      if (code === 0) {
        resolve(proc);
      } else {
        reject(new Error(`Process failed with exit code ${code}`));
      }
    });

    return proc;
  });
}

/**
 * Discover modules with frontend builds (have vite.config.ts and package.json)
 */
export function discoverModulesWithVite(rootDir) {
  const modulesDir = path.resolve(rootDir, 'modules');
  const modules = [];

  if (!existsSync(modulesDir)) return modules;

  const moduleGroups = readdirSync(modulesDir);
  for (const group of moduleGroups) {
    const srcDir = path.resolve(modulesDir, group, 'src');
    if (!existsSync(srcDir)) continue;

    const moduleDirs = readdirSync(srcDir);
    for (const moduleName of moduleDirs) {
      const modulePath = path.join(srcDir, moduleName);
      const packagePath = path.join(modulePath, 'package.json');
      const vitePath = path.join(modulePath, 'vite.config.ts');

      if (existsSync(packagePath) && existsSync(vitePath)) {
        const relPath = path.relative(rootDir, modulePath);
        modules.push(relPath.split(path.sep).join('/'));
      }
    }
  }

  return modules.sort();
}

/**
 * Discover buildable workspaces (those with build scripts in package.json)
 */
export function discoverBuildableWorkspaces(rootDir) {
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

import { spawn } from 'child_process';
import path from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';
import fs from 'fs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const rootDir = path.resolve(__dirname, '..');

function discoverModules() {
  const modulesDir = path.resolve(rootDir, 'modules');
  const modules = [];

  if (!fs.existsSync(modulesDir)) return modules;

  // Scan modules directory
  const moduleGroups = fs.readdirSync(modulesDir);

  for (const group of moduleGroups) {
    const srcDir = path.resolve(modulesDir, group, 'src');
    if (!fs.existsSync(srcDir)) continue;

    const moduleDirs = fs.readdirSync(srcDir);
    for (const moduleName of moduleDirs) {
      const modulePath = path.join(srcDir, moduleName);
      const packagePath = path.join(modulePath, 'package.json');
      const vitePath = path.join(modulePath, 'vite.config.ts');

      // Include if has package.json AND vite.config.ts
      if (fs.existsSync(packagePath) && fs.existsSync(vitePath)) {
        const relPath = path.relative(rootDir, modulePath);
        modules.push(relPath.split(path.sep).join('/'));
      }
    }
  }

  return modules.sort();
}

// Auto-discover all module workspace paths
const modules = discoverModules();

const childProcesses = [];

function log(prefix, message) {
  console.log(`\x1b[36m[${prefix}]\x1b[0m ${message}`);
}

function startDotnetRun() {
  log('setup', 'Starting dotnet run...');
  const proc = spawn('dotnet', ['run', '--project', 'template/SimpleModule.Host'], {
    cwd: rootDir,
    stdio: 'inherit',
    shell: true,
  });

  proc.on('error', (err) => {
    log('dotnet', `Error: ${err.message}`);
  });

  proc.on('exit', (code) => {
    log('dotnet', `Exited with code ${code}`);
  });

  childProcesses.push(proc);
  return proc;
}

function startModuleWatch(modulePath) {
  const moduleName = path.basename(path.dirname(modulePath));
  log('setup', `Starting watch for ${moduleName}...`);

  const proc = spawn('npm', ['run', 'watch'], {
    cwd: path.resolve(rootDir, modulePath),
    stdio: 'inherit',
    shell: true,
  });

  proc.on('error', (err) => {
    log(moduleName, `Error: ${err.message}`);
  });

  proc.on('exit', (code) => {
    if (code !== null) {
      log(moduleName, `Watch exited with code ${code}`);
    }
  });

  childProcesses.push(proc);
  return proc;
}

function startClientAppWatch() {
  log('setup', 'Starting ClientApp watch...');
  const proc = spawn('npm', ['run', 'watch'], {
    cwd: path.resolve(rootDir, 'template/SimpleModule.Host/ClientApp'),
    stdio: 'inherit',
    shell: true,
  });

  proc.on('error', (err) => {
    log('ClientApp', `Error: ${err.message}`);
  });

  proc.on('exit', (code) => {
    if (code !== null) {
      log('ClientApp', `Watch exited with code ${code}`);
    }
  });

  childProcesses.push(proc);
  return proc;
}

function shutdown() {
  log('shutdown', 'Stopping all processes...');
  childProcesses.forEach((proc) => {
    try {
      proc.kill('SIGTERM');
    } catch (err) {
      // Process already exited
    }
  });
  setTimeout(() => process.exit(0), 500);
}

// Allow syntax check
if (process.argv.includes('--check')) {
  log('check', 'Syntax valid');
  log('check', `Discovered ${modules.length} modules: ${modules.join(', ')}`);
  process.exit(0);
}

// Handle signals
process.on('SIGINT', shutdown);
process.on('SIGTERM', shutdown);

// Start all processes
log('startup', 'Starting development environment...');
startDotnetRun();
startClientAppWatch();
modules.forEach((modulePath) => startModuleWatch(modulePath));

log('startup', `All processes started. Press Ctrl+C to stop.`);

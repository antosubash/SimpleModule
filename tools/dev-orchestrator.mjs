import { spawn } from 'child_process';
import path from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';
import { createLogger, discoverModulesWithVite } from './orchestrator-utils.mjs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const rootDir = path.resolve(__dirname, '..');

// Auto-discover all module workspace paths
const modules = discoverModulesWithVite(rootDir);

const childProcesses = [];
const log = createLogger();

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
      log('shutdown', `Warning: Failed to terminate process: ${err.message}`);
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

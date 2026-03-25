import { spawn } from 'child_process';
import path from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';

const __dirname = dirname(fileURLToPath(import.meta.url));
const rootDir = path.resolve(__dirname, '..');

// Get all module workspace paths
const modules = [
  'modules/Admin/src/Admin',
  'modules/AuditLogs/src/AuditLogs',
  'modules/Dashboard/src/Dashboard',
  'modules/OpenIddict/src/OpenIddict',
  'modules/Permissions/src/Permissions',
  'modules/Products/src/Products',
  'modules/Settings/src/Settings',
  'modules/Users/src/Users',
  'modules/PageBuilder/src/PageBuilder',
];

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

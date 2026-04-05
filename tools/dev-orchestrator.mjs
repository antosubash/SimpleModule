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
let shuttingDown = false;

function startDotnetRun() {
  log('setup', 'Starting dotnet run...');
  const proc = spawn('dotnet', ['run', '--no-restore', '--project', 'template/SimpleModule.Host'], {
    cwd: rootDir,
    stdio: 'inherit',
    shell: true,
  });

  proc.on('error', (err) => {
    log('dotnet', `Critical error: ${err.message}`);
    log('error', 'Failed to start dotnet backend. Shutting down.');
    shutdown(1);
  });

  proc.on('exit', (code) => {
    if (code !== 0 && !shuttingDown) {
      log('dotnet', `Exited with code ${code}`);
      log('error', 'Backend process terminated unexpectedly. Shutting down.');
      shutdown(1);
    }
  });

  childProcesses.push({ proc, label: 'dotnet' });
  log('setup', `dotnet started (PID ${proc.pid})`);
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
    // Module watch failure is non-fatal; continue running other watches
  });

  proc.on('exit', (code) => {
    if (code !== 0 && code !== null && !shuttingDown) {
      log(moduleName, `Watch exited with non-zero code ${code}`);
    }
  });

  childProcesses.push({ proc, label: moduleName });
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
    // ClientApp watch failure is non-fatal; continue running backend and other modules
  });

  proc.on('exit', (code) => {
    if (code !== 0 && code !== null && !shuttingDown) {
      log('ClientApp', `Watch exited with non-zero code ${code}`);
    }
  });

  childProcesses.push({ proc, label: 'ClientApp' });
  return proc;
}

function killProcess(proc, label) {
  try {
    if (proc.exitCode !== null) return; // already exited

    if (process.platform === 'win32') {
      // Windows: use taskkill to kill the entire process tree
      spawn('taskkill', ['/PID', String(proc.pid), '/T', '/F'], {
        stdio: 'ignore',
      });
    } else {
      // Unix: send SIGTERM to the process group (negative PID kills the group)
      // This ensures shell-spawned children (node, vite, dotnet) also receive the signal
      try {
        process.kill(-proc.pid, 'SIGTERM');
      } catch {
        // Process group may not exist; fall back to direct signal
        proc.kill('SIGTERM');
      }
    }
  } catch (err) {
    log(label, `Warning: Failed to terminate (PID ${proc.pid}): ${err.message}`);
  }
}

function forceKillAll() {
  for (const { proc, label } of childProcesses) {
    try {
      if (proc.exitCode === null) {
        if (process.platform === 'win32') {
          spawn('taskkill', ['/PID', String(proc.pid), '/T', '/F'], {
            stdio: 'ignore',
          });
        } else {
          try {
            process.kill(-proc.pid, 'SIGKILL');
          } catch {
            proc.kill('SIGKILL');
          }
        }
      }
    } catch (err) {
      log(label, `Warning: Force-kill failed (PID ${proc.pid}): ${err.message}`);
    }
  }
}

function shutdown(exitCode = 0) {
  if (shuttingDown) return;
  shuttingDown = true;

  log('shutdown', 'Stopping all processes...');

  // Phase 1: Graceful termination (SIGTERM)
  for (const { proc, label } of childProcesses) {
    killProcess(proc, label);
  }

  // Phase 2: Wait briefly, then force-kill survivors
  setTimeout(() => {
    const survivors = childProcesses.filter(({ proc }) => proc.exitCode === null);
    if (survivors.length > 0) {
      log('shutdown', `Force-killing ${survivors.length} remaining process(es)...`);
      forceKillAll();
    }

    // Phase 3: Final exit after a short grace period
    setTimeout(() => {
      const stillAlive = childProcesses.filter(({ proc }) => proc.exitCode === null);
      if (stillAlive.length > 0) {
        log('shutdown', `Warning: ${stillAlive.length} process(es) may still be running:`);
        for (const { proc, label } of stillAlive) {
          log('shutdown', `  ${label} (PID ${proc.pid})`);
        }
      }
      process.exit(exitCode);
    }, 2000);
  }, 3000);
}

// Allow syntax check
if (process.argv.includes('--check')) {
  log('check', 'Syntax valid');
  log('check', `Discovered ${modules.length} modules: ${modules.join(', ')}`);
  process.exit(0);
}

// Handle all termination signals
process.on('SIGINT', () => shutdown(0));
process.on('SIGTERM', () => shutdown(0));
process.on('SIGHUP', () => shutdown(0));

// Safety net: kill children if this process exits unexpectedly
process.on('exit', () => {
  forceKillAll();
});

// Handle uncaught exceptions — kill children before crashing
process.on('uncaughtException', (err) => {
  log('error', `Uncaught exception: ${err.message}`);
  forceKillAll();
  process.exit(1);
});

// Start all processes
log('startup', 'Starting development environment...');
startDotnetRun();
startClientAppWatch();
modules.forEach((modulePath) => startModuleWatch(modulePath));

log('startup', `All processes started. Press Ctrl+C to stop.`);

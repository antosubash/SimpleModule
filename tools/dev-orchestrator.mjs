import { spawn, execSync } from 'child_process';
import path from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';
import { readdirSync, readFileSync, existsSync } from 'fs';
import { createLogger, discoverModulesWithVite } from './orchestrator-utils.mjs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const rootDir = path.resolve(__dirname, '..');
const isWindows = process.platform === 'win32';

// Auto-discover all module workspace paths
const modules = discoverModulesWithVite(rootDir);

const childProcesses = [];
const log = createLogger();
let shuttingDown = false;

// ---------------------------------------------------------------------------
// Process spawning
//
// Key: use `detached: true` on Unix so each child gets its own process group.
// This lets us `process.kill(-pid, signal)` to signal the entire tree
// (the child + all its descendants).
//
// On Windows `detached: true` opens a new console window, so we skip it
// and rely on taskkill /T for tree cleanup instead.
// ---------------------------------------------------------------------------

function spawnChild(command, args, options, label) {
  const proc = spawn(command, args, {
    cwd: options.cwd ?? rootDir,
    stdio: 'inherit',
    // Unix: new process group so we can signal the whole tree later.
    // Windows: no detach (avoids spawning a new console window).
    detached: !isWindows,
    // Avoid shell: true — it creates an intermediary sh/cmd process that
    // swallows signals and makes tree cleanup unreliable. Instead, resolve
    // the binary directly.
    shell: false,
  });

  proc.on('error', (err) => {
    log(label, `Error: ${err.message}`);
  });

  childProcesses.push({ proc, label });

  if (proc.pid) {
    log('setup', `${label} started (PID ${proc.pid})`);
  }

  // Unref so the parent doesn't wait on children when exiting
  // (we handle cleanup explicitly in shutdown)
  if (!isWindows) {
    proc.unref();
  }

  return proc;
}

function startDotnetRun() {
  log('setup', 'Starting dotnet run...');
  const proc = spawnChild(
    'dotnet',
    ['run', '--no-restore', '--project', 'template/SimpleModule.Host'],
    { cwd: rootDir },
    'dotnet',
  );

  proc.on('exit', (code) => {
    if (code !== 0 && !shuttingDown) {
      log('dotnet', `Exited with code ${code}`);
      log('error', 'Backend process terminated unexpectedly. Shutting down.');
      shutdown(1);
    }
  });

  return proc;
}

function startModuleWatch(modulePath) {
  const moduleName = path.basename(modulePath);
  log('setup', `Starting watch for ${moduleName}...`);

  // Resolve npx binary path to avoid shell: true
  const npxBin = isWindows ? 'npx.cmd' : 'npx';
  const proc = spawnChild(
    npxBin,
    ['vite', 'build', '--configLoader', 'runner', '--watch'],
    { cwd: path.resolve(rootDir, modulePath) },
    moduleName,
  );

  proc.on('exit', (code) => {
    if (code !== 0 && code !== null && !shuttingDown) {
      log(moduleName, `Watch exited with non-zero code ${code}`);
    }
  });

  return proc;
}

function startClientAppWatch() {
  log('setup', 'Starting ClientApp watch...');
  const npxBin = isWindows ? 'npx.cmd' : 'npx';
  const proc = spawnChild(
    npxBin,
    ['vite', 'build', '--configLoader', 'runner', '--watch'],
    { cwd: path.resolve(rootDir, 'template/SimpleModule.Host/ClientApp') },
    'ClientApp',
  );

  proc.on('exit', (code) => {
    if (code !== 0 && code !== null && !shuttingDown) {
      log('ClientApp', `Watch exited with non-zero code ${code}`);
    }
  });

  return proc;
}

// ---------------------------------------------------------------------------
// Process cleanup
// ---------------------------------------------------------------------------

/**
 * Graceful termination for a single child.
 *
 * Unix:  `process.kill(-pid, 'SIGTERM')` — signals the entire process group
 *        because we spawned with `detached: true` (child is a group leader).
 *
 * Windows: `taskkill /PID <pid> /T` — walks the process tree.
 *          (no /F = graceful WM_CLOSE)
 */
function killGraceful(proc, label) {
  try {
    if (proc.exitCode !== null) return;

    if (isWindows) {
      spawn('taskkill', ['/PID', String(proc.pid), '/T'], {
        stdio: 'ignore',
      });
    } else {
      // Negative PID = signal the process group (works because detached: true)
      process.kill(-proc.pid, 'SIGTERM');
    }
  } catch (err) {
    // ESRCH: process/group already exited — that's fine
    if (err.code !== 'ESRCH') {
      log(label, `Warning: Failed to terminate (PID ${proc.pid}): ${err.message}`);
    }
  }
}

/**
 * Force-kill a single child and its entire tree.
 *
 * Unix:  SIGKILL to the process group.
 * Windows: taskkill /T /F (force).
 */
function killForce(proc, label) {
  try {
    if (proc.exitCode !== null) return;

    if (isWindows) {
      spawn('taskkill', ['/PID', String(proc.pid), '/T', '/F'], {
        stdio: 'ignore',
      });
    } else {
      process.kill(-proc.pid, 'SIGKILL');
    }
  } catch (err) {
    if (err.code !== 'ESRCH') {
      log(label, `Warning: Force-kill failed (PID ${proc.pid}): ${err.message}`);
    }
  }
}

function forceKillAll() {
  for (const { proc, label } of childProcesses) {
    killForce(proc, label);
  }
}

function shutdown(exitCode = 0) {
  if (shuttingDown) return;
  shuttingDown = true;

  log('shutdown', 'Stopping all processes...');

  // Phase 1: Graceful termination (SIGTERM / WM_CLOSE)
  for (const { proc, label } of childProcesses) {
    killGraceful(proc, label);
  }

  // Phase 2: Wait 3s, then force-kill survivors
  setTimeout(() => {
    const survivors = childProcesses.filter(({ proc }) => proc.exitCode === null);
    if (survivors.length > 0) {
      log('shutdown', `Force-killing ${survivors.length} remaining process(es)...`);
      for (const { proc, label } of survivors) {
        killForce(proc, label);
      }
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

// ---------------------------------------------------------------------------
// Allow syntax check
// ---------------------------------------------------------------------------
if (process.argv.includes('--check')) {
  log('check', 'Syntax valid');
  log('check', `Discovered ${modules.length} modules: ${modules.join(', ')}`);
  process.exit(0);
}

// ---------------------------------------------------------------------------
// Signal handlers
//
// SIGINT: Ctrl+C in terminal
// SIGTERM: kill <pid>, Docker stop, systemd, etc.
// SIGHUP: terminal closed (Linux/macOS only, doesn't exist on Windows)
// ---------------------------------------------------------------------------
process.on('SIGINT', () => shutdown(0));
process.on('SIGTERM', () => shutdown(0));
if (!isWindows) {
  process.on('SIGHUP', () => shutdown(0));
}

// Safety net: force-kill children when this process exits.
// Note: `process.on('exit')` callbacks run synchronously — no async.
// `process.kill()` is synchronous, so this works. `spawn()` would not.
process.on('exit', () => {
  for (const { proc, label } of childProcesses) {
    try {
      if (proc.exitCode === null) {
        if (isWindows) {
          // On Windows in 'exit' handler, we can't spawn new processes reliably.
          // Use proc.kill() as best-effort (only kills direct child, not tree).
          proc.kill('SIGTERM');
        } else {
          // Unix: kill the process group synchronously
          process.kill(-proc.pid, 'SIGKILL');
        }
      }
    } catch {
      // ESRCH or already exited — ignore
    }
  }
});

// Handle uncaught exceptions — kill children before crashing
process.on('uncaughtException', (err) => {
  log('error', `Uncaught exception: ${err.message}`);
  forceKillAll();
  process.exit(1);
});

// ---------------------------------------------------------------------------
// Start all processes
// ---------------------------------------------------------------------------
log('startup', 'Starting development environment...');
startDotnetRun();
startClientAppWatch();
modules.forEach((modulePath) => startModuleWatch(modulePath));

log('startup', `All processes started. Press Ctrl+C to stop.`);

#!/usr/bin/env node
// Real-consumer smoke test for packed module .nupkgs.
//
// Builds a throwaway ASP.NET Core app that PackageReferences the packed
// module .nupkgs from a local NuGet feed and asserts the consumer build
// receives the modules' static web assets through MSBuild props/targets.
//
// This catches the regression class where the .nupkg shipped fine on disk
// but the consumer's wwwroot pipeline didn't pick the assets up — invisible
// to the in-repo `template/SimpleModule.Host` because it consumes modules
// via ProjectReference.
//
// Usage: node scripts/smoke-test-nupkg-consumer.mjs <nupkg-dir> [--version 0.0.0-ci]

import { execFileSync, spawnSync } from 'node:child_process';
import {
  cpSync,
  existsSync,
  mkdirSync,
  mkdtempSync,
  readFileSync,
  readdirSync,
  rmSync,
  statSync,
  writeFileSync,
} from 'node:fs';
import { tmpdir } from 'node:os';
import { basename, dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '..');

const args = process.argv.slice(2);
const nupkgDir = args[0];
if (!nupkgDir) {
  console.error('Usage: smoke-test-nupkg-consumer.mjs <nupkg-dir> [--version <ver>]');
  process.exit(2);
}
const versionArgIdx = args.indexOf('--version');
const packageVersion = versionArgIdx >= 0 ? args[versionArgIdx + 1] : '0.0.0-ci';

const absoluteNupkgDir = resolve(nupkgDir);
if (!existsSync(absoluteNupkgDir)) {
  console.error(`nupkg dir not found: ${absoluteNupkgDir}`);
  process.exit(2);
}

// Discover UI-shipping modules the same way verify-nupkg-static-assets.mjs does.
const modulesDir = join(repoRoot, 'modules');
const moduleDirs = readdirSync(modulesDir)
  .map((name) => join(modulesDir, name, 'src'))
  .filter((p) => existsSync(p) && statSync(p).isDirectory())
  .flatMap((srcDir) =>
    readdirSync(srcDir)
      .map((name) => join(srcDir, name))
      .filter((p) => statSync(p).isDirectory()),
  );

const uiModules = moduleDirs
  .filter((dir) => {
    const csproj = readdirSync(dir).find((f) => f.endsWith('.csproj'));
    return (
      csproj &&
      existsSync(join(dir, 'package.json')) &&
      existsSync(join(dir, 'Pages', 'index.ts'))
    );
  })
  .map((dir) => basename(dir));

if (uiModules.length === 0) {
  console.error('No UI-shipping modules discovered under modules/.');
  process.exit(2);
}

const consumerDir = mkdtempSync(join(tmpdir(), 'sm-smoke-consumer-'));
const cleanup = () => rmSync(consumerDir, { recursive: true, force: true });
process.on('exit', cleanup);
process.on('SIGINT', () => {
  cleanup();
  process.exit(130);
});

console.log(`Smoke consumer at ${consumerDir}`);
console.log(`Local feed: ${absoluteNupkgDir}`);
console.log(`Package version: ${packageVersion}`);
console.log(`UI modules: ${uiModules.join(', ')}\n`);

const packageRefs = uiModules
  .map((id) => `    <PackageReference Include="${id}" Version="${packageVersion}" />`)
  .join('\n');

writeFileSync(
  join(consumerDir, 'Consumer.csproj'),
  `<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <!-- The consumer only verifies that static assets propagate. We don't call
         AddSimpleModule, so generator diagnostics that require runtime wiring
         (DB, auth, etc.) would be noise here. -->
    <NoWarn>$(NoWarn);SM0001;SM0002;SM0003;SM0025;SM0028</NoWarn>
    <!-- The Web SDK's default **/*.cs glob would otherwise pick up content
         files from the local global-packages folder (e.g. ImTools.cs ships
         multiple copies via content/ and contentFiles/, which duplicate when
         the project root contains them). -->
    <DefaultItemExcludes>$(DefaultItemExcludes);packages/**</DefaultItemExcludes>
  </PropertyGroup>
  <ItemGroup>
${packageRefs}
  </ItemGroup>
</Project>
`,
);

writeFileSync(
  join(consumerDir, 'Program.cs'),
  `var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/", () => "ok");
app.Run();
`,
);

writeFileSync(
  join(consumerDir, 'nuget.config'),
  `<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <config>
    <add key="globalPackagesFolder" value="./packages" />
  </config>
  <packageSources>
    <clear />
    <add key="local" value="${absoluteNupkgDir}" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
`,
);

// Empty Directory.Build.props/targets to prevent inheriting repo-level config.
writeFileSync(join(consumerDir, 'Directory.Build.props'), '<Project />\n');
writeFileSync(join(consumerDir, 'Directory.Build.targets'), '<Project />\n');

const run = (cmd, runArgs, opts = {}) => {
  const result = spawnSync(cmd, runArgs, {
    cwd: consumerDir,
    stdio: 'inherit',
    ...opts,
  });
  if (result.status !== 0) {
    console.error(`\nFAIL: ${cmd} ${runArgs.join(' ')} (exit ${result.status})`);
    process.exit(result.status ?? 1);
  }
};

console.log('--- dotnet restore ---');
run('dotnet', ['restore', '--verbosity', 'minimal']);

console.log('\n--- dotnet build ---');
run('dotnet', ['build', '-c', 'Release', '--no-restore', '--verbosity', 'minimal']);

// Verify the consumer's static web assets manifest references each module's pages.js.
const candidates = [
  join(consumerDir, 'obj', 'Release', 'net10.0', 'staticwebassets.build.json'),
  join(consumerDir, 'obj', 'Release', 'net10.0', 'staticwebassets.build.endpoints.json'),
  join(consumerDir, 'bin', 'Release', 'net10.0', 'Consumer.staticwebassets.runtime.json'),
];

const manifests = candidates.filter((p) => existsSync(p));
if (manifests.length === 0) {
  console.error('\nFAIL: no static-web-assets manifest produced by consumer build.');
  console.error(`Looked for:\n  ${candidates.join('\n  ')}`);
  process.exit(1);
}

const manifestText = manifests.map((p) => readFileSync(p, 'utf8')).join('\n');

let failures = 0;
for (const id of uiModules) {
  const needle = `_content/${id}/${id}.pages.js`;
  if (manifestText.includes(needle)) {
    console.log(`OK   ${id} → ${needle}`);
  } else {
    console.error(`FAIL ${id}: ${needle} not in any manifest`);
    failures++;
  }
}

if (failures > 0) {
  console.error(`\n${failures}/${uiModules.length} module(s) missing static asset entries in consumer manifest.`);
  process.exit(1);
}

console.log(`\nVerified ${uiModules.length} module package(s) consumed by a real PackageReference build.`);

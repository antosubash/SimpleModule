// Browser-runtime exports only. Build-time helpers (defineModuleConfig,
// vendorBuildPlugin, vendorPaths, defaultVendors) import Node-only APIs
// (node:fs / node:path / node:module) and must NOT be re-exported here: a page
// that imports the root barrel would otherwise pull `createRequire` into the
// browser bundle, throw "createRequire is not a function", and render blank (#237).
// Import those from '@simplemodule/client/module' and '@simplemodule/client/vite'.
export { resolvePage } from './resolve-page.ts';
export { routes } from './routes.ts';
export { useTranslation } from './use-translation.ts';

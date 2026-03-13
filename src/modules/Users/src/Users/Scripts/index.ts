import { initQrCode } from './qrcode';

export function afterWebStarted(blazor: any) {
  initModuleScripts();
  blazor.addEventListener('enhancedload', initModuleScripts);
}

function initModuleScripts() {
  initQrCode();
}

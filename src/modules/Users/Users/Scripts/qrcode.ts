import QRCode from 'qrcode';

export function initQrCode() {
    const dataEl = document.getElementById('qrCodeData');
    const qrEl = document.getElementById('qrCode');

    if (!dataEl || !qrEl) return;

    const url = dataEl.getAttribute('data-url');
    if (!url) return;

    while (qrEl.firstChild) {
        qrEl.removeChild(qrEl.firstChild);
    }

    const canvas = document.createElement('canvas');
    qrEl.appendChild(canvas);

    QRCode.toCanvas(canvas, url, { width: 200 }, (error) => {
        if (error) console.error('QR code generation failed:', error);
    });
}

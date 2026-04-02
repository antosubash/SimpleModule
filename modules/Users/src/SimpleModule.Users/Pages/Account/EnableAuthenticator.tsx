import { router } from '@inertiajs/react';
import { Alert, AlertDescription, Button, Input, Label } from '@simplemodule/ui';
import QRCode from 'qrcode';
import { useEffect, useState } from 'react';
import ManageLayout from './ManageLayout';

interface Props {
  sharedKey: string;
  authenticatorUri: string;
}

const statusMessages: Record<string, string> = {
  'authenticator-reset':
    'Your authenticator app key has been reset, you will need to configure your authenticator app using the new key.',
};

const errorMessages: Record<string, string> = {
  'invalid-code': 'Verification code is invalid.',
};

export default function EnableAuthenticator({ sharedKey, authenticatorUri }: Props) {
  const [qrCodeUrl, setQrCodeUrl] = useState<string | null>(null);

  const params = new URLSearchParams(window.location.search);
  const status = params.get('status');
  const error = params.get('error');
  const statusMessage = status ? statusMessages[status] : null;
  const errorMessage = error ? errorMessages[error] : null;

  useEffect(() => {
    QRCode.toDataURL(authenticatorUri).then(setQrCodeUrl);
  }, [authenticatorUri]);

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post('/Identity/Account/Manage/EnableAuthenticator', formData);
  }

  return (
    <ManageLayout activePage="TwoFactorAuthentication">
      <h3 className="text-lg font-semibold mb-3 sm:mb-4">Configure authenticator app</h3>

      {statusMessage && (
        <Alert variant="success" className="mb-4">
          <AlertDescription>{statusMessage}</AlertDescription>
        </Alert>
      )}

      {errorMessage && (
        <Alert variant="danger" className="mb-4">
          <AlertDescription>{errorMessage}</AlertDescription>
        </Alert>
      )}

      <p className="text-sm text-text-muted mb-4">
        To use an authenticator app go through the following steps:
      </p>

      <ol className="list-decimal pl-5 space-y-4 sm:space-y-6 text-sm">
        <li>
          <p className="text-text-muted">
            Download a two-factor authenticator app like Microsoft Authenticator or Google
            Authenticator.
          </p>
        </li>
        <li>
          <p className="mb-2">
            Scan the QR Code or enter this key into your two factor authenticator app. Spaces and
            casing do not matter.
          </p>
          <code className="block bg-surface-raised px-3 py-2 rounded-lg text-sm mb-4 select-all">
            {sharedKey}
          </code>
          {qrCodeUrl && (
            <img
              src={qrCodeUrl}
              alt="QR Code for authenticator app"
              className="w-36 h-36 sm:w-48 sm:h-48"
            />
          )}
        </li>
        <li>
          <p className="mb-4">
            Once you have scanned the QR code or input the key above, your two factor authentication
            app will provide you with a unique code. Enter the code in the confirmation box below.
          </p>
          <form onSubmit={handleSubmit} className="space-y-3 sm:space-y-4">
            <div>
              <Label htmlFor="code">Verification Code</Label>
              <Input
                id="code"
                name="code"
                autoComplete="off"
                placeholder="Please enter the code."
              />
            </div>
            <Button type="submit">Verify</Button>
          </form>
        </li>
      </ol>
    </ManageLayout>
  );
}

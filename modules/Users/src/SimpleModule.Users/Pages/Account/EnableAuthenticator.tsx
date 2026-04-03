import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import { Alert, AlertDescription, Button, Input, Label } from '@simplemodule/ui';
import QRCode from 'qrcode';
import { useEffect, useState } from 'react';
import { UsersKeys } from '../../Locales/keys';
import ManageLayout from './ManageLayout';

interface Props {
  sharedKey: string;
  authenticatorUri: string;
}

export default function EnableAuthenticator({ sharedKey, authenticatorUri }: Props) {
  const { t } = useTranslation('Users');
  const [qrCodeUrl, setQrCodeUrl] = useState<string | null>(null);

  const params = new URLSearchParams(window.location.search);
  const status = params.get('status');
  const error = params.get('error');

  const statusMessages: Record<string, string> = {
    'authenticator-reset': t(UsersKeys.EnableAuthenticator.Status.AuthenticatorReset),
  };

  const errorMessages: Record<string, string> = {
    'invalid-code': t(UsersKeys.EnableAuthenticator.Error.InvalidCode),
  };

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
      <h3 className="text-lg font-semibold mb-3 sm:mb-4">
        {t(UsersKeys.EnableAuthenticator.Title)}
      </h3>

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

      <p className="text-sm text-text-muted mb-4">{t(UsersKeys.EnableAuthenticator.Intro)}</p>

      <ol className="list-decimal pl-5 space-y-4 sm:space-y-6 text-sm">
        <li>
          <p className="text-text-muted">{t(UsersKeys.EnableAuthenticator.Step1)}</p>
        </li>
        <li>
          <p className="mb-2">{t(UsersKeys.EnableAuthenticator.Step2)}</p>
          <code className="block bg-surface-raised px-3 py-2 rounded-lg text-sm mb-4 select-all">
            {sharedKey}
          </code>
          {qrCodeUrl && (
            <img
              src={qrCodeUrl}
              alt={t(UsersKeys.EnableAuthenticator.QrCodeAlt)}
              className="w-36 h-36 sm:w-48 sm:h-48"
            />
          )}
        </li>
        <li>
          <p className="mb-4">{t(UsersKeys.EnableAuthenticator.Step3)}</p>
          <form onSubmit={handleSubmit} className="space-y-3 sm:space-y-4">
            <div>
              <Label htmlFor="code">{t(UsersKeys.EnableAuthenticator.VerificationCodeLabel)}</Label>
              <Input
                id="code"
                name="code"
                autoComplete="off"
                placeholder={t(UsersKeys.EnableAuthenticator.VerificationCodePlaceholder)}
              />
            </div>
            <Button type="submit">{t(UsersKeys.EnableAuthenticator.VerifyButton)}</Button>
          </form>
        </li>
      </ol>
    </ManageLayout>
  );
}

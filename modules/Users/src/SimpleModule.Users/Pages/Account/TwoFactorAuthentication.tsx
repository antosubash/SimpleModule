import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import { Alert, AlertDescription, AlertTitle, Button } from '@simplemodule/ui';
import { UsersKeys } from '@/Locales/keys';
import ManageLayout from './ManageLayout';

interface Props {
  hasAuthenticator: boolean;
  is2faEnabled: boolean;
  isMachineRemembered: boolean;
  recoveryCodesLeft: number;
}

export default function TwoFactorAuthentication({
  hasAuthenticator,
  is2faEnabled,
  isMachineRemembered,
  recoveryCodesLeft,
}: Props) {
  const { t } = useTranslation('Users');
  const status = new URLSearchParams(window.location.search).get('status');

  const statusMessages: Record<string, string> = {
    'browser-forgotten': t(UsersKeys.TwoFactor.Status.BrowserForgotten),
    '2fa-disabled': t(UsersKeys.TwoFactor.Status['2faDisabled']),
    'authenticator-verified': t(UsersKeys.TwoFactor.Status.AuthenticatorVerified),
  };

  const statusMessage = status ? statusMessages[status] : null;

  function handleForgetBrowser(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    router.post('/Identity/Account/Manage/TwoFactorAuthentication/forget-browser');
  }

  return (
    <ManageLayout activePage="TwoFactorAuthentication">
      <h3 className="text-lg font-semibold mb-3 sm:mb-4">{t(UsersKeys.TwoFactor.Title)}</h3>

      {statusMessage && (
        <Alert variant="success" className="mb-4">
          <AlertDescription>{statusMessage}</AlertDescription>
        </Alert>
      )}

      {is2faEnabled && (
        <>
          {recoveryCodesLeft === 0 && (
            <Alert variant="danger" className="mb-4">
              <AlertTitle>{t(UsersKeys.TwoFactor.NoRecoveryCodesTitle)}</AlertTitle>
              <AlertDescription>
                {t(UsersKeys.TwoFactor.NoRecoveryCodesDescription)}{' '}
                <a
                  href="/Identity/Account/Manage/GenerateRecoveryCodes"
                  className="font-medium underline"
                >
                  {t(UsersKeys.TwoFactor.NoRecoveryCodesLinkText)}
                </a>{' '}
                {t(UsersKeys.TwoFactor.NoRecoveryCodesDescriptionSuffix)}
              </AlertDescription>
            </Alert>
          )}

          {recoveryCodesLeft === 1 && (
            <Alert variant="danger" className="mb-4">
              <AlertTitle>{t(UsersKeys.TwoFactor.OneRecoveryCodeTitle)}</AlertTitle>
              <AlertDescription>
                {t(UsersKeys.TwoFactor.OneRecoveryCodeDescription)}{' '}
                <a
                  href="/Identity/Account/Manage/GenerateRecoveryCodes"
                  className="font-medium underline"
                >
                  {t(UsersKeys.TwoFactor.OneRecoveryCodeLinkText)}
                </a>
                .
              </AlertDescription>
            </Alert>
          )}

          {recoveryCodesLeft >= 2 && recoveryCodesLeft <= 3 && (
            <Alert variant="warning" className="mb-4">
              <AlertTitle>
                {t(UsersKeys.TwoFactor.FewRecoveryCodesTitle, { count: String(recoveryCodesLeft) })}
              </AlertTitle>
              <AlertDescription>
                {t(UsersKeys.TwoFactor.FewRecoveryCodesDescription)}{' '}
                <a
                  href="/Identity/Account/Manage/GenerateRecoveryCodes"
                  className="font-medium underline"
                >
                  {t(UsersKeys.TwoFactor.FewRecoveryCodesLinkText)}
                </a>
                .
              </AlertDescription>
            </Alert>
          )}

          <div className="mb-4 sm:mb-6 space-y-3">
            {isMachineRemembered && (
              <form onSubmit={handleForgetBrowser}>
                <Button type="submit" variant="outline">
                  {t(UsersKeys.TwoFactor.ForgetBrowser)}
                </Button>
              </form>
            )}
            <div className="flex flex-wrap gap-2">
              <Button
                variant="outline"
                onClick={() => router.get('/Identity/Account/Manage/Disable2fa')}
              >
                {t(UsersKeys.TwoFactor.Disable2fa)}
              </Button>
              <Button
                variant="outline"
                onClick={() => router.get('/Identity/Account/Manage/GenerateRecoveryCodes')}
              >
                {t(UsersKeys.TwoFactor.ResetRecoveryCodes)}
              </Button>
            </div>
          </div>

          <div className="border-t border-border pt-4 sm:pt-6" />
        </>
      )}

      <h3 className="text-lg font-semibold mb-3 sm:mb-4">
        {t(UsersKeys.TwoFactor.AuthenticatorAppTitle)}
      </h3>
      {!hasAuthenticator ? (
        <Button onClick={() => router.get('/Identity/Account/Manage/EnableAuthenticator')}>
          {t(UsersKeys.TwoFactor.AddAuthenticatorApp)}
        </Button>
      ) : (
        <div className="flex flex-wrap gap-2">
          <Button onClick={() => router.get('/Identity/Account/Manage/EnableAuthenticator')}>
            {t(UsersKeys.TwoFactor.SetUpAuthenticatorApp)}
          </Button>
          <Button
            variant="outline"
            onClick={() => router.get('/Identity/Account/Manage/ResetAuthenticator')}
          >
            {t(UsersKeys.TwoFactor.ResetAuthenticatorApp)}
          </Button>
        </div>
      )}
    </ManageLayout>
  );
}

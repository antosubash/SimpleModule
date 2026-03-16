import { router } from '@inertiajs/react';
import { Alert, AlertDescription, AlertTitle, Button } from '@simplemodule/ui';
import ManageLayout from './ManageLayout';

interface Props {
  hasAuthenticator: boolean;
  is2faEnabled: boolean;
  isMachineRemembered: boolean;
  recoveryCodesLeft: number;
}

const statusMessages: Record<string, string> = {
  'browser-forgotten':
    'The current browser has been forgotten. When you login again from this browser you will be prompted for your 2FA code.',
  '2fa-disabled':
    '2FA has been disabled. You can reenable 2FA when you setup an authenticator app.',
  'authenticator-verified': 'Your authenticator app has been verified.',
};

export default function TwoFactorAuthentication({
  hasAuthenticator,
  is2faEnabled,
  isMachineRemembered,
  recoveryCodesLeft,
}: Props) {
  const status = new URLSearchParams(window.location.search).get('status');
  const statusMessage = status ? statusMessages[status] : null;

  function handleForgetBrowser(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    router.post('/Identity/Account/Manage/TwoFactorAuthentication/forget-browser');
  }

  return (
    <ManageLayout activePage="TwoFactorAuthentication">
      <h3 className="text-lg font-semibold mb-4">Two-factor authentication (2FA)</h3>

      {statusMessage && (
        <Alert variant="success" className="mb-4">
          <AlertDescription>{statusMessage}</AlertDescription>
        </Alert>
      )}

      {is2faEnabled && (
        <>
          {recoveryCodesLeft === 0 && (
            <Alert variant="danger" className="mb-4">
              <AlertTitle>You have no recovery codes left.</AlertTitle>
              <AlertDescription>
                You must{' '}
                <a
                  href="/Identity/Account/Manage/GenerateRecoveryCodes"
                  className="font-medium underline"
                >
                  generate a new set of recovery codes
                </a>{' '}
                before you can log in with a recovery code.
              </AlertDescription>
            </Alert>
          )}

          {recoveryCodesLeft === 1 && (
            <Alert variant="danger" className="mb-4">
              <AlertTitle>You have 1 recovery code left.</AlertTitle>
              <AlertDescription>
                You can{' '}
                <a
                  href="/Identity/Account/Manage/GenerateRecoveryCodes"
                  className="font-medium underline"
                >
                  generate a new set of recovery codes
                </a>
                .
              </AlertDescription>
            </Alert>
          )}

          {recoveryCodesLeft >= 2 && recoveryCodesLeft <= 3 && (
            <Alert variant="warning" className="mb-4">
              <AlertTitle>You have {recoveryCodesLeft} recovery codes left.</AlertTitle>
              <AlertDescription>
                You should{' '}
                <a
                  href="/Identity/Account/Manage/GenerateRecoveryCodes"
                  className="font-medium underline"
                >
                  generate a new set of recovery codes
                </a>
                .
              </AlertDescription>
            </Alert>
          )}

          <div className="mb-6 space-y-3">
            {isMachineRemembered && (
              <form onSubmit={handleForgetBrowser}>
                <Button type="submit" variant="outline">
                  Forget this browser
                </Button>
              </form>
            )}
            <div className="flex flex-wrap gap-2">
              <Button
                variant="outline"
                onClick={() => router.get('/Identity/Account/Manage/Disable2fa')}
              >
                Disable 2FA
              </Button>
              <Button
                variant="outline"
                onClick={() => router.get('/Identity/Account/Manage/GenerateRecoveryCodes')}
              >
                Reset recovery codes
              </Button>
            </div>
          </div>

          <div className="border-t border-border pt-6" />
        </>
      )}

      <h3 className="text-lg font-semibold mb-4">Authenticator app</h3>
      {!hasAuthenticator ? (
        <Button onClick={() => router.get('/Identity/Account/Manage/EnableAuthenticator')}>
          Add authenticator app
        </Button>
      ) : (
        <div className="flex flex-wrap gap-2">
          <Button onClick={() => router.get('/Identity/Account/Manage/EnableAuthenticator')}>
            Set up authenticator app
          </Button>
          <Button
            variant="outline"
            onClick={() => router.get('/Identity/Account/Manage/ResetAuthenticator')}
          >
            Reset authenticator app
          </Button>
        </div>
      )}
    </ManageLayout>
  );
}

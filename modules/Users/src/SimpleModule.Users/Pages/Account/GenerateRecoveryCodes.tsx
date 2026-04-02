import { router } from '@inertiajs/react';
import { Alert, AlertDescription, AlertTitle, Button } from '@simplemodule/ui';
import ManageLayout from './ManageLayout';

export default function GenerateRecoveryCodes() {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    router.post('/Identity/Account/Manage/GenerateRecoveryCodes');
  }

  return (
    <ManageLayout activePage="TwoFactorAuthentication">
      <h3 className="text-lg font-semibold mb-3 sm:mb-4">
        Generate two-factor authentication (2FA) recovery codes
      </h3>

      <Alert variant="warning" className="mb-4 sm:mb-6">
        <AlertTitle>Put these codes in a safe place.</AlertTitle>
        <AlertDescription>
          <p>
            If you lose your device and don&apos;t have the recovery codes you will lose access to
            your account.
          </p>
          <p className="mt-2">
            Generating new recovery codes does not change the keys used in authenticator apps. If
            you wish to change the key used in an authenticator app you should{' '}
            <a href="/Identity/Account/Manage/ResetAuthenticator" className="font-medium underline">
              reset your authenticator keys
            </a>
            .
          </p>
        </AlertDescription>
      </Alert>

      <form onSubmit={handleSubmit}>
        <Button type="submit" variant="danger">
          Generate Recovery Codes
        </Button>
      </form>
    </ManageLayout>
  );
}

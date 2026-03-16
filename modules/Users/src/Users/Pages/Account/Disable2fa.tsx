import { router } from '@inertiajs/react';
import { Alert, AlertDescription, AlertTitle, Button } from '@simplemodule/ui';
import ManageLayout from './ManageLayout';

export default function Disable2fa() {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    router.post('/Identity/Account/Manage/Disable2fa');
  }

  return (
    <ManageLayout activePage="TwoFactorAuthentication">
      <h3 className="text-lg font-semibold mb-4">Disable two-factor authentication (2FA)</h3>

      <Alert variant="warning" className="mb-6">
        <AlertTitle>This action only disables 2FA.</AlertTitle>
        <AlertDescription>
          Disabling 2FA does not change the keys used in authenticator apps. If you wish to change
          the key used in an authenticator app you should{' '}
          <a href="/Identity/Account/Manage/ResetAuthenticator" className="font-medium underline">
            reset your authenticator keys
          </a>
          .
        </AlertDescription>
      </Alert>

      <form onSubmit={handleSubmit}>
        <Button type="submit" variant="danger">
          Disable 2FA
        </Button>
      </form>
    </ManageLayout>
  );
}

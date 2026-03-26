import { router } from '@inertiajs/react';
import { Alert, AlertDescription, AlertTitle, Button } from '@simplemodule/ui';
import ManageLayout from './ManageLayout';

export default function ResetAuthenticator() {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    router.post('/Identity/Account/Manage/ResetAuthenticator');
  }

  return (
    <ManageLayout activePage="TwoFactorAuthentication">
      <h3 className="text-lg font-semibold mb-4">Reset authenticator key</h3>

      <Alert variant="warning" className="mb-6">
        <AlertTitle>
          If you reset your authenticator key your authenticator app will not work until you
          reconfigure it.
        </AlertTitle>
        <AlertDescription>
          This process disables 2FA until you verify your authenticator app. If you do not complete
          your authenticator app configuration you may lose access to your account.
        </AlertDescription>
      </Alert>

      <form onSubmit={handleSubmit}>
        <Button type="submit" variant="danger">
          Reset authenticator key
        </Button>
      </form>
    </ManageLayout>
  );
}

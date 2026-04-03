import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import { Alert, AlertDescription, AlertTitle, Button } from '@simplemodule/ui';
import { UsersKeys } from '@/Locales/keys';
import ManageLayout from './ManageLayout';

export default function ResetAuthenticator() {
  const { t } = useTranslation('Users');

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    router.post('/Identity/Account/Manage/ResetAuthenticator');
  }

  return (
    <ManageLayout activePage="TwoFactorAuthentication">
      <h3 className="text-lg font-semibold mb-3 sm:mb-4">
        {t(UsersKeys.ResetAuthenticator.Title)}
      </h3>

      <Alert variant="warning" className="mb-4 sm:mb-6">
        <AlertTitle>{t(UsersKeys.ResetAuthenticator.WarningTitle)}</AlertTitle>
        <AlertDescription>{t(UsersKeys.ResetAuthenticator.WarningDescription)}</AlertDescription>
      </Alert>

      <form onSubmit={handleSubmit}>
        <Button type="submit" variant="danger">
          {t(UsersKeys.ResetAuthenticator.ResetButton)}
        </Button>
      </form>
    </ManageLayout>
  );
}

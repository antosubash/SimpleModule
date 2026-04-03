import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import { Alert, AlertDescription, AlertTitle, Button } from '@simplemodule/ui';
import { UsersKeys } from '../../Locales/keys';
import ManageLayout from './ManageLayout';

export default function Disable2fa() {
  const { t } = useTranslation('Users');

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    router.post('/Identity/Account/Manage/Disable2fa');
  }

  return (
    <ManageLayout activePage="TwoFactorAuthentication">
      <h3 className="text-lg font-semibold mb-3 sm:mb-4">{t(UsersKeys.Disable2fa.Title)}</h3>

      <Alert variant="warning" className="mb-4 sm:mb-6">
        <AlertTitle>{t(UsersKeys.Disable2fa.WarningTitle)}</AlertTitle>
        <AlertDescription>
          {t(UsersKeys.Disable2fa.WarningDescription)}{' '}
          <a href="/Identity/Account/Manage/ResetAuthenticator" className="font-medium underline">
            {t(UsersKeys.Disable2fa.ResetKeysLinkText)}
          </a>
          .
        </AlertDescription>
      </Alert>

      <form onSubmit={handleSubmit}>
        <Button type="submit" variant="danger">
          {t(UsersKeys.Disable2fa.DisableButton)}
        </Button>
      </form>
    </ManageLayout>
  );
}

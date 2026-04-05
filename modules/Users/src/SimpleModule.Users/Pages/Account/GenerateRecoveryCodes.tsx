import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import { Alert, AlertDescription, AlertTitle, Button } from '@simplemodule/ui';
import ManageLayout from '@/components/ManageLayout';
import { UsersKeys } from '@/Locales/keys';

export default function GenerateRecoveryCodes() {
  const { t } = useTranslation('Users');

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    router.post('/Identity/Account/Manage/GenerateRecoveryCodes');
  }

  return (
    <ManageLayout activePage="TwoFactorAuthentication">
      <h3 className="text-lg font-semibold mb-3 sm:mb-4">
        {t(UsersKeys.GenerateRecoveryCodes.Title)}
      </h3>

      <Alert variant="warning" className="mb-4 sm:mb-6">
        <AlertTitle>{t(UsersKeys.GenerateRecoveryCodes.WarningTitle)}</AlertTitle>
        <AlertDescription>
          <p>{t(UsersKeys.GenerateRecoveryCodes.WarningDescription1)}</p>
          <p className="mt-2">
            {t(UsersKeys.GenerateRecoveryCodes.WarningDescription2)}{' '}
            <a href="/Identity/Account/Manage/ResetAuthenticator" className="font-medium underline">
              {t(UsersKeys.GenerateRecoveryCodes.ResetKeysLinkText)}
            </a>
            .
          </p>
        </AlertDescription>
      </Alert>

      <form onSubmit={handleSubmit}>
        <Button type="submit" variant="danger">
          {t(UsersKeys.GenerateRecoveryCodes.GenerateButton)}
        </Button>
      </form>
    </ManageLayout>
  );
}

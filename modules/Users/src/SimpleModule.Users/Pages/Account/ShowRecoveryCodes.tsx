import { useTranslation } from '@simplemodule/client/use-translation';
import { Alert, AlertDescription, AlertTitle, Button } from '@simplemodule/ui';
import ManageLayout from '@/components/ManageLayout';
import { UsersKeys } from '@/Locales/keys';

interface Props {
  recoveryCodes: string[];
  statusMessage?: string;
}

export default function ShowRecoveryCodes({ recoveryCodes, statusMessage }: Props) {
  const { t } = useTranslation('Users');

  return (
    <ManageLayout activePage="TwoFactorAuthentication">
      <h3 className="text-lg font-semibold mb-3 sm:mb-4">{t(UsersKeys.ShowRecoveryCodes.Title)}</h3>

      {statusMessage && (
        <Alert variant="success" className="mb-4">
          <AlertDescription>{statusMessage}</AlertDescription>
        </Alert>
      )}

      <Alert variant="warning" className="mb-4 sm:mb-6">
        <AlertTitle>{t(UsersKeys.ShowRecoveryCodes.WarningTitle)}</AlertTitle>
        <AlertDescription>{t(UsersKeys.ShowRecoveryCodes.WarningDescription)}</AlertDescription>
      </Alert>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 mb-4 sm:mb-6">
        {recoveryCodes.map((code) => (
          <code
            key={code}
            className="block bg-surface-raised px-3 py-2 rounded-lg text-sm text-center select-all"
          >
            {code}
          </code>
        ))}
      </div>

      <Button
        variant="outline"
        onClick={() => {
          window.location.href = '/Identity/Account/Manage/TwoFactorAuthentication';
        }}
      >
        {t(UsersKeys.ShowRecoveryCodes.BackButton)}
      </Button>
    </ManageLayout>
  );
}

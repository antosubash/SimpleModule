import { Alert, AlertDescription, AlertTitle, Button } from '@simplemodule/ui';
import ManageLayout from './ManageLayout';

interface Props {
  recoveryCodes: string[];
  statusMessage?: string;
}

export default function ShowRecoveryCodes({ recoveryCodes, statusMessage }: Props) {
  return (
    <ManageLayout activePage="TwoFactorAuthentication">
      <h3 className="text-lg font-semibold mb-4">Recovery codes</h3>

      {statusMessage && (
        <Alert variant="success" className="mb-4">
          <AlertDescription>{statusMessage}</AlertDescription>
        </Alert>
      )}

      <Alert variant="warning" className="mb-6">
        <AlertTitle>Put these codes in a safe place.</AlertTitle>
        <AlertDescription>
          If you lose your device and don&apos;t have the recovery codes you will lose access to
          your account.
        </AlertDescription>
      </Alert>

      <div className="grid grid-cols-2 gap-2 mb-6">
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
        Back to two-factor authentication
      </Button>
    </ManageLayout>
  );
}

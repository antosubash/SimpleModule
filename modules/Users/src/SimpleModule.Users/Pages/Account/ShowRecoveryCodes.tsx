import { useTranslation } from '@simplemodule/client/use-translation';
import { Alert, AlertDescription, AlertTitle, Button } from '@simplemodule/ui';
import ManageLayout from '@/components/ManageLayout';
import { UsersKeys } from '@/Locales/keys';

interface Props {
  recoveryCodes: string[];
  userEmail?: string | null;
  generatedAt?: string | null;
  statusMessage?: string;
}

function downloadCodes(codes: string[], header: string, fileName = 'simplemodule-recovery-codes.txt') {
  const body = [header, '', ...codes].join('\n');
  const blob = new Blob([body], { type: 'text/plain;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}

export default function ShowRecoveryCodes({
  recoveryCodes,
  userEmail,
  generatedAt,
  statusMessage,
}: Props) {
  const { t } = useTranslation('Users');
  const printHeader = t(UsersKeys.ShowRecoveryCodes.PrintHeader, {
    email: userEmail ?? '',
    date: generatedAt ?? new Date().toISOString(),
  });

  return (
    <ManageLayout activePage="TwoFactorAuthentication">
      <style>{`
        @media print {
          body * { visibility: hidden; }
          #recovery-codes-print, #recovery-codes-print * { visibility: visible; }
          #recovery-codes-print {
            position: absolute; left: 0; top: 0; width: 100%;
            color: #000; background: #fff; padding: 1in; font-family: ui-monospace, monospace;
          }
        }
      `}</style>

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

      <div id="recovery-codes-print">
        <p className="hidden print:block mb-4 text-sm">{printHeader}</p>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 mb-4 sm:mb-6">
          {recoveryCodes.map((code) => (
            <code
              key={code}
              className="block bg-surface-raised px-3 py-2 rounded-lg text-sm text-center select-all print:bg-transparent print:text-black"
            >
              {code}
            </code>
          ))}
        </div>
      </div>

      <div className="flex flex-wrap gap-2">
        <Button
          variant="outline"
          onClick={() => {
            window.location.href = '/Identity/Account/Manage/TwoFactorAuthentication';
          }}
        >
          {t(UsersKeys.ShowRecoveryCodes.BackButton)}
        </Button>
        <Button variant="outline" onClick={() => downloadCodes(recoveryCodes, printHeader)}>
          {t(UsersKeys.ShowRecoveryCodes.DownloadButton)}
        </Button>
        <Button variant="outline" onClick={() => window.print()}>
          {t(UsersKeys.ShowRecoveryCodes.PrintButton)}
        </Button>
      </div>
    </ManageLayout>
  );
}

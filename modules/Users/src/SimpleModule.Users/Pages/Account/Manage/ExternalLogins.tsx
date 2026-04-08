import { router } from '@inertiajs/react';
import { Button } from '@simplemodule/ui';
import ManageLayout from '@/components/ManageLayout';

interface LoginInfo {
  loginProvider: string;
  providerKey: string;
  providerDisplayName?: string;
}

interface ExternalProvider {
  name: string;
  displayName: string;
}

interface Props {
  currentLogins: LoginInfo[];
  otherLogins: ExternalProvider[];
  showRemoveButton: boolean;
  statusMessage?: string;
}

export default function ExternalLogins({
  currentLogins,
  otherLogins,
  showRemoveButton,
  statusMessage,
}: Props) {
  function handleRemove(loginProvider: string, providerKey: string) {
    const formData = new FormData();
    formData.append('formAction', 'remove');
    formData.append('loginProvider', loginProvider);
    formData.append('providerKey', providerKey);
    router.post('/Identity/Account/Manage/ExternalLogins', formData);
  }

  function handleLink(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    formData.append('formAction', 'link');
    router.post('/Identity/Account/Manage/ExternalLogins', formData);
  }

  return (
    <ManageLayout activePage="ExternalLogins">
      {statusMessage && (
        <div className="alert-success mb-4 text-sm" role="alert">
          {statusMessage}
        </div>
      )}
      {currentLogins.length > 0 && (
        <>
          <h3 className="text-xl font-bold mb-4">Registered Logins</h3>
          <table className="w-full mb-6">
            <tbody>
              {currentLogins.map((login) => (
                <tr key={login.loginProvider}>
                  <td className="py-2">{login.providerDisplayName ?? login.loginProvider}</td>
                  <td className="py-2 text-right">
                    {showRemoveButton && (
                      <Button
                        type="button"
                        variant="secondary"
                        onClick={() => handleRemove(login.loginProvider, login.providerKey)}
                      >
                        Remove
                      </Button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </>
      )}
      {otherLogins.length > 0 && (
        <>
          <h4 className="text-lg font-semibold mb-2">Add another service to log in.</h4>
          <hr className="mb-4" />
          <form onSubmit={handleLink}>
            <div className="flex gap-2 flex-wrap">
              {otherLogins.map((provider) => (
                <Button key={provider.name} type="submit" name="provider" value={provider.name}>
                  {provider.displayName}
                </Button>
              ))}
            </div>
          </form>
        </>
      )}
    </ManageLayout>
  );
}
